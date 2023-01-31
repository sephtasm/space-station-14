using Content.Shared.Genetics;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Genetics.MutationEffects
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class MutationEffect
    {
        public string EffectName { get => GetType().Name; }
        public void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            entityManager.EnsureComponent<MutationsComponent>(uid, out var mutations);
            var activeEffects = mutations.ActiveMutationEffectsBySource.GetOrNew(source);
            if (activeEffects.Add(EffectName))
                DoApply(uid, source, mutations, entityManager, prototypeManager);
        }
        public void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            entityManager.EnsureComponent<MutationsComponent>(uid, out var mutations);
            if (mutations.ActiveMutationEffectsBySource.TryGetValue(source, out var activeEffects))
            {
                activeEffects.Remove(EffectName);
                if (activeEffects.Count == 0) mutations.ActiveMutationEffectsBySource.Remove(source);
                DoRemove(uid, source, mutations, entityManager, prototypeManager);
            }
        }
        protected abstract void DoApply(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager);
        protected abstract void DoRemove(EntityUid uid, string source, MutationsComponent mutationsComponent, IEntityManager entityManager, IPrototypeManager prototypeManager);
    }

}
