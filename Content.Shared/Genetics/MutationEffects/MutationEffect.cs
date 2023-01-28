using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics.MutationEffects
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class MutationEffect
    {
        public abstract void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager);
        public abstract void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager);
    }

}
