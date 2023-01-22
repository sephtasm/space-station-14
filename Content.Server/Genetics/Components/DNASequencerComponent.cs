using System.Threading;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using static Content.Server.Genetics.GeneticSequenceComponent;

namespace Content.Server.Genetics.Components
{
    /// <summary>
    /// Server behavior for dna sequencer. Supports both
    /// sampling and modification of dna.
    /// </summary>
    [RegisterComponent]
    public sealed class DNASequencerComponent : SharedDNASequencerComponent
    {
        /// <summary>
        /// Injection delay (seconds) when the target is a mob.
        /// </summary>
        /// <remarks>
        /// The base delay has a minimum of 1 second, but this will still be modified if the target is incapacitated or
        /// in combat mode.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 5;

        [DataField("sample")]
        public List<Gene>? Sample = null;

        /// <summary>
        ///     Token for interrupting a do-after action (e.g., injection another player). If not null, implies
        ///     component is currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;

        [DataField("toggleState")] private DNASequencerToggleMode _toggleState = DNASequencerToggleMode.Sequence;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. Containers must have the
        /// right SolutionCaps to support injection/drawing. For InjectOnly injectors this should
        /// only ever be set to Inject
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public DNASequencerToggleMode ToggleState
        {
            get => _toggleState;
            set
            {
                _toggleState = value;
                Dirty();
            }
        }
    }
}
