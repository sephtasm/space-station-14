using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Collections.Immutable;
using Content.Shared.Genetics;
using Content.Server.Humanoid;
using Content.Shared.Genetics.Prototypes;
using Content.Shared.Damage;
using Content.Server.Genetics.Components;
using static Content.Shared.Humanoid.HumanoidAppearanceState;
using static Content.Shared.Humanoid.SharedHumanoidAppearanceSystem;

namespace Content.Server.Genetics
{
    
    /// <summary>
    /// This event alerts system that the solution was changed
    /// </summary>
    public sealed class DNASequenceChangedEvent : EntityEventArgs
    {
        public readonly List<Gene>? Blocks;

        public DNASequenceChangedEvent(List<Gene>? blocks)
        {
            Blocks = blocks;
        }
    }

    public sealed class GeneticsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;

        private readonly Dictionary<MarkingCategories, Dictionary<uint, MarkingPrototype>> _markingsIndex = new();
        private readonly Dictionary<uint, string> _speciesIndex = new();
        private readonly Dictionary<uint, MutationPrototype> _mutationsIndex = new();
        private readonly Dictionary<MarkingCategories, int> _maxMarkingPointsPerCategory = new();
        private readonly Dictionary<uint, Sex> _sexIndex = new();

        private byte[] _encryptionKey = default!;

        private const int SPECIES_GENE_INDEX = 0;
        private const int SEX_GENE_INDEX = 1;
        private const int SKIN_COLOR_GENE_INDEX = 2;
        private const int EYE_COLOR_GENE_INDEX = 3;
        private const int MARKINGS_GENE_INDEX = 4;

        private readonly string _damageGroupAffectingDNA = "Genetic";

        public override void Initialize()
        {
            PopulateSpeciesIndex();
            PopulateMarkingsIndex();
            PopulateMarkingPointsIndex();
            PopulateSexIndex();
            PopulateMutationIndex();
            GenerateObfuscationKeys();

            SubscribeLocalEvent<GeneticSequenceComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<HumanoidAppearanceUpdateEvent>(OnAppearanceUpdate);
            SubscribeLocalEvent<ActivateMutationEvent>(OnMutationActivate);
            SubscribeLocalEvent<DeactivateMutationEvent>(OnMutationDeactivate);

            SubscribeLocalEvent<GeneticSequenceComponent, DamageChangedEvent>(OnDamageTaken);
        }

        public void ModifyGenes(EntityUid uid, List<Gene> genes, GeneModificationMethod modificationMethod,
            HumanoidAppearanceComponent? targetHumanoid = null)
        {
            if (!Resolve(uid, ref targetHumanoid) || !TryComp<GeneticSequenceComponent>(uid, out var geneticSequence))
            {
                return;
            }

            geneticSequence.Genes = genes;

            var species = _speciesIndex[genes[SPECIES_GENE_INDEX].Blocks[0].Value];
            var sex = _sexIndex[genes[SEX_GENE_INDEX].Blocks[0].Value];
            var skinColor = ConvertToColor(genes[SKIN_COLOR_GENE_INDEX]);

            var newBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(targetHumanoid.CustomBaseLayers);
            if (genes[EYE_COLOR_GENE_INDEX].Blocks.Count > 0)
            {
                targetHumanoid.CustomBaseLayers[HumanoidVisualLayers.Eyes] = new CustomBaseLayerInfo(string.Empty, ConvertToColor(genes[EYE_COLOR_GENE_INDEX]));
            }

            List<Marking> markings = new List<Marking>();
            var geneIndex = MARKINGS_GENE_INDEX;
            while(geneIndex < genes.Count)
            {
                var gene = genes[geneIndex++];
                if (gene.Type == GeneType.Markings && gene.Blocks.Count > 0)
                {
                    if (TryConvertToMarking(gene, out var marking))
                    {
                        markings.Add(marking!);
                    }
                }
            }

            _humanoidSystem.SetAppearance(uid, species, skinColor, sex, newBaseLayers,
                new MarkingSet(markings), targetHumanoid);
        }

