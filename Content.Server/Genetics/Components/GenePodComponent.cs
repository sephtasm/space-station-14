using Content.Shared.Construction.Prototypes;
using Content.Shared.DragDrop;
using Content.Shared.Genetics.GenePod;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Genetics.GenePod
{
    [RegisterComponent]
    public sealed class GenePodComponent : SharedGenePodComponent
    {
        public const string ScannerPort = "GenePodReceiver";
        public ContainerSlot BodyContainer = default!;
        public EntityUid? ConnectedConsole;

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseRadiationDamageOnRepair = 3f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseGeneticDamageOnEdit = 5f;

        [DataField("machinePartDamageReduction", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartDamageReduction = "Manipulator";

        [DataField("partRatingDamageReductionMultiplier")]
        public float PartRatingDamageReductionMultiplier = 0.75f;

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }
    }
}
