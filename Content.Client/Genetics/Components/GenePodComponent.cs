using Content.Shared.DragDrop;
using Content.Shared.Genetics.GenePod;
using Robust.Shared.GameObjects;

namespace Content.Client.Genetics.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGenePodComponent))]
    public sealed class GenePodComponent : SharedGenePodComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