        private bool TryConvertToMarking(Gene gene, out Marking? marking)
        {
            marking = null;
            MarkingCategories category = (MarkingCategories) gene.MarkingCategory!;

            MarkingPrototype? markingPrototype = null;
            List<Block> colorBlocks = new List<Block>();
            List<Color> convertedColors = new List<Color>();
            foreach (Block block in gene.Blocks)
            {
                if (block.Type == BlockType.Primary && block.Value > 0)
                {
                    markingPrototype = _markingsIndex[category][block.Value];
                }
                else if (block.Type == BlockType.Modifier)
                {
                    colorBlocks.Add(block);
                    if (colorBlocks.Count == 4)
                    {
                        convertedColors.Add(ConvertToColor(colorBlocks));
                        colorBlocks.Clear();
                    }
                }
            }

            if (markingPrototype != null)
            {
                var convertedMarking = markingPrototype.AsMarking();
                int i = 0;
                foreach (Color color in convertedColors)
                {
                    convertedMarking.SetColor(i++, color);
                }
                marking = convertedMarking;
                return true;
            }
            return false;
        }

        private Gene ConvertToGene(string species)
        {
            var geneSegment = new List<Block>();
            var key = _speciesIndex.FirstOrDefault(x => x.Value == species).Key;
            geneSegment.Add(new Block (key, ObfuscateToString(key)));
            var gene = new Gene(GeneType.Species, geneSegment);

            return gene;
        }
        private Gene ConvertToGene(Sex sex)
        {
            var geneSegment = new List<Block>();
            var key = _sexIndex.FirstOrDefault(x => x.Value == sex).Key;
            geneSegment.Add(new Block(key, ObfuscateToString(key)));
            var gene = new Gene(GeneType.Sex, geneSegment);

            return gene;
        }
        private List<Gene> ConvertToGenes(MarkingSet markings)
        {
            // Dictionary<MarkingCategories, Dictionary<long, string>> _markingsIndex
            var genes = new List<Gene>();
            foreach (MarkingCategories markingCategory in Enum.GetValues(typeof(MarkingCategories)))
            {
                var perCategory = markings.TryGetCategory(markingCategory, out var categoryMarkings)
                    ? new List<Marking>(categoryMarkings)
                    : new();

                var markingGenes = new List<Gene>();
                foreach (Marking marking in perCategory)
                {
                    markingGenes.Add(ConvertToGene(marking, markingCategory));
                }
                while (markingGenes.Count < _maxMarkingPointsPerCategory.GetValueOrDefault(markingCategory, 0))
                {
                    markingGenes.Add(new Gene(GeneType.Markings, new List<Block>() { new Block(0, ObfuscateToString(0), BlockType.Primary) }, markingCategory));
                }
                genes.AddRange(markingGenes);
            }

            return genes;
        }

        private Gene ConvertToGene(Marking marking, MarkingCategories markingCategory)
        {
            var blocksForGene = new List<Block>();
            var key = _markingsIndex[markingCategory].FirstOrDefault(x => x.Value.ID == marking.MarkingId).Key;
            blocksForGene.Add(new Block(key, ObfuscateToString(key), BlockType.Primary));

            foreach (Color color in marking.MarkingColors)
            {
                blocksForGene.AddRange(ConvertToBlocks(color, BlockType.Modifier));
            }

            return new Gene(GeneType.Markings, blocksForGene, markingCategory);
        }

        private Gene ConvertToGene(Color color, GeneType type = GeneType.SkinColor, BlockType blockType = BlockType.Primary)
        {
            return new Gene(type, ConvertToBlocks(color, blockType));
        }
        private List<Block> ConvertToBlocks(Color color, BlockType blockType = BlockType.Primary)
        {
            var geneSegment = new List<Block>();
            uint integerColorR = (uint) Math.Floor(color.R * 100.0d);
            uint integerColorG = (uint) Math.Floor(color.G * 100.0d);
            uint integerColorB = (uint) Math.Floor(color.B * 100.0d);
            uint integerColorA = (uint) Math.Floor(color.A * 100.0d);

            geneSegment.Add(new Block(integerColorR, ObfuscateToString(integerColorR), blockType));
            geneSegment.Add(new Block(integerColorG, ObfuscateToString(integerColorG), blockType));
            geneSegment.Add(new Block(integerColorB, ObfuscateToString(integerColorB), blockType));
            geneSegment.Add(new Block(integerColorA, ObfuscateToString(integerColorA), blockType));

            return geneSegment;
        }
        private Color ConvertToColor(Gene gene)
        {
            return ConvertToColor(gene.Blocks);
        }

