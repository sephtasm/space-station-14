using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class LowPressureResistMutationEffect : MutationEffect
    {

        /// <summary>
        ///     Source of the effect. An entity can only benefit from one effect per source.
        /// </summary>
        [DataField("source", required: true)]
        public string Source = default!;

        [DataField("lowPressureMultiplier", required: true)]
        public int LowPressureMultiplier = 1;

        public override void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (!entityManager.TryGetComponent<MutationsComponent>(uid, out var mutationsComponent))
            {
                mutationsComponent = entityManager.AddComponent<MutationsComponent>(uid);
            }
            mutationsComponent.LowPressureResistances[Source] = LowPressureMultiplier;
        }

        public override void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<MutationsComponent>(uid, out var mutationsComponent))
            {
                mutationsComponent.LowPressureResistances.Remove(Source);
            }
        }
    }

}
