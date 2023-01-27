using System.Linq;
using Content.Server.MachineLinking.Events;
using Content.Server.Genetics.GenePod;
using Content.Shared.Genetics.GeneticsConsole;
using Content.Server.Genetics.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Server.UserInterface;
using Content.Shared.MachineLinking.Events;
using Content.Server.MachineLinking.Components;
using Content.Shared.Genetics;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Server.Genetics
{
    public sealed class GeneticsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const string GeneRepairDamageType = "Radiation";
        private const string GeneRepairHealingGroup = "Genetic";
        private const string MutationDamageType = "Cellular";

        private Dictionary<Gene, GenePuzzle> _puzzles = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GeneticsConsoleComponent, SequenceButtonPressedMessage>(OnSequenceButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, RepairButtonPressedMessage>(OnRepairButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, StartActivationButtonPressedMessage>(OnStartActivationButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, CancelActivationButtonPressedMessage>(OnCancelActivationButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, ActivateButtonPressedMessage>(OnActivateButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, UnusedBlockButtonPressedMessage>(OnUnusedBlockButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, UsedBlockButtonPressedMessage>(OnUsedBlockButton);
            SubscribeLocalEvent<GeneticsConsoleComponent, FillBasePairButtonPressedMessage>(OnFillBasePairButton);

            SubscribeLocalEvent<GeneticsConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GeneticsConsoleComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<GeneticsConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

            SubscribeLocalEvent<GeneticsConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);

            SubscribeLocalEvent<GeneticsConsoleComponent, BeforeActivatableUIOpenEvent>((e, c, _) => UpdateUserInterface(e, c));
        }

        private void OnMapInit(EntityUid uid, GeneticsConsoleComponent component, MapInitEvent args)
        {
            if (!TryComp<SignalTransmitterComponent>(uid, out var transmitter))
                return;

            foreach (var port in transmitter.Outputs.Values.SelectMany(ports => ports))
            {
                if (!TryComp<GenePodComponent>(port.Uid, out var genePod))
                    continue;
                genePod.ConnectedConsole = uid;
                component.GenePod = port.Uid;
                component.GenePodInRange = CheckPodInRange(component, uid, port.Uid);
                return;
            }
        }

        private void OnNewLink(EntityUid uid, GeneticsConsoleComponent component, NewLinkEvent args)
        {
            if (!TryComp<GenePodComponent>(args.Receiver, out var genePod))
                return;

            component.GenePod = args.Receiver;
            genePod.ConnectedConsole = uid;

            RecheckConnections(uid, component.GenePod, component);
        }

        private void OnPortDisconnected(EntityUid uid, GeneticsConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port == component.PodPort && component.GenePod != null)
            {
                if (TryComp<GenePodComponent>(component.GenePod, out var genePod))
                    genePod.ConnectedConsole = null;
                component.GenePod = null;
            }

            UpdateUserInterface(uid, component);
        }

        private void OnAnchorChanged(EntityUid uid, GeneticsConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.GenePod == null || !TryComp<GenePodComponent>(component.GenePod, out var pod))
                return;

            if (args.Anchored)
            {
                RecheckConnections(uid, component.GenePod, component);
                return;
            }
        }

        private bool CheckPodInRange(GeneticsConsoleComponent consoleComp, EntityUid consoleUid, EntityUid genePodUid)
        {
            Transform(genePodUid).Coordinates.TryDistance(EntityManager, Transform(consoleUid).Coordinates, out float podDistance);
            return podDistance <= consoleComp.MaxDistance;
        }

        public void RecheckConnections(EntityUid console, EntityUid? genePod, GeneticsConsoleComponent? consoleComp = null)
        {
            if (!Resolve(console, ref consoleComp))
                return;

            if (genePod != null)
                consoleComp.GenePodInRange = CheckPodInRange(consoleComp, console, genePod.Value);

            UpdateUserInterface(console, consoleComp);
        }

        private void UpdateUserInterface(EntityUid uid, GeneticsConsoleComponent? component = null, bool forceUpdate = false)
        {
            if (!Resolve(uid, ref component, false))
                return;

            var podStatus = PodStatus.PodEmpty;
            var podConnected = false;
            var podInRange = false;
            EntityUid? body = null;
            var timeRemaining = TimeSpan.Zero;
            var totalTime = TimeSpan.Zero;
            List<GeneDisplay> geneSequence = new();
            if (component.GenePod != null && TryComp<GenePodComponent>(component.GenePod, out var genePod))
            {
                podConnected = true;
                podInRange = component.GenePodInRange;

                body = genePod.BodyContainer.ContainedEntity;
                if (genePod.Scanning) timeRemaining = _timing.CurTime - genePod.StartTime;
                totalTime = genePod.SequencingDuration * genePod.SequencingDurationMulitplier;

                if (body == null) { podStatus = PodStatus.PodEmpty; }
                else if (genePod.Scanning)
                {
                    podStatus = (timeRemaining.TotalMilliseconds > 0) ? PodStatus.ScanStarted : PodStatus.ScanComplete;
                }
                else if (genePod.LastScannedBody != null)
                {
                    podStatus = PodStatus.ScanComplete;
                    if (TryComp<GeneticSequenceComponent>(genePod.LastScannedBody, out var geneticSequence))
                    {
                        geneSequence = GenerateDisplayForAllGenes(geneticSequence.Genes);
                    }
                    else
                    {
                        podStatus = PodStatus.PodOccupantNoGenes;
                    }
                }
                else { podStatus = PodStatus.PodOccupantAlive; }

            }

            var state = new GeneticsConsoleBoundUserInterfaceState(body, podStatus, podConnected, podInRange, timeRemaining, totalTime, geneSequence,
                component.ResearchedMutations, component.TargetActivationGene, component.Puzzle, forceUpdate);
            var bui = _ui.GetUi(uid, GeneticsConsoleUiKey.Key);
            _ui.SetUiState(bui, state);
        }

        public List<GeneDisplay> GenerateDisplayForAllGenes(List<Gene> genes)
        {

            List<GeneDisplay> display = new();
            foreach (var gene in genes)
            {
                if (gene.Type != GeneType.Mutation || gene.Active)
                {
                    display.Add(new GeneDisplay(gene, string.Join(" ", gene.Blocks.Select(b => b.Display))));
                }
                else
                {
                    // inactive mutation
                    if (!_puzzles.ContainsKey(gene))
                    {
                        var solution = gene.Blocks[0].Display;
                        _puzzles[gene] = GeneratePuzzle(solution);
                    }

                    List<List<BasePair>> allBPs = _puzzles[gene].UsedBlocks.Concat(_puzzles[gene].UnusedBlocks).ToList();
                    display.Add(new GeneDisplay(gene, PuzzleChecker.CalculateSubmittedSequence(allBPs)));
                }
                
            }

            return display;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var pod in EntityQuery<GenePodComponent>())
            {
                if (pod.ConnectedConsole != null)
                    UpdateUserInterface(pod.ConnectedConsole.Value);

                if (!pod.Scanning || _timing.CurTime - pod.StartTime < (pod.SequencingDuration * pod.SequencingDurationMulitplier))
                    continue;

                FinishSequencing(pod.Owner, pod);
            }
        }

        public void FinishSequencing(EntityUid uid, GenePodComponent? pod = null)
        {
            if (!Resolve(uid, ref pod))
                return;

            pod.Scanning = false;
            pod.LastScannedBody = pod.BodyContainer.ContainedEntity;

            _audio.PlayPvs(pod.SequencingFinishedSound, uid);

            if (pod.ConnectedConsole != null)
                UpdateUserInterface((EntityUid) pod.ConnectedConsole, null, true);
        }

        /// <summary>
        ///     Force closes all interfaces currently open related to this console.
        /// </summary>
        private void ForceCloseAllInterfaces(EntityUid uid)
        {
            _ui.TryCloseAll(uid, GeneticsConsoleUiKey.Key);
        }

        private void OnSequenceButton(EntityUid uid, GeneticsConsoleComponent component, SequenceButtonPressedMessage args)
        {
            component.TargetActivationGene = null;
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;

            if (genePod.BodyContainer.ContainedEntity == null) return;

            genePod.StartTime = _timing.CurTime;
            genePod.Scanning = true;
            genePod.LastScannedBody = null;
            UpdateUserInterface(uid, component, true);
        }

        private void OnRepairButton(EntityUid uid, GeneticsConsoleComponent component, RepairButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            if (!TryComp<GeneticSequenceComponent>(genePod.LastScannedBody, out var geneticSequence))
                return;

            if (TryComp<DamageableComponent>(genePod.BodyContainer.ContainedEntity, out var damageable)) {
                var dmg = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(GeneRepairDamageType), genePod.BaseRadiationDamageOnRepair * genePod.DamageReductionMultiplier);
                var actual = _damageableSystem.TryChangeDamage(genePod.BodyContainer.ContainedEntity, dmg, origin: component.GenePod);

                var totalTaken = damageable.DamagePerGroup[GeneRepairHealingGroup];
                var damagedGenes = geneticSequence.Genes.Where(g => g.Damaged).ToList();

                var healedAmt = - (totalTaken / damagedGenes.Count);
                var heal = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>(GeneRepairHealingGroup), healedAmt);
                _damageableSystem.TryChangeDamage(genePod.BodyContainer.ContainedEntity, heal, ignoreResistances: true, origin: component.GenePod);
            }

            _audio.PlayPvs(genePod.RepairGeneSound, uid);
            geneticSequence.Genes[args.Index].Damaged = false;
            UpdateUserInterface(uid, component, true);
        }

        private void OnStartActivationButton(EntityUid uid, GeneticsConsoleComponent component, StartActivationButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            if (!TryComp<GeneticSequenceComponent>(genePod.LastScannedBody, out var geneticSequence))
                return;
            var gene = geneticSequence.Genes[args.Index];
            component.TargetActivationGene = gene;

            if (!_puzzles.ContainsKey(gene))
            {
                var solution = gene.Blocks[0].Display;
                _puzzles[gene] = GeneratePuzzle(solution);
            }
            component.Puzzle = _puzzles[gene];

            UpdateUserInterface(uid, component, true);
        }

        private void OnCancelActivationButton(EntityUid uid, GeneticsConsoleComponent component, CancelActivationButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            component.TargetActivationGene = null;
            UpdateUserInterface(uid, component, true);
        }

        private void OnUnusedBlockButton(EntityUid uid, GeneticsConsoleComponent component, UnusedBlockButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            if (component.Puzzle != null && args.Index < component.Puzzle.UnusedBlocks.Count)
            {
                var block = component.Puzzle.UnusedBlocks[args.Index];
                component.Puzzle.UnusedBlocks.RemoveAt(args.Index);
                component.Puzzle.UsedBlocks.Add(block);
            }
            UpdateUserInterface(uid, component, true);
        }

        private void OnUsedBlockButton(EntityUid uid, GeneticsConsoleComponent component, UsedBlockButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            if (component.Puzzle != null && args.Index < component.Puzzle.UsedBlocks.Count)
            {
                var block = component.Puzzle.UsedBlocks[args.Index];
                component.Puzzle.UsedBlocks.RemoveAt(args.Index);
                component.Puzzle.UnusedBlocks.Add(block);
            }
            UpdateUserInterface(uid, component, true);
        }

        private void OnFillBasePairButton(EntityUid uid, GeneticsConsoleComponent component, FillBasePairButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            if (component.Puzzle != null && args.TargetBlockIndex < component.Puzzle.UnusedBlocks.Count)
            {
                var block = component.Puzzle.UnusedBlocks[args.TargetBlockIndex];
                if (args.TargetPairIndex < block.Count) {
                    var pair = block[args.TargetPairIndex];
                    if (args.IsTop) pair.TopAssigned = args.NewBase;
                    else pair.BotAssigned = args.NewBase;
                }
            }
            else if (component.Puzzle != null && args.TargetBlockIndex - component.Puzzle.UnusedBlocks.Count < component.Puzzle.UsedBlocks.Count)
            {
                var block = component.Puzzle.UsedBlocks[args.TargetBlockIndex - component.Puzzle.UnusedBlocks.Count];
                if (args.TargetPairIndex < block.Count)
                {
                    var pair = block[args.TargetPairIndex];
                    if (args.IsTop) pair.TopAssigned = args.NewBase;
                    else pair.BotAssigned = args.NewBase;
                }
            }
            UpdateUserInterface(uid, component, true);
        }

        private void OnActivateButton(EntityUid uid, GeneticsConsoleComponent component, ActivateButtonPressedMessage args)
        {
            if (component.Puzzle == null || !TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;
            if (!TryComp<GeneticSequenceComponent>(genePod.LastScannedBody, out var geneticSequence))
                return;

            var puzzle = component.Puzzle;
            int slnDiff = PuzzleChecker.CalculatePuzzleSolutionDiff(puzzle);

            if (TryComp<DamageableComponent>(genePod.BodyContainer.ContainedEntity, out var damageable))
            {
                var amount = (genePod.BaseGeneticDamageOnEdit + genePod.AdditionalGeneticDamageOnFailure * slnDiff) * genePod.DamageReductionMultiplier;
                var dmg = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(MutationDamageType), amount);
                _damageableSystem.TryChangeDamage(genePod.BodyContainer.ContainedEntity, dmg, origin: component.GenePod);
            }

            if (component.TargetActivationGene != null && slnDiff == 0)
            {
                component.TargetActivationGene.Active = true;
                var mutationId = component.TargetActivationGene.Blocks[0].Value;
                RaiseLocalEvent(new ActivateMutationEvent((EntityUid) genePod.BodyContainer.ContainedEntity!, mutationId, uid));
                component.TargetActivationGene = null;
                _audio.PlayPvs(genePod.EditGeneSuccessSound, uid);
            }
            else
            {
                _audio.PlayPvs(genePod.EditGeneFailedSound, uid);
            }
            UpdateUserInterface(uid, component, true);
        }

        private List<BasePair> GenerateSolvedPairs(string solution)
        {
            var charToBP = new Dictionary<char, Base>
            {
                { 'A', Base.A },
                { 'T', Base.T },
                { 'G', Base.G },
                { 'C', Base.C }
            };
            var charToOpposite = new Dictionary<char, Base>
            {
                { 'A', Base.T },
                { 'T', Base.A },
                { 'G', Base.C },
                { 'C', Base.G }
            };

            var pairs = new List<BasePair>();
            foreach (char c in solution)
            {
                var p = new BasePair(charToBP[c], charToBP[c], charToOpposite[c], charToOpposite[c]);
                pairs.Add(p);
            }
            return pairs;
        }

        private GenePuzzle GeneratePuzzle(string solution)
        {
            var random = new Random();

            int cuts = 3;
            int doubleUnkowns = 2;
            int unknowns = 4;
            List<BasePair> solved = GenerateSolvedPairs(solution);
            List<List<BasePair>> puzzle = new List<List<BasePair>>();
            int totalLength = solved.Count;

            // insert single unknown bases
            for (var i = 0; i < unknowns; ++i)
            {
                int index = random.Next(0, totalLength);
                if (random.Next(0, 2) == 0)
                {
                    solved[index].TopAssigned = Base.Unknown;
                    solved[index].TopActual = Base.Unknown;
                }
                else
                {
                    solved[index].BotAssigned = Base.Unknown;
                    solved[index].BotActual = Base.Unknown;
                }
            }

            // insert double unknowns
            for (var i = 0; i < doubleUnkowns; ++i)
            {
                int index = random.Next(0, totalLength);
                solved[index].TopAssigned = Base.Unknown;
                solved[index].TopActual = Base.Unknown;
                solved[index].BotAssigned = Base.Unknown;
                solved[index].BotActual = Base.Unknown;
            }

            // make cuts
            int start = 0;
            for (var i = 0; i < cuts; ++i)
            {
                int min = Math.Max(i+2, (totalLength / cuts) * i);
                int max = Math.Min(totalLength - 1, (totalLength / cuts) * (i + 1));

                int cut = random.Next(min, max);
                int stop = Math.Min(cut, totalLength);
                List<BasePair> block = new List<BasePair>();
                for (var j = start; j < stop; j++)
                {
                    block.Add(solved[j]);
                }
                start += block.Count;
                puzzle.Add(block);
            }
            List<BasePair> remaining = new List<BasePair>();
            for (var j = start; j < totalLength; j++)
            {
                remaining.Add(solved[j]);
            }
            if (remaining.Count > 0) puzzle.Add(remaining);

            // for each resulting segment, decide if it will be a clean or jagged cut
            List<BasePair>? previous = null;
            foreach(var block in puzzle)
            {
                if (previous != null)
                {
                    var dieRoll = random.Next(0, 10);
                    if (dieRoll < 4)
                    {
                        var splitPairs = splitPair(previous.Pop());
                        previous.Add(splitPairs.Item1);
                        block.Insert(0, splitPairs.Item2);
                    }
                    else if (dieRoll < 8)
                    {
                        var splitPairs = splitPair(previous.Pop());
                        previous.Add(splitPairs.Item2);
                        block.Insert(0, splitPairs.Item1);
                    }
                }
                previous = block;
            }
            Shuffle(puzzle, random);
            return new GenePuzzle(solution, puzzle, new List<List<BasePair>>());
        }

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private Tuple<BasePair, BasePair> splitPair(BasePair pair)
        {
            var top = new BasePair(pair.TopActual, pair.TopAssigned, Base.Empty, Base.Empty);
            var bot = new BasePair(Base.Empty, Base.Empty, pair.BotActual, pair.BotAssigned);
            return new Tuple<BasePair, BasePair>(top, bot);
        }
    }
}
