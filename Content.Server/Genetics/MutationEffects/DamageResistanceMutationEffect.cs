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

        public override void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            DamageModifierSet newSet = new DamageModifierSet();
            newSet.Coefficients[DamageTypeName] = Coefficient;

            mutationsComponent.DamageModifiers[source] = newSet;
        }

        public override void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            mutationsComponent.DamageModifiers.Remove(source);
        }
    }

}
