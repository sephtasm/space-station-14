using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class LowPressureResistMutationEffect : MutationEffect
    {

        [DataField("lowPressureMultiplier", required: true)]
        public int LowPressureMultiplier = 1;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (!entityManager.TryGetComponent<MutationsComponent>(uid, out var mutationsComponent))
            {
                mutationsComponent = entityManager.AddComponent<MutationsComponent>(uid);
            }
            mutationsComponent.LowPressureResistances[source] = LowPressureMultiplier;
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<MutationsComponent>(uid, out var mutationsComponent))
            {
                mutationsComponent.LowPressureResistances.Remove(source);
            }
        }
    }

}
