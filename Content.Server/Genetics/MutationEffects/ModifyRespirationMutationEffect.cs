using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    /// <summary>
    /// Modifies the entity's capacity for respiration. Does not change the types of gases that are metabolized.
    /// Does not use ApplyMetabolicMultiplierEvent because we don't want to apply changes to all metabolic processes.
    /// </summary>
    [UsedImplicitly]
    public sealed class ModifyRespirationMutationEffect : MutationEffect
    {
        [DataField("cycleDelayMultiplier")]
        public float CycleDelayMultiplier = 1.0f;

        [DataField("saturationMultiplier")]
        public float SaturationMultiplier = 1.0f;

        [DataField("maxSaturationMultiplier")]
        public float MaxSaturationMultiplier = 1.0f;

        [DataField("minSaturationMultiplier")]
        public float MinSaturationMultiplier = 1.0f;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<RespiratorComponent>(uid, out var respiratorComponent))
            {
                var respiratorSystem = entityManager.System<RespiratorSystem>();
                respiratorSystem.ApplyRespirationModifer(uid, respiratorComponent, CycleDelayMultiplier, SaturationMultiplier,
                    MinSaturationMultiplier, MaxSaturationMultiplier);
            }
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<RespiratorComponent>(uid, out var respiratorComponent))
            {
                var respiratorSystem = entityManager.System<RespiratorSystem>();
                respiratorSystem.ApplyRespirationModifer(uid, respiratorComponent, CycleDelayMultiplier, SaturationMultiplier,
                    MinSaturationMultiplier, MaxSaturationMultiplier, true);
            }
        }
    }

}