        private Color ConvertToColor(List<Block> blocks)
        {
            if (blocks.Count != 4)
            {
                return Color.White;
            }
            return new Color(
                blocks[0].Value / 100.0f, // R
                blocks[1].Value / 100.0f, // G
                blocks[2].Value / 100.0f, // B
                blocks[3].Value / 100.0f  // A
                );
        }

        private byte[] GenerateEncryptionKey()
        {
            byte[] key = new byte[256];
            for(int i=0; i < 256; ++i)
            {
                key[i] = (byte) i;
            }
            _random.Shuffle(key);
            return key;
        }

        private byte[] Encrypt(byte[] data, byte[] key)
        {
            byte[] result = new byte[data.Length];
            int c = 0;
            for(int i=0; i<data.Length; i++)
            {
                c = key[data[i] ^ c];
                result[i] = (byte) c;
            }
            return result;
        }

        private uint Obfuscate(uint number)
        {
            var arrA = BitConverter.GetBytes(number);
            var encryptA = Encrypt(arrA, _encryptionKey);
            return BitConverter.ToUInt32(encryptA);
        }

        private string ObfuscateToString(uint number)
        {
            var arrA = BitConverter.GetBytes(number);
            var encryptA = Encrypt(arrA, _encryptionKey);
            return SharedGenetics.DecimalToGene(BitConverter.ToUInt32(encryptA));
        }

        /// <summary>
        /// Generate an encryption key to use when representing gene values so that they will be obfuscated and won't appear sequential.
        /// </summary>
        private void GenerateObfuscationKeys()
        {
            // If we want to make this even more difficult we could generate a different key for each mutation/gene
            _encryptionKey = GenerateEncryptionKey();
        }

        private void PopulateSpeciesIndex()
        {
            _speciesIndex.Clear();
            var allSpecies = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>().ToImmutableList();
            // var genesRequiredToRepresent = allSpecies.Count / 4;
            uint i = 0;
            foreach (var species in allSpecies)
            {
                uint geneIndex = i++;
                _speciesIndex.Add(geneIndex, species.ID);
            }
        }

        private void PopulateMutationIndex()
        {
            _mutationsIndex.Clear();
            var allMutations = _prototypeManager.EnumeratePrototypes<MutationPrototype>().ToImmutableList();
            uint i = 0;
            foreach (var mutation in allMutations)
            {
                uint geneIndex = i++;
                _mutationsIndex.Add(geneIndex, mutation);
            }
        }

        private void PopulateMarkingPointsIndex()
        {
            _maxMarkingPointsPerCategory.Clear();
            var markingPointsPrototypes = _prototypeManager.EnumeratePrototypes<MarkingPointsPrototype>().ToImmutableList();
            foreach (var markingPoints in markingPointsPrototypes)
            {
                foreach(var category in markingPoints.Points.Keys)
                {
                    if (!_maxMarkingPointsPerCategory.ContainsKey(category) || _maxMarkingPointsPerCategory[category] < markingPoints.Points[category].Points)
                    {
                        _maxMarkingPointsPerCategory[category] = markingPoints.Points[category].Points;
                    }
                }
            }
        }
        private void PopulateSexIndex()
        {
            _sexIndex.Clear();
            var sexes = Enum.GetValues(typeof(Sex));
            uint i = 0;
            foreach (Sex sex in sexes)
            {
                uint geneIndex = i++;
                _sexIndex.Add(geneIndex, sex);
            }
        }

