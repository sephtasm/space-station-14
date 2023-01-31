using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class MutationEffect
    {
        public void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            entityManager.EnsureComponent<MutationsComponent>(uid, out var mutations);
            if (mutations.ActiveMutationIDs.Add(source))
            {
                DoApply(uid, source, mutations, entityManager, prototypeManager);
            }
        }
        public void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            entityManager.EnsureComponent<MutationsComponent>(uid, out var mutations);
            if (mutations.ActiveMutationIDs.Remove(source))
            {
                DoRemove(uid, source, mutations, entityManager, prototypeManager);
            }
        }
        public abstract void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager);
        public abstract void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager);
    }

}
