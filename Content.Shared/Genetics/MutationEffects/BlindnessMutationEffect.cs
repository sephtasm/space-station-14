using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics.MutationEffects
{
    public sealed class BlindnessMutationEffect : MutationEffect
    {
        [DataField("severity")]
        public int Severity = 8;

        public override void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var blindingSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>();
            blindingSystem.AdjustEyeDamage(uid, Severity); // 8 is the magic number
        }

        public override void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var blindingSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>();
            blindingSystem.AdjustEyeDamage(uid, -Severity); // reverse the damage
        }
    }
}
