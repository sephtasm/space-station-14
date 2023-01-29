using Content.Shared.Damage;
using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{

    [UsedImplicitly]
    public sealed class DamageResistanceMutationEffect : MutationEffect
    {
        /// <summary>
        ///     Source of the effect. An entity can only benefit from one effect per source.
        /// </summary>
        [DataField("damageTypeName", required: true)]
        public string DamageTypeName = default!;

        [DataField("coefficient")]
        public float Coefficient = 1.0f;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            entityManager.EnsureComponent<MutationsComponent>(uid, out var mutations);
            DamageModifierSet newSet = new DamageModifierSet();
            newSet.Coefficients[DamageTypeName] = Coefficient;

            mutations.DamageModifiers[source] = newSet;
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<MutationsComponent>(uid, out var damageResistanceMutation))
            {
                damageResistanceMutation.DamageModifiers.Remove(source);
            }
        }
    }

}
