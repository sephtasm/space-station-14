using Content.Shared.Construction.Prototypes;
using Content.Shared.DragDrop;
using Content.Shared.Genetics.GenePod;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Genetics.GenePod
{
    [RegisterComponent]
    public sealed class GenePodComponent : SharedGenePodComponent
    {
        public const string PodPort = "GenePodReceiver";
        public ContainerSlot BodyContainer = default!;
        
        public EntityUid? ConnectedConsole;

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseRadiationDamageOnRepair = 3f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseGeneticDamageOnEdit = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float AdditionalGeneticDamageOnFailure = 3f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float DamageReductionMultiplier = 1f;

        [DataField("lastScannedBody")]
        public EntityUid? LastScannedBody = null;

        [DataField("sequencingDuration", customTypeSerializer: typeof(TimespanSerializer))]
        public TimeSpan SequencingDuration = TimeSpan.FromSeconds(1);

        /// <summary>
        /// A mulitplier on the duration of sequencing.
        /// Used for machine upgrading.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SequencingDurationMulitplier = 1;

        /// <summary>
        /// The machine part that modifies sequencing duration.
        /// </summary>
        [DataField("machinePartSequencingDuration", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartSequencingDuration = "ScanningModule";

        /// <summary>
        /// The modifier raised to the part rating to determine the duration multiplier.
        /// </summary>
        [DataField("partRatingSequencingDurationMultiplier")]
        public float PartRatingSequencingDurationMultiplier = 0.75f;

        [DataField("machinePartDamageReduction", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartDamageReduction = "Manipulator";

        [DataField("partRatingDamageReductionMultiplier")]
        public float PartRatingDamageReductionMultiplier = 0.80f;

        [DataField("startTime", customTypeSerializer: typeof(TimespanSerializer))]
        public TimeSpan StartTime;

        [DataField("scanning")]
        public bool Scanning;

        [DataField("sequencingFinishedSound")]
        public readonly SoundSpecifier SequencingFinishedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

        [DataField("repairGeneSound")]
        public readonly SoundSpecifier RepairGeneSound = new SoundPathSpecifier("/Audio/Items/Medical/generic_healing_end.ogg");

        [DataField("editGeneSuccessSound")]
        public readonly SoundSpecifier EditGeneSuccessSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");

        [DataField("editGeneFailedSound")]
        public readonly SoundSpecifier EditGeneFailedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }
    }
}
