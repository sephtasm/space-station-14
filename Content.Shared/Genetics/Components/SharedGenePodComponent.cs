using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Serialization;

namespace Content.Shared.Genetics.GenePod
{
    public abstract class SharedGenePodComponent : Component, IDragDropOn
    {
        [Serializable, NetSerializable]
        public enum GenePodVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum GenePodStatus
        {
            Off,
            Unoccupied,
            Occupied,
            Maintenance,
            Scanning,
        }

        public bool CanInsert(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<BodyComponent>(entity);
        }

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            return CanInsert(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
