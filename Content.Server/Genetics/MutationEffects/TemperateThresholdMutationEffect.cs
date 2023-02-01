using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class TemperateThresholdMutationEffect : MutationEffect
    {

        [DataField("heatDamageThresholdMultipliers", required: true)]
        public float HeatDamageThresholdMultiplier = 1.0f;

        [DataField("coldDamageThresholdMultipliers", required: true)]
        public float ColdDamageThresholdMultiplier = 1.0f;

        protected override void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<TemperatureComponent>(uid, out var temperatureComponent))
            {
                temperatureComponent.HeatDamageThreshold *= HeatDamageThresholdMultiplier;
                temperatureComponent.ColdDamageThreshold *= ColdDamageThresholdMultiplier;
            }
        }

        protected override void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<TemperatureComponent>(uid, out var temperatureComponent))
            {
                temperatureComponent.HeatDamageThreshold /= HeatDamageThresholdMultiplier;
                temperatureComponent.ColdDamageThreshold /= ColdDamageThresholdMultiplier;
            }
        }
    }

}
