using System.Linq;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Content.Shared.Genetics.GeneticsConsole;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Content.Shared.Genetics;
using Robust.Client.Graphics;

namespace Content.Client.GeneticsConsole.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class GeneticsConsoleWindow : FancyWindow
    {
        public event Action<BaseButton.ButtonEventArgs>? OnSequenceButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, IndexedButton>? OnRepairButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, IndexedButton>? OnStartActivationButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, IndexedButton>? OnSpliceButtonPressed;
        public event Action<BaseButton.ButtonEventArgs>? OnActivateButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, IndexedButton>? OnUsedBlockButtonPressed;
        public event Action<BaseButton.ButtonEventArgs, IndexedButton>? OnUnusedBlockButtonPressed;
        public event Action<BaseButton.ButtonEventArgs>? OnCancelActivationButtonPressed;
        public event Action<BaseButton.ButtonEventArgs>? OnPrintReportButtonPressed;

        public event Action<BaseButton.ButtonEventArgs>? OnXferToBeakerButtonPressed;
        public event Action<BaseButton.ButtonEventArgs>? OnXferFromBeakerButtonPressed;


        public event Action<BaseButton.ButtonEventArgs, FillBasePairButtonPressedMessage>? OnFillBasePairButtonPressed;

        private bool _geneDirty = false;

        private int _prevUsedBlockCnt = 0;
        private int _prevUnusedBlockCnt = 0;
        private bool _modalOpen = false;

        private int _targetDNABlockIndex = 0;
        private int _targetPairIndex = 0;
        private bool _targetBaseTop = false;

        private static readonly Vector2 BlockSize = new(15, 15);

        private static readonly Dictionary<Base, Color> BaseToColor = new Dictionary<Base, Color>
            {
                { Base.A, Color.FromHex("#092142") },
                { Base.T, Color.FromHex("#732909") },
                { Base.G, Color.FromHex("#540939") },
                { Base.C, Color.FromHex("#204D07") },
                { Base.Unknown, Color.FromHex("#B81414") },
                { Base.Empty, Color.Transparent }
            };

        public GeneticsConsoleWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            BtnSequencing.OnPressed += a => OnSequenceButtonPressed?.Invoke(a);
            BtnActivateGene.OnPressed += a => OnActivateButtonPressed?.Invoke(a);
            BtnCancelActivation.OnPressed += a => OnCancelActivationButtonPressed?.Invoke(a);
            BtnPrint.OnPressed += a => OnPrintReportButtonPressed?.Invoke(a);

            BtnXferToBeaker.OnPressed += a => OnXferToBeakerButtonPressed?.Invoke(a);
            BtnXferFromBeaker.OnPressed += a => OnXferFromBeakerButtonPressed?.Invoke(a);

            BtnFillAsA.OnPressed += a => SendFillBasePairEvent(a, Base.A);
            BtnFillAsT.OnPressed += a => SendFillBasePairEvent(a, Base.T);
            BtnFillAsG.OnPressed += a => SendFillBasePairEvent(a, Base.G);
            BtnFillAsC.OnPressed += a => SendFillBasePairEvent(a, Base.C);
            BtnFillAsUnkown.OnPressed += a => SendFillBasePairEvent(a, Base.Unknown);

            BtnFillCancel.OnPressed += a => OnCancelFillBasePairButtonPressed(a);

            CTabContainer.SetTabTitle(0, Loc.GetString("genetics-console-ui-window-tab-gene-repair"));
            CTabContainer.SetTabTitle(1, Loc.GetString("genetics-console-ui-window-tab-innate-mutations"));
            CTabContainer.SetTabTitle(2, Loc.GetString("genetics-console-ui-window-tab-researched-mutations"));
        }

        public void SetGeneDirty()
        {
            _geneDirty = true;
        }

        private PanelContainer CreateBoxForBP(BasePair bp, bool top, int blockIndex, int pairIndex)
        {
            Base b = (top) ? bp.TopAssigned : bp.BotAssigned;
            var editable = top && bp.TopActual == Base.Unknown || !top && bp.BotActual == Base.Unknown;
            var container = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = BaseToColor[b] },
                MinSize = BlockSize,
                RectDrawClipMargin = 0,
                HorizontalExpand = true,
                HorizontalAlignment = HAlignment.Stretch
            };
            if (editable)
            {
                var bpButton = new FillBasePairButton(blockIndex, pairIndex, top)
                {
                    Text = SharedGenetics.BaseToChar[b],
                    MinSize = BlockSize,
                    HorizontalAlignment = HAlignment.Center,
                    HorizontalExpand = false

                };
                container.AddChild(bpButton);
                bpButton.OnPressed += e => OnOpenBasePairModalButton(e, bpButton);
            }
            else
            { 
                container.AddChild(new Label
                {
                    Text = SharedGenetics.BaseToChar[b],
                    MinSize = BlockSize,
                    HorizontalAlignment = HAlignment.Center,
                    HorizontalExpand = false
                });
            }
            return container;
        }

        private bool ShouldUpdateGenePuzzle(GenePuzzle puzzle)
        {
            if (_geneDirty) return true;
            if (puzzle.UsedBlocks.Count != _prevUsedBlockCnt) return true;
            if (puzzle.UnusedBlocks.Count != _prevUnusedBlockCnt) return true;
            return false;
        }

        public void OnOpenBasePairModalButton(BaseButton.ButtonEventArgs e, FillBasePairButton btn)
        {
            _targetBaseTop = btn.TopOrBottom;
            _targetDNABlockIndex = btn.BlockIndex;
            _targetPairIndex = btn.PairIndex;
            if (!_modalOpen)
            {
                _modalOpen = true;
                ActivationScreenMainBody.Modulate = Color.FromHex("#888888");
                FillInBlockOverlay.MouseFilter = MouseFilterMode.Stop;
                FillInBlockOverlay.Visible = true;
            }
        }
        public void OnCancelFillBasePairButtonPressed(BaseButton.ButtonEventArgs e)
        {
            CloseModal();
        }

        private void CloseModal()
        {
            if (_modalOpen)
            {
                _modalOpen = false;
                ActivationScreenMainBody.Modulate = Color.White;
                FillInBlockOverlay.MouseFilter = MouseFilterMode.Pass;
                FillInBlockOverlay.Visible = false;
                _geneDirty = true;
            }
        }

        public void SendFillBasePairEvent(BaseButton.ButtonEventArgs e, Base newBase)
        {
            var btnEvent = new FillBasePairButtonPressedMessage(newBase, _targetDNABlockIndex, _targetPairIndex, _targetBaseTop);
            OnFillBasePairButtonPressed?.Invoke(e, btnEvent);
            CloseModal();
        }

        public void SetTargetGene(Gene gene, Dictionary<long, string> knownMutations, GenePuzzle? puzzle)
        {
            if (puzzle == null || !ShouldUpdateGenePuzzle(puzzle)) return;

            _prevUsedBlockCnt = puzzle.UsedBlocks.Count;
            _prevUnusedBlockCnt = puzzle.UnusedBlocks.Count;
            _geneDirty = false;

            var geneTypeText = SharedGenetics.GetGeneTypeLoc(gene, knownMutations);
            ActivationTargetType.Text = Loc.GetString("genetics-console-ui-window-gene-block-type", ("type", geneTypeText));

            BtnActivateGene.Disabled = !SharedGenetics.CanSubmitPuzzle(puzzle);

            UnusedTargetBlocks.RemoveAllChildren();
            ActivationTargetBlocks.RemoveAllChildren();
            var usedBlockContainer = new BoxContainer
            {
                HorizontalExpand = true
            };
            var unusedBlockContainer = new BoxContainer
            {
                HorizontalExpand = true,

            };
            if (puzzle != null)
            {
                int i = 0;
                foreach (var block in puzzle.UnusedBlocks)
                {
                    var unusedBlockButton = new IndexedButton(i, "");
                    unusedBlockButton.OnPressed += e => OnUnusedBlockButtonPressed?.Invoke(e, unusedBlockButton);
                    var panelContainer = CreatePuzzleBlock(block, i);
                    unusedBlockButton.AddChild(panelContainer);
                    unusedBlockContainer.AddChild(unusedBlockButton);
                    i++;
                }

                int j = 0;
                foreach (var block in puzzle.UsedBlocks)
                {
                    var usedBlockButton = new IndexedButton(j, "");
                    usedBlockButton.OnPressed += e => OnUsedBlockButtonPressed?.Invoke(e, usedBlockButton);
                    var panelContainer = CreatePuzzleBlock(block, i+j);
                    usedBlockButton.AddChild(panelContainer);
                    usedBlockContainer.AddChild(usedBlockButton);
                    j++;
                }
            }
            UnusedTargetBlocks.AddChild(unusedBlockContainer);
            ActivationTargetBlocks.AddChild(usedBlockContainer);
        }

        private PanelContainer CreatePuzzleBlock(List<BasePair> block, int blockIndex)
        {
            var panelContainer = new PanelContainer
            {
                Margin = new Thickness(5, 2),
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#404046"),
                    BorderColor = Color.FromHex("#86868d"),
                    BorderThickness = new Thickness(1)
                }
            };
            var gridContainer = new GridContainer
            {
                Columns = block.Count,
                Rows = 2,
                HSeparationOverride = 2,
                VSeparationOverride = 2
            };

            int i = 0;
            foreach (var pair in block)
            {
                var topBox = CreateBoxForBP(pair, true, blockIndex, i);
                gridContainer.AddChild(topBox);

                var botBox = CreateBoxForBP(pair, false, blockIndex, i);
                gridContainer.AddChild(botBox);
                i++;
            }
            panelContainer.AddChild(gridContainer);
            return panelContainer;
        }

        public void SetGeneRepairPanel(string patientName, List<GeneDisplay> geneDisplays, Dictionary<long, string> knownMutations, bool mutagenForSplice)
        {
            if (!_geneDirty) return;
            _geneDirty = false;

            PatientName.Text = Loc.GetString("genetics-console-ui-window-patient-name", ("name", patientName));
            GeneRepairTable.RemoveAllChildren();
            MutationTable.RemoveAllChildren();
            var presentMutations = new HashSet<long>();
            var index = 0;
            foreach (var geneDisplay in geneDisplays)
            {
                BoxContainer repairButtonContainer = new BoxContainer { MinWidth = 100 };
                BoxContainer geneOverviewRow = CreateGeneTableRow(knownMutations, geneDisplay, repairButtonContainer);
                if (geneDisplay.Gene.Damaged)
                {
                    geneOverviewRow.Modulate = new Color(255, 0, 0);
                    var repairButton = new IndexedButton(index, Loc.GetString("genetics-console-ui-window-gene-repair-prompt"));
                    repairButton.OnPressed += e => OnRepairButtonPressed?.Invoke(e, repairButton);
                    repairButtonContainer.AddChild(repairButton);
                }
                GeneRepairTable.AddChild(geneOverviewRow);

                if (geneDisplay.Gene.Type == GeneType.Mutation)
                {
                    BoxContainer activateButtonContainer = new BoxContainer { MinWidth = 100 };
                    BoxContainer mutationRow = CreateGeneTableRow(knownMutations, geneDisplay, activateButtonContainer);
                    presentMutations.Add(geneDisplay.Gene.Blocks[0].Value);
                    if (!geneDisplay.Gene.Active)
                    {
                        var activateButton = new IndexedButton(index, Loc.GetString("genetics-console-ui-window-gene-activate-prompt"));
                        activateButton.OnPressed += e => OnStartActivationButtonPressed?.Invoke(e, activateButton);
                        activateButtonContainer.AddChild(activateButton);
                    }
                    else
                    {
                        activateButtonContainer.AddChild(new Label
                        {
                            Text = Loc.GetString("genetics-console-ui-window-gene-mutation-status-activated"),
                            HorizontalExpand = true
                        });
                    }
                    MutationTable.AddChild(mutationRow);
                }

                ++index;
            }
            GeneSpliceTable.RemoveAllChildren();
            if (knownMutations.Count == 0)
            {
                var spliceRow = new BoxContainer();
                spliceRow.AddChild(new Label
                {
                    Text = Loc.GetString("genetics-console-ui-window-gene-splice-no-known"),
                    HorizontalExpand = true
                });

                GeneSpliceTable.AddChild(spliceRow);
            }
            else
            {
                foreach (var mutation in knownMutations)
                {
                    var isPresent = presentMutations.Contains(mutation.Key);

                    var spliceRow = new BoxContainer();
                    var geneSpliceButton = new IndexedButton((int) mutation.Key, Loc.GetString("genetics-console-ui-window-gene-splice-prompt"));
                    geneSpliceButton.OnPressed += e => OnSpliceButtonPressed?.Invoke(e, geneSpliceButton);
                    geneSpliceButton.Disabled = (!mutagenForSplice || isPresent);
                    spliceRow.AddChild(geneSpliceButton);

                    if (isPresent)
                    {
                        spliceRow.AddChild(new Label
                        {
                            Text = Loc.GetString("genetics-console-ui-window-gene-splice-already-present"),
                        });
                    }
                    else if (!mutagenForSplice)
                    {
                        spliceRow.AddChild(new Label
                        {
                            Text = Loc.GetString("genetics-console-ui-window-gene-splice-not-enough-mutagen"),
                        });
                    }
                    

                    spliceRow.AddChild(new Label
                    {
                        Text = mutation.Value,
                        HorizontalExpand = true,
                        Align = Label.AlignMode.Center
                    });

                    GeneSpliceTable.AddChild(spliceRow);
                }
            }
        }

        private BoxContainer CreateGeneTableRow(Dictionary<long, string> knownMutations, GeneDisplay geneDisplay, BoxContainer buttonContainer)
        {
            var geneBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };
            geneBox.AddChild(buttonContainer);

            var geneTypeText = SharedGenetics.GetGeneTypeLoc(geneDisplay.Gene, knownMutations);
            geneBox.AddChild(new Label
            {
                Text = Loc.GetString("genetics-console-ui-window-gene-block-type", ("type", geneTypeText)),
                HorizontalExpand = true
            });

            geneBox.AddChild(new Label
            {
                Text = geneDisplay.Display,
                HorizontalExpand = true,
                ClipText = true
            });
            return geneBox;
        }

        public void SetProgressBarStatus(bool visible, float percentage)
        {
            ProgressBar.Visible = visible;
            ProgressBar.Value = percentage;
        }

        public void SetMutagenBufferLevel(float percentage)
        {
            MutagenBufferLevel.Value = percentage;
        }

        public void SetActiveScreen(GeneticsConsoleScreen screen)
        {
            switch (screen)
            {
                case GeneticsConsoleScreen.Status:
                    IntroScreen.Visible = true;
                    ModificationScreen.Visible = false;
                    ActivationScreen.Visible = false;
                    break;
                case GeneticsConsoleScreen.GeneRepair:
                    IntroScreen.Visible = false;
                    ModificationScreen.Visible = true;
                    ActivationScreen.Visible = false;
                    break;
                case GeneticsConsoleScreen.Activation:
                    IntroScreen.Visible = false;
                    ModificationScreen.Visible = false;
                    ActivationScreen.Visible = true;
                    break;
            }
        }

        public void SetStatusMessage(string msg, bool btnEnabled)
        {
            StatusMessage.Text = msg;
            BtnSequencing.Disabled = !btnEnabled;
        }

        public sealed class FillBasePairButton : Button
        {
            public int BlockIndex { get; }
            public int PairIndex { get; }
            public bool TopOrBottom { get; }
            public FillBasePairButton(int blockIndex, int pairIndex, bool topOrBottom)
            {
                BlockIndex = blockIndex;
                PairIndex = pairIndex;
                TopOrBottom = topOrBottom;
            }
        }
        public sealed class IndexedButton : Button
        {
            public int Index { get; }

            public IndexedButton(int index, string text)
            {
                Index = index;
                Text = text;
            }
        }
    }
}

