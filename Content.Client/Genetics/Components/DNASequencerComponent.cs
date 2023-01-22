using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Genetics.Components
{
    /// <summary>
    /// Client behavior for dna sequencers
    /// </summary>
    [RegisterComponent]
    public sealed class DNASequencerComponent : SharedDNASequencerComponent
    {
        public DNASequencerToggleMode CurrentMode = DNASequencerToggleMode.Sequence;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded;
    }
}
