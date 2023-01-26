using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Components;
using Content.Shared.Genetics.GeneticsConsole;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Genetics.Components
{
    [RegisterComponent]
    public sealed class GeneticsConsoleComponent : SharedGeneticsConsoleComponent
    {
        [DataField("podPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public readonly string PodPort = "GenePodSender";

        [ViewVariables]
        public EntityUid? GenePod = null;

        [ViewVariables]
        public Dictionary<long, string> ResearchedMutations = new();

        [ViewVariables]
        public Gene? TargetActivationGene = null;

        public GenePuzzle? Puzzle = null;

        /// Maximum distance between console and one if its machines
        [DataField("maxDistance")]
        public float MaxDistance = 4f;

        public bool GenePodInRange = true;
    }
}
