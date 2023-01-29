using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class MutationEffect
    {
        public abstract void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager);
        public abstract void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager);
    }

}
