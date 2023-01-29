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

        [DataField("entityTargetActions", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityTargetActionPrototype>))]
        public readonly List<string> EntityTargetActions = new();

        [DataField("worldActions", customTypeSerializer: typeof(PrototypeIdListSerializer<WorldTargetActionPrototype>))]
        public readonly List<string> WorldActions = new();

        [DataField("instantActions", customTypeSerializer: typeof(PrototypeIdListSerializer<InstantActionPrototype>))]
        public readonly List<string> InstantActions = new();

        public override void Apply(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var actionsSystem = entityManager.System<SharedActionsSystem>();
            var actionTypes = GetAllActions(entityManager, prototypeManager);
            actionsSystem.AddActions(uid, actionTypes, null);
        }

        public override void Remove(EntityUid uid, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var actionsSystem = entityManager.System<SharedActionsSystem>();
            var actionTypes = GetAllActions(entityManager, prototypeManager);
            actionsSystem.RemoveActions(uid, actionTypes, null);
        }

        private List<ActionType> GetAllActions(IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            var actionTypes = new List<ActionType>();
            foreach (var action in EntityTargetActions)
            {
                var actionType = new EntityTargetAction(prototypeManager.Index<EntityTargetActionPrototype>(action));
                actionTypes.Add(actionType);
            }
            foreach (var action in WorldActions)
            {
                var actionType = new WorldTargetAction(prototypeManager.Index<WorldTargetActionPrototype>(action));
                actionTypes.Add(actionType);
            }
            foreach (var action in InstantActions)
            {
                var actionType = new InstantAction(prototypeManager.Index<InstantActionPrototype>(action));
                actionTypes.Add(actionType);
            }
            return actionTypes;
        }
    }

}
