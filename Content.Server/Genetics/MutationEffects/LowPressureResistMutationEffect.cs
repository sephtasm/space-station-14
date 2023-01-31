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

        public override void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            mutationsComponent.LowPressureResistances[source] = LowPressureMultiplier;
        }

        public override void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            mutationsComponent.LowPressureResistances.Remove(source);
        }
    }

}
