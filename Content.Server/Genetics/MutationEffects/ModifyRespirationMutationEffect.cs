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
    /// Some things are controlled by the lungs or the reagent effect system.
    /// </summary>
    [UsedImplicitly]
    public sealed class ModifyRespirationMutationEffect : MutationEffect
    {
        /// <summary>
        /// This controls the speed at which the respirator's saturation level is updated. Changing this has unusual effects.
        /// </summary>
        [DataField("cycleDelayMultiplier")]
        public float CycleDelayMultiplier = 1.0f;

        [DataField("saturationMultiplier")]
        public float SaturationMultiplier = 1.0f;

        [DataField("maxSaturationMultiplier")]
        public float MaxSaturationMultiplier = 1.0f;

        [DataField("minSaturationMultiplier")]
        public float MinSaturationMultiplier = 1.0f;

        [DataField("suffocationDamageMultiplier")]
        public float SuffocationDamageMultiplier = 1.0f;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<RespiratorComponent>(uid, out var respiratorComponent))
            {
                var respiratorSystem = entityManager.System<RespiratorSystem>();
                respiratorSystem.ApplyRespirationModifer(uid, respiratorComponent, CycleDelayMultiplier, SaturationMultiplier,
                    MinSaturationMultiplier, MaxSaturationMultiplier);
                respiratorSystem.ApplySuffocationDamageMultiplier(uid, respiratorComponent, SuffocationDamageMultiplier);
            }
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<RespiratorComponent>(uid, out var respiratorComponent))
            {
                var respiratorSystem = entityManager.System<RespiratorSystem>();
                respiratorSystem.ApplyRespirationModifer(uid, respiratorComponent, CycleDelayMultiplier, SaturationMultiplier,
                    MinSaturationMultiplier, MaxSaturationMultiplier, true);
                respiratorSystem.ApplySuffocationDamageMultiplier(uid, respiratorComponent, SuffocationDamageMultiplier, true);
            }
        }
    }

}
