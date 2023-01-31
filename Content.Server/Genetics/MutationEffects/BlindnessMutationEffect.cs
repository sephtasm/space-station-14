using Content.Shared.Eye.Blinding;
using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class BlindnessMutationEffect : MutationEffect
    {
        [DataField("severity")]
        public int Severity = 8; // 8 is the magic number

        public override void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var blindingSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>();
            blindingSystem.AdjustEyeDamage(uid, Severity);
        }

        public override void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var blindingSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>();
            blindingSystem.AdjustEyeDamage(uid, -Severity); // reverse the damage
        }
    }
}
