using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Genetics.MutationEffects
{

    [UsedImplicitly]
    public sealed class DamageResistanceMutationEffect : MutationEffect
    {
        /// <summary>
        ///     Source of the effect. An entity can only benefit from one effect per source.
        /// </summary>
        [DataField("source", required: true)]
        public string Source = default!;

        /// <summary>
        ///     Source of the effect. An entity can only benefit from one effect per source.
        /// </summary>
        [DataField("damageTypeName", required: true)]
        public string DamageTypeName = default!;

        [DataField("coefficient")]
        public float Coefficient = 1.0f;

        public override void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (!entityManager.TryGetComponent<MutationsComponent>(uid, out var damageResistanceMutation))
            {
                damageResistanceMutation = entityManager.AddComponent<MutationsComponent>(uid);
            }
            DamageModifierSet newSet = new DamageModifierSet();
            newSet.Coefficients[DamageTypeName] = Coefficient;

            damageResistanceMutation.DamageModifiers[Source] = newSet;
        }

        public override void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<MutationsComponent>(uid, out var damageResistanceMutation))
            {
                damageResistanceMutation.DamageModifiers.Remove(Source);
            }
        }
    }

}
