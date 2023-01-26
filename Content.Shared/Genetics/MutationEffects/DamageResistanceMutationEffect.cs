using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics.MutationEffects
{

    public abstract class DamageResistanceMutationEffect : MutationEffect
    {
        protected abstract string ResistanceDamageType { get; }

        protected abstract string DamageModifierSetName { get; }
        public override void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager, float strength)
        {
            if (!entityManager.TryGetComponent<DamageResistanceMutationComponent>(uid, out var damageResistanceMutation))
            {
                damageResistanceMutation = entityManager.AddComponent<DamageResistanceMutationComponent>(uid);
            }
            if (prototypeManager.TryIndex<DamageModifierSetPrototype>(DamageModifierSetName, out var modifier))
            {
                DamageModifierSet newSet = new DamageModifierSet();
                newSet.Coefficients[ResistanceDamageType] = 1 - (modifier.Coefficients[ResistanceDamageType] * strength);

                damageResistanceMutation.Modifiers[DamageModifierSetName] = newSet;
            }
        }

        public override void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager, float strength)
        {
            if (!entityManager.TryGetComponent<DamageResistanceMutationComponent>(uid, out var damageResistanceMutation))
            {
                damageResistanceMutation = entityManager.AddComponent<DamageResistanceMutationComponent>(uid);
                damageResistanceMutation.Modifiers.Remove(DamageModifierSetName);
            }
        }
    }

    [UsedImplicitly]
    public sealed class HeatResistanceMutationEffect : DamageResistanceMutationEffect
    {
        protected override string ResistanceDamageType { get; } = "Heat";
        protected override string DamageModifierSetName { get; } = "HeatResistanceMutation";
    }
}