        private void PopulateMarkingsIndex()
        {
            _markingsIndex.Clear();

            foreach (MarkingCategories markingCategory in Enum.GetValues(typeof(MarkingCategories)))
            {
                var markingsIndexForCategory = new Dictionary<uint, MarkingPrototype>();
                uint i = 0;
                foreach (var prototype in _prototypeManager.EnumeratePrototypes<MarkingPrototype>())
                {
                    uint geneIndex = i++;
                    markingsIndexForCategory.Add(geneIndex, prototype);
                }
                _markingsIndex.Add(markingCategory, markingsIndexForCategory);
            }
            
        }

        public void UpdateKnownMutations(EntityUid consoleUid, uint mutationId)
        {
            if (TryComp(consoleUid, out GeneticsConsoleComponent? console))
            {
                var localizedName = Loc.GetString(_mutationsIndex[mutationId].LocalizationStringId);
                if (!console.ResearchedMutations.ContainsKey(mutationId))
                    console.ResearchedMutations.Add(mutationId, localizedName);
            }
        }

        private void OnMutationActivate(ActivateMutationEvent ev)
        {
            if (!ev.Handled && TryComp(ev.Uid, out GeneticSequenceComponent? geneSequence))
            {
                // not needed
                foreach (var gene in geneSequence.Genes)
                {
                    if (gene.Type == GeneType.Mutation && gene.Blocks[0].Value == ev.Value) gene.Active = true;
                }
                var mutationProto = _mutationsIndex[ev.Value];
                foreach (var effect in mutationProto.Effects)
                    effect.Apply(ev.Uid, mutationProto.ID, EntityManager, _prototypeManager);

                if (ev.ConsoleUid != null) UpdateKnownMutations((EntityUid) ev.ConsoleUid, ev.Value);
            }
        }

        private void OnMutationDeactivate(DeactivateMutationEvent ev)
        {
            if (!ev.Handled && TryComp(ev.Uid, out GeneticSequenceComponent? geneSequence))
            {
                // not needed
                foreach (var gene in geneSequence.Genes)
                {
                    if (gene.Type == GeneType.Mutation && gene.Blocks[0].Value == ev.Value) gene.Active = false;
                }
                var mutationProto = _mutationsIndex[ev.Value];
                foreach (var effect in mutationProto.Effects)
                    effect.Remove(ev.Uid, mutationProto.ID, EntityManager, _prototypeManager);
            }
        }

        private void OnDamageTaken(EntityUid uid, GeneticSequenceComponent geneComponent, DamageChangedEvent ev)
        {
            if (ev.DamageDelta != null && TryComp(uid, out DamageableComponent? damageable))
            {
                var damagePerGroup = ev.DamageDelta.GetDamagePerGroup();
                if (damagePerGroup.ContainsKey(_damageGroupAffectingDNA) && damagePerGroup[_damageGroupAffectingDNA] > 0)
                {
                    var totalTaken = damageable.DamagePerGroup[_damageGroupAffectingDNA];
                    var undamagedGenes = geneComponent.Genes.Where(g => !g.Damaged).ToList();

                    int targetDamagedGeneCount = Math.Min(geneComponent.Genes.Count, (int) (geneComponent.Genes.Count * (totalTaken / 100)));
                    int damagedGenes = geneComponent.Genes.Count - undamagedGenes.Count();
                    while (damagedGenes < targetDamagedGeneCount)
                    {
                        var toDamage = _random.Next(undamagedGenes.Count);
                        undamagedGenes[toDamage].Damaged = true;
                        undamagedGenes.RemoveAt(toDamage);
                        damagedGenes++;
                    }
                }
            }
        }
        public void ActivateAllMutations(EntityUid uid)
        {
            if (!TryComp<GeneticSequenceComponent>(uid, out var geneticSequence))
                return;
            foreach (var gene in geneticSequence.Genes)
            {
                ActivateMutation(uid, gene);
            }
        }

        public MutationPrototype? TryGetMutationProtoForGene(Gene gene)
        {
            if (gene.Type != GeneType.Mutation || gene.Blocks.Count != 1) return null;
            var mutationId = gene.Blocks[0].Value;
            return _mutationsIndex[mutationId];
        }

