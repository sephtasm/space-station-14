using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Genetics.GeneticsConsole;

namespace Content.Client.GeneticsConsole.UI
{
    [UsedImplicitly]
    public sealed class GeneticsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private GeneticsConsoleWindow? _window;

        public GeneticsConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new GeneticsConsoleWindow
            {
                Title = Loc.GetString("genetics-console-ui-window-title")
            };
            _window.OnClose += Close;

            _window.OnSequenceButtonPressed += _ => SendMessage(new SequenceButtonPressedMessage());
            _window.OnRepairButtonPressed += (args, btnRef) => SendMessage(new RepairButtonPressedMessage(btnRef.Index));
            _window.OnActivateButtonPressed += (args) => SendMessage(new ActivateButtonPressedMessage());
            _window.OnSpliceButtonPressed += (args, btnRef) => SendMessage(new GeneSpliceButtonPressedMessage(btnRef.Index));
            _window.OnCancelActivationButtonPressed += (args) => SendMessage(new CancelActivationButtonPressedMessage());
            _window.OnPrintReportButtonPressed += (args) => SendMessage(new PrintReportButtonPressedMessage());
            _window.OnStartActivationButtonPressed += (args, btnRef) => SendMessage(new StartActivationButtonPressedMessage(btnRef.Index));
            _window.OnUnusedBlockButtonPressed += (args, btnRef) => SendMessage(new UnusedBlockButtonPressedMessage(btnRef.Index));
            _window.OnUsedBlockButtonPressed += (args, btnRef) => SendMessage(new UsedBlockButtonPressedMessage(btnRef.Index));

            _window.OnXferToBeakerButtonPressed += (args) => SendMessage(new XferToBeakerButtonPressedMessage());
            _window.OnXferFromBeakerButtonPressed += (args) => SendMessage(new XferFromBeakerButtonPressedMessage());

            _window.OnFillBasePairButtonPressed += (args, ev) => SendMessage(ev);

            _window.SetActiveScreen(GeneticsConsoleScreen.Status);
            _window.SetStatusMessage(Loc.GetString("genetics-console-ui-window-ready-to-scan"), true);
            _window.SetGeneDirty();
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case GeneticsConsoleBoundUserInterfaceState msg:
                    UpdateConsoleWindow(msg);
                    break;
            }
        }

        private void DisplayStatusMessage(string message, bool scanReady = false)
        {
            if (_window == null)
                return;
            _window.SetActiveScreen(GeneticsConsoleScreen.Status);
            _window.SetStatusMessage(message, scanReady);
        }

        private void UpdateConsoleWindow(GeneticsConsoleBoundUserInterfaceState state)
        {
            var dirty = true;
            if (_window == null)
                return;
            _window.SetProgressBarStatus(state.PodStatus == PodStatus.ScanStarted, (float) state.TimeRemaining.Divide(state.TotalTime));
            _window.SetMutagenBufferLevel(state.MutagenLevel);
            
            if (!state.PodConnected)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-no-pod-connected"));
            }
            else if (!state.PodInRange)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-no-pod-in-range"));
            }
            else if (state.PodStatus == PodStatus.PodEmpty)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-no-patient"));
            }
            else if (state.PodStatus == PodStatus.PodOccupantDead)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-no-patient"));
            }
            else if (state.PodStatus == PodStatus.ScanStarted)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-scan-started"));
            }
            else if (state.PodStatus == PodStatus.PodOccupantAlive)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-ready-to-scan"), true);
            }
            else if (state.PodStatus == PodStatus.PodOccupantNoGenes)
            {
                DisplayStatusMessage(Loc.GetString("genetics-console-ui-window-no-genes"), true);
            }
            else
            {
                dirty = state.ForceUpdate;
                if (state.ActivationTargetGene != null)
                {
                    _window.SetActiveScreen(GeneticsConsoleScreen.Activation);
                    _window.SetTargetGene(state.ActivationTargetGene, state.KnownMutations, state.Puzzle);
                }
                else
                {
                    var patientName = (state.PodBodyUid.HasValue) ? _entityManager.GetComponent<MetaDataComponent>(state.PodBodyUid.Value).EntityName
                        : Loc.GetString("genetics-console-ui-window-patient-name-unknown");
                    _window.SetActiveScreen(GeneticsConsoleScreen.GeneRepair);
                    _window.SetGeneRepairPanel(patientName, state.SequencedGenes, state.KnownMutations, state.MutagenForSplice);
                } 
            }
            if (dirty) _window.SetGeneDirty();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_window != null)
            {
                _window.OnClose -= Close;
                //_window.CloneButton.OnPressed -= _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
            }
            _window?.Dispose();
        }
    }
}
