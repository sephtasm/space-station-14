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
using Content.Shared.Popups;
using Content.Server.Paper;
using Robust.Shared.Random;

namespace Content.Server.Genetics
{
    public sealed class GeneticsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly GeneticsSystem _geneticsSystem = default!;
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

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
            SubscribeLocalEvent<GeneticsConsoleComponent, PrintReportButtonPressedMessage>(OnPrintReportButton);
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
                        geneSequence = GenerateDisplayForAllGenes(geneticSequence.Genes, component.ResearchedMutations);
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

        public List<GeneDisplay> GenerateDisplayForAllGenes(List<Gene> genes, Dictionary<long, string> researchedMutations)
        {

            List<GeneDisplay> display = new();
            foreach (var gene in genes)
            {
                if (gene.Type != GeneType.Mutation || gene.Active || researchedMutations.ContainsKey(gene.Blocks[0].Value))
                {
                    display.Add(new GeneDisplay(gene, string.Join(" ", gene.Blocks.Select(b => b.Display))));
                }
                else
                {
                    // inactive mutation
                    if (!_puzzles.ContainsKey(gene))
                    {
                        var solution = gene.Blocks[0].Display;
                        _puzzles[gene] = SharedGenetics.GeneratePuzzle(solution, _random);
                    }

                    List<List<BasePair>> allBPs = _puzzles[gene].UsedBlocks.Concat(_puzzles[gene].UnusedBlocks).ToList();
                    display.Add(new GeneDisplay(gene, SharedGenetics.CalculateSubmittedSequence(allBPs)));
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

            if (pod.ConnectedConsole != null &&
                TryComp<GeneticSequenceComponent>(pod.LastScannedBody, out var geneticSequence))
            {
                foreach (var gene in geneticSequence.Genes)
                {
                    if (gene.Type == GeneType.Mutation && gene.Blocks.Count == 1 && gene.Active)
                    {
                        _geneticsSystem.UpdateKnownMutations(pod.ConnectedConsole.Value, gene.Blocks[0].Value);
                    }
                }
                
                UpdateUserInterface((EntityUid) pod.ConnectedConsole, null, true);
            }
                
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

            _audio.PlayPvs(genePod.RepairGeneSound, component.GenePod.Value);
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

            if (component.ResearchedMutations.ContainsKey(gene.Blocks[0].Value))
            {
                DoMutationActivate(uid, gene, component.GenePod.Value, genePod);
            }
            else
            {
                component.TargetActivationGene = gene;

                if (!_puzzles.ContainsKey(gene))
                {
                    var solution = gene.Blocks[0].Display;
                    _puzzles[gene] = SharedGenetics.GeneratePuzzle(solution, _random);
                }
                component.Puzzle = _puzzles[gene];
            }

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

        private void OnPrintReportButton(EntityUid uid, GeneticsConsoleComponent component, PrintReportButtonPressedMessage args)
        {
            if (!TryComp<GenePodComponent>(component.GenePod, out var genePod))
                return;

            var patientName = genePod.LastScannedBody.HasValue ? _entityManager.GetComponent<MetaDataComponent>(genePod.LastScannedBody.Value).EntityName
                        : Loc.GetString("genetics-console-ui-window-patient-name-unknown");

            var report = Spawn(component.ReportEntityId, Transform(uid).Coordinates);
            MetaData(report).EntityName = Loc.GetString("genetics-console-print-report-title", ("name", patientName));

            var msg = GetGeneticsScanMessage(component, genePod, patientName);
            if (msg == null)
                return;

            _audio.PlayPvs(component.PrintReportSound, uid);
            _popup.PopupEntity(Loc.GetString("genetics-console-print-popup"), uid);
            _paper.SetContent(report, msg.ToMarkup());
            UpdateUserInterface(uid, component);
        }

        private FormattedMessage? GetGeneticsScanMessage(GeneticsConsoleComponent console, GenePodComponent genePod, string patientName)
        {
            var msg = new FormattedMessage();
            if (genePod.LastScannedBody == null)
                return null;

            var n = genePod.LastScannedBody;

            msg.AddMarkup(Loc.GetString("genetics-console-print-report-title", ("name", patientName)));
            if (TryComp<GeneticSequenceComponent>(genePod.LastScannedBody, out var geneticSequence))
            {
                var geneDisplay = GenerateDisplayForAllGenes(geneticSequence.Genes, console.ResearchedMutations);

                foreach (var display in geneDisplay)
                {
                    var type = SharedGenetics.GetGeneTypeLoc(display.Gene, console.ResearchedMutations);
                    var typeMarkup = Loc.GetString("genetics-console-print-report-gene-type", ("type", type)).PadRight(24);
                    msg.AddMarkup(typeMarkup);
                    if (display.Gene.Type == GeneType.Mutation)
                    {
                        var statusStringId = (display.Gene.Active) ? "genetics-console-print-report-mutation-activated" : "genetics-console-print-report-mutation-dormant";
                        msg.AddMarkup(Loc.GetString("genetics-console-print-report-status", ("status", Loc.GetString(statusStringId))));
                    }
                    msg.PushNewline();
                    msg.AddMarkup(Loc.GetString("genetics-console-print-report-sequence", ("sequence", display.Display)));
                    msg.PushNewline();
                    msg.PushNewline();
                }
            }

            return msg;
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
            int slnDiff = SharedGenetics.CalculatePuzzleSolutionDiff(puzzle);

            bool forceActivate = true; // for debugging only
            if (forceActivate) slnDiff = 0;

            if (TryComp<DamageableComponent>(genePod.BodyContainer.ContainedEntity, out var damageable))
            {
                var amount = (genePod.BaseGeneticDamageOnEdit + genePod.AdditionalGeneticDamageOnFailure * slnDiff) * genePod.DamageReductionMultiplier;
                var dmg = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(MutationDamageType), amount);
                _damageableSystem.TryChangeDamage(genePod.BodyContainer.ContainedEntity, dmg, origin: component.GenePod);
            }

            if (component.TargetActivationGene != null && slnDiff == 0)
            {
                DoMutationActivate(uid, component.TargetActivationGene, component.GenePod.Value, genePod);
                component.TargetActivationGene = null;
            }
            else
            {
                _audio.PlayPvs(genePod.EditGeneFailedSound, component.GenePod.Value);
            }
            UpdateUserInterface(uid, component, true);
        }

        private void DoMutationActivate(EntityUid uid, Gene gene, EntityUid podUid, GenePodComponent genePod)
        {
            _geneticsSystem.ActivateMutation((EntityUid) genePod.BodyContainer.ContainedEntity!, gene, uid);
            _audio.PlayPvs(genePod.EditGeneSuccessSound, podUid);
        }
    }
}
