using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class BlindnessMutationEffect : MutationEffect
    {
        [DataField("severity")]
        public int Severity = 8; // 8 is the magic number

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var blindingSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>();
            blindingSystem.AdjustEyeDamage(uid, Severity);
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var blindingSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>();
            blindingSystem.AdjustEyeDamage(uid, -Severity); // reverse the damage
        }
    }
}
