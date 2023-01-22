using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Shared class for injectors & syringes
    /// </summary>
    [NetworkedComponent, ComponentProtoName("DNASequencer")]
    public abstract class SharedDNASequencerComponent : Component
    {
        /// <summary>
        /// Component data used for net updates. Used by client for item status ui
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class DNASequencerComponentState : ComponentState
        {
            public List<Gene>? Sample { get; }
            public DNASequencerToggleMode CurrentMode { get; } = DNASequencerToggleMode.Sequence;

            public DNASequencerComponentState(List<Gene>? sample, SharedDNASequencerComponent.DNASequencerToggleMode currentMode)
            {
                Sample = sample;
                CurrentMode = currentMode;
            }
        }

        public enum DNASequencerToggleMode : byte
        {
            Sequence,
            Modify,
        }
    }
}
