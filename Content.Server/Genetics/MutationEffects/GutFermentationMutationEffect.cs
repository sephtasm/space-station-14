using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class GutFermentationMutationEffect : MutationEffect
    {

        protected override void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
        }

        protected override void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
        }
    }

}
