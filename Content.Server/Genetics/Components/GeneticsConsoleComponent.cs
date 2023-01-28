using Robust.Shared.Prototypes;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Components;
using Content.Shared.Genetics.GeneticsConsole;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.Genetics.Components
{
    [RegisterComponent]
    public sealed class GeneticsConsoleComponent : SharedGeneticsConsoleComponent
    {
        [DataField("podPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public readonly string PodPort = "GenePodSender";

        /// <summary>
        /// The entity spawned by a report.
        /// </summary>
        [DataField("reportEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ReportEntityId = "Paper";

        [DataField("printReportSound")]
        public readonly SoundSpecifier PrintReportSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

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
