using Content.Server.Body.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{

    [UsedImplicitly]
    public sealed class ReactToDamageMutationEffect : MutationEffect
    {
        [DataField("behavior")]
        public IThresholdBehavior Behavior = default!;

        [DataField("threshold")]
        public int Threshold = 15;

        [DataField("limit")]
        public float Limit = 100.0f;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<DestructibleComponent>(uid, out var destructibleComponent))
            {
                int num = Threshold;
                while(num < Limit)
                {
                    var behaviors = new List<IThresholdBehavior> { Behavior };
                    var trigger = new DamageTrigger();
                    trigger.Damage = num;
                    var dmg = new DamageThreshold(trigger, behaviors);
                    destructibleComponent.Thresholds.Add(dmg);

                    num += Threshold;
                }
            }
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<DestructibleComponent>(uid, out var destructibleComponent))
            {
                foreach (var threshold in destructibleComponent.Thresholds)
                {
                    if (threshold.Behaviors.Count == 1 && threshold.Behaviors[0] == Behavior) // this is dumb but it should mostly work
                        destructibleComponent.Thresholds.Remove(threshold);
                }
            }
        }
    }
}
