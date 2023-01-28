using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class GiveActionMutationEffect : MutationEffect
    {

        [DataField("worldActions", customTypeSerializer: typeof(PrototypeIdListSerializer<WorldTargetActionPrototype>))]
        public readonly List<string> WorldActions = new();

        public override void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var actionsSystem = entityManager.System<SharedActionsSystem>();
            var actionTypes = new List<ActionType>();
            foreach (var action in WorldActions)
            {
                var actionType = new WorldTargetAction(prototypeManager.Index<WorldTargetActionPrototype>(action));
                actionTypes.Add(actionType);
            }
            actionsSystem.AddActions(uid, actionTypes, null);
        }

        public override void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var actionsSystem = entityManager.System<SharedActionsSystem>();
            var actionTypes = new List<ActionType>();
            foreach (var action in WorldActions)
            {
                var actionType = new WorldTargetAction(prototypeManager.Index<WorldTargetActionPrototype>(action));
                actionTypes.Add(actionType);
            }
            actionsSystem.RemoveActions(uid, actionTypes, null);
        }
    }

}