        public void ActivateMutation(EntityUid uid, Gene gene, EntityUid? consoleUid = null)
        {
            if (gene.Active) return;
            gene.Active = true;
            var mutationId = gene.Blocks[0].Value;
            RaiseLocalEvent(new ActivateMutationEvent(uid, mutationId, consoleUid));
        }

        private void OnAppearanceUpdate(HumanoidAppearanceUpdateEvent ev)
        {
            if (!ev.Handled && TryComp(ev.Uid, out HumanoidAppearanceComponent? targetHumanoid) && TryComp(ev.Uid, out GeneticSequenceComponent? geneSequence))
            {
                geneSequence.Genes = GenerateGeneticSequence(targetHumanoid, 0);
                // TODO: should not remove mutations
                ev.Handled = true;
            }
            return;

        }
        private void OnInit(EntityUid uid, GeneticSequenceComponent component, ComponentInit args)
        {
            if (TryComp(uid, out HumanoidAppearanceComponent? targetHumanoid))
            {
                component.Genes = GenerateGeneticSequence(targetHumanoid, component.RandomActiveMutationsOnInit);

                if (component.ForcedActiveMutationsOnInit.Count == 0)
                    return;

                foreach (var (key, value) in _mutationsIndex)
                {
                    if (component.ForcedActiveMutationsOnInit.Contains(value.ID))
                    {
                        var found = false;
                        foreach (Gene g in component.Genes)
                        {
                            if (g.Type == GeneType.Mutation && g.Blocks[0].Value == key)
                            {
                                ActivateMutation(uid, g);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            var mutationGene = CreateGeneForMutation(key);
                            component.Genes.Add(mutationGene);
                            ActivateMutation(uid, mutationGene);
                        }
                    }
                }
            }
        }

        private List<Gene> GenerateGeneticSequence(HumanoidAppearanceComponent targetHumanoid, int randomMutations)
        {
            var geneSequence = new List<Gene>(4)
            {
                ConvertToGene(targetHumanoid.Species), // SPECIES_GENE_INDEX = 0
                ConvertToGene(targetHumanoid.Sex), // SEX_GENE_INDEX = 1
                ConvertToGene(targetHumanoid.SkinColor) // SKIN_COLOR_GENE_INDEX = 2
            };

            if (targetHumanoid.CustomBaseLayers.TryGetValue(HumanoidVisualLayers.Eyes, out CustomBaseLayerInfo eyeBaseLayer) && eyeBaseLayer.Color != null) // EYE_COLOR_GENE_INDEX = 3
            {
                geneSequence.Add(ConvertToGene(eyeBaseLayer.Color.Value, GeneType.EyeColor, BlockType.Primary));
            }
            else
            {
                geneSequence.Add(new Gene(GeneType.EyeColor, new List<Block>()));
            }
            geneSequence.AddRange(ConvertToGenes(targetHumanoid.MarkingSet));

            int dormantMutationsGranted = 0;
            List<(uint, float)> weightedIds = new List<(uint, float)>();

            float totalWeight = 0f;
            foreach(var (id, proto) in _mutationsIndex)
            {
                weightedIds.Add( (id, proto.Weight) );
                totalWeight += proto.Weight;
            }

            // TODO: handle cases where randomMutations is close to the same as total mutations, low weights, etc.
            HashSet<uint> added = new();
            while (dormantMutationsGranted < randomMutations)
            {
                float currentWeightIndex = 0;
                float itemWeightIndex = (float) _random.NextDouble() * totalWeight;
                foreach ( var (id, weight) in weightedIds)
                {
                    currentWeightIndex += weight;
                    if (currentWeightIndex >= itemWeightIndex)
                    {
                        if (!added.Contains(id))
                        {
                            added.Add(id);
                            geneSequence.Add(CreateGeneForMutation(id));
                            dormantMutationsGranted++;
                        }
                        break;
                    }
                }

            }
            return geneSequence;
        }

        private Gene CreateGeneForMutation(uint id)
        {
            var block = new List<Block>();
            block.Add(new Block(id, ObfuscateToString(id)));
            var mutationGene = new Gene(GeneType.Mutation, block);
            mutationGene.Active = false;
            return mutationGene;
        }
    }
}
