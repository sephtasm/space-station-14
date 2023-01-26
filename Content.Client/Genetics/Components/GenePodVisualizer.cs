using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using static Content.Shared.Genetics.GenePod.SharedGenePodComponent;

namespace Content.Client.Genetics.Components
{
    [UsedImplicitly]
    public sealed class GenePodVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(component.Owner);
            if (!component.TryGetData(GenePodVisuals.Status, out GenePodStatus status)) return;
            sprite.LayerSetVisible(GenePodVisualLayers.Screen, StatusToScreenVisibility(status));
            sprite.LayerSetVisible(GenePodVisualLayers.Pod, StatusToPodVisibility(status));
            sprite.LayerSetVisible(GenePodVisualLayers.Panel, StatusToPanelVisibility(status));
        }

        private bool StatusToPodVisibility(GenePodStatus status)
        {
            return (status == GenePodStatus.Occupied || status == GenePodStatus.Scanning);
        }

        private bool StatusToScreenVisibility(GenePodStatus status)
        {
            return (status == GenePodStatus.Scanning);
        }

        private bool StatusToPanelVisibility(GenePodStatus status)
        {
            return (status == GenePodStatus.Maintenance);
        }


        public enum GenePodVisualLayers : byte
        {
            Icon,
            Screen,
            Pod,
            Panel,
        }
    }
}
