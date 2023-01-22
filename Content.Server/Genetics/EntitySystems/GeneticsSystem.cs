using Content.Server.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Collections.Immutable;
using System.Text;
using Linguini.Syntax.Ast;
using Content.Shared.Genetics;
using static Content.Server.Genetics.GeneticSequenceComponent;
using System.Collections.Generic;
using Content.Server.Humanoid;
using Robust.Shared.Enums;
using static Content.Shared.Humanoid.SharedHumanoidSystem;
using System.ComponentModel.Design;
using Content.Server.Database;

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
        [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;

        private readonly Dictionary<MarkingCategories, Dictionary<long, MarkingPrototype>> _markingsIndex = new();
        private readonly Dictionary<long, string> _speciesIndex = new();
        private readonly Dictionary<MarkingCategories, long> _maxMarkingPointsPerCategory = new();
        private readonly Dictionary<long, Sex> _sexIndex = new();

        private const int SPECIES_GENE_INDEX = 0;
        private const int SEX_GENE_INDEX = 1;
        private const int SKIN_COLOR_GENE_INDEX = 2;
        private const int EYE_COLOR_GENE_INDEX = 3;
        private const int MARKINGS_GENE_INDEX = 4;

        public override void Initialize()
        {
            // SubscribeLocalEvent<FingerprintComponent, ContactInteractionEvent>(OnInteract);
            // SubscribeLocalEvent<FingerprintComponent, ComponentInit>(OnInit);
            
            PopulateSpeciesIndex();
            PopulateMarkingsIndex();
            PopulateMarkingPointsIndex();
            PopulateSexIndex();

            SubscribeLocalEvent<GeneticSequenceComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<HumanoidAppearanceUpdateEvent>(OnAppearanceUpdate);
        }

        public void ModifyGenes(EntityUid uid, List<Gene> genes, GeneModificationMethod modificationMethod,
            HumanoidComponent? targetHumanoid = null)
        {
            if (!Resolve(uid, ref targetHumanoid) || !TryComp<GeneticSequenceComponent>(uid, out var geneticSequence))
            {
                return;
            }

            geneticSequence.Genes = genes;

            targetHumanoid.Species = _speciesIndex[genes[SPECIES_GENE_INDEX].Blocks[0].Value];
            targetHumanoid.Sex = _sexIndex[genes[SEX_GENE_INDEX].Blocks[0].Value];
            targetHumanoid.SkinColor = ConvertToColor(genes[SKIN_COLOR_GENE_INDEX]);

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
            targetHumanoid.CurrentMarkings = new MarkingSet(markings);

            _humanoidSystem.SetAppearance(uid,
                targetHumanoid.Species,
                targetHumanoid.CustomBaseLayers,
                targetHumanoid.SkinColor,
                targetHumanoid.Sex,
                targetHumanoid.AllHiddenLayers.ToList(),
                targetHumanoid.CurrentMarkings.GetForwardEnumerator().ToList());
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
                if (block.Type == BlockType.Primary)
                {
                    markingPrototype = _markingsIndex[category][block.Value];
                }
                else
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

        /// <summary>
        /// Converts the given decimal number to a base 4 system using gene pairs.
        /// </summary>
        /// <param name="decimalNumber">The number to convert.</param>
        /// <returns></returns>
        private static string DecimalToGene(long decimalNumber)
        {
            const int radix = 4;
            const int bitsInLong = 64;
            const string digits = "ATGC";

            if (decimalNumber == 0)
                return "A";

            int index = bitsInLong - 1;
            long currentNumber = Math.Abs(decimalNumber);
            char[] charArray = new char[bitsInLong];

            while (currentNumber != 0)
            {
                int remainder = (int) (currentNumber % radix);
                charArray[index--] = digits[remainder];
                currentNumber = currentNumber / radix;
            }

            string result = new String(charArray, index + 1, bitsInLong - index - 1);
            if (decimalNumber < 0)
            {
                result = "-" + result;
            }

            return result;
        }
        private Gene ConvertToGene(String species)
        {
            var geneSegment = new List<Block>();
            var key = _speciesIndex.FirstOrDefault(x => x.Value == species).Key;
            geneSegment.Add(new Block (key));
            var gene = new Gene(GeneType.Species, geneSegment);

            return gene;
        }
        private Gene ConvertToGene(Sex sex)
        {
            var geneSegment = new List<Block>();
            var key = _sexIndex.FirstOrDefault(x => x.Value == sex).Key;
            geneSegment.Add(new Block(key));
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
                    markingGenes.Add(new Gene(GeneType.Markings, new List<Block>(), markingCategory));
                }
                genes.AddRange(markingGenes);
            }

            return genes;
        }

        private Gene ConvertToGene(Marking marking, MarkingCategories markingCategory)
        {
            var blocksForGene = new List<Block>();
            var key = _markingsIndex[markingCategory].FirstOrDefault(x => x.Value.ID == marking.MarkingId).Key;
            blocksForGene.Add(new Block(key, BlockType.Primary));

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
            int integerColorR = (int) Math.Floor(color.R * 100.0d);
            int integerColorG = (int) Math.Floor(color.G * 100.0d);
            int integerColorB = (int) Math.Floor(color.B * 100.0d);
            int integerColorA = (int) Math.Floor(color.A * 100.0d);

            geneSegment.Add(new Block(integerColorR, blockType));
            geneSegment.Add(new Block(integerColorG, blockType));
            geneSegment.Add(new Block(integerColorB, blockType));
            geneSegment.Add(new Block(integerColorA, blockType));

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

        private void PopulateSpeciesIndex()
        {
            _speciesIndex.Clear();
            var allSpecies = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>().ToImmutableList();
            // var genesRequiredToRepresent = allSpecies.Count / 4;
            var i = 0;
            foreach (var species in allSpecies)
            {
                var geneIndex = i++;
                _speciesIndex.Add(geneIndex, species.ID);
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
            // var genesRequiredToRepresent = allSpecies.Count / 4;
            var i = 0;
            foreach (Sex sex in sexes)
            {
                _sexIndex.Add(i++, sex);
            }
        }

        private void PopulateMarkingsIndex()
        {
            _markingsIndex.Clear();
            // var genesRequiredToRepresent = allSpecies.Count / 4;

            foreach (MarkingCategories markingCategory in Enum.GetValues(typeof(MarkingCategories)))
            {
                var markingsIndexForCategory = new Dictionary<long, MarkingPrototype>();
                var i = 0;
                foreach (var prototype in _prototypeManager.EnumeratePrototypes<MarkingPrototype>())
                {
                    markingsIndexForCategory.Add(i++, prototype);
                }
                _markingsIndex.Add(markingCategory, markingsIndexForCategory);
            }
            
        }
        
        private void OnAppearanceUpdate(HumanoidAppearanceUpdateEvent ev)
        {
            if (!ev.Handled && TryComp(ev.Uid, out HumanoidComponent? targetHumanoid) && TryComp(ev.Uid, out GeneticSequenceComponent? geneSequence))
            {
                geneSequence.Genes = GenerateGeneticSequence(targetHumanoid);
                ev.Handled = true;
            }
            return;

        }
        private void OnInit(EntityUid uid, GeneticSequenceComponent component, ComponentInit args)
        {
            // don't do anything because the entity's appearance will be updated?
            /*
            if (TryComp(uid, out HumanoidComponent? targetHumanoid)) {
                component.Genes = GenerateGeneticSequence(targetHumanoid);
            }*/
            return;

        }

        private List<Gene> GenerateGeneticSequence(HumanoidComponent targetHumanoid)
        {
            var geneSequence = new List<Gene>(4)
            {
                ConvertToGene(targetHumanoid.Species), // SPECIES_GENE_INDEX = 0
                ConvertToGene(targetHumanoid.Sex), // SEX_GENE_INDEX = 1
                ConvertToGene(targetHumanoid.SkinColor) // SKIN_COLOR_GENE_INDEX = 2
            };

            if (targetHumanoid.CustomBaseLayers.ContainsKey(HumanoidVisualLayers.Eyes)) // EYE_COLOR_GENE_INDEX = 3
            {
                geneSequence.Add(ConvertToGene(targetHumanoid.CustomBaseLayers[HumanoidVisualLayers.Eyes].Color, GeneType.EyeColor));
            }
            else
            {
                geneSequence.Add(new Gene(GeneType.EyeColor, new List<Block>()));
            }
            geneSequence.AddRange(ConvertToGenes(targetHumanoid.CurrentMarkings));

            return geneSequence;
        }

    }
}
