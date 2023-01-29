using Content.Server.Body.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Genetics;
using JetBrains.Annotations;
using Linguini.Syntax.Ast;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Genetics.MutationEffects
{
    /// <summary>
    /// Modify the target's bloodstream with new values. Keeps track of the old values so that they can be restored if the
    /// mutation is removed. This might have weird effects if multiple things are modifying the bloodstream.
    /// More could be added, or there could be a BloodStreamPrototype to change all values at once.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChangeBloodMutationEffect : MutationEffect
    {
        [DataField("bloodReagent")]
        public string? BloodReagent = null;

        [DataField("bloodRefreshBonusMultiplier")]
        public float BloodRefreshBonusMultiplier = 1.0f;

        [DataField("maxBleedAmountMultiplier")]
        public float MaxBleedAmountMultiplier = 1.0f;

        [DataField("bloodMaxVolumeMultiplier")]
        public float BloodMaxVolumeMultiplier = 1.0f;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            entityManager.EnsureComponent<MutationsComponent>(uid, out var mutations);
            if (entityManager.TryGetComponent<BloodstreamComponent>(uid, out var bloodstreamComponent))
            {
                if (BloodReagent != null)
                {
                    mutations.SuppressedBloodReagent = bloodstreamComponent.BloodReagent;
                    SwapBlood(bloodstreamComponent, mutations.SuppressedBloodReagent, BloodReagent);
                }
                bloodstreamComponent.BloodRefreshAmount *= BloodRefreshBonusMultiplier;
                bloodstreamComponent.MaxBleedAmount *= MaxBleedAmountMultiplier;
                bloodstreamComponent.BloodMaxVolume *= BloodMaxVolumeMultiplier;
            }
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<BloodstreamComponent>(uid, out var bloodstreamComponent) &&
                entityManager.TryGetComponent<MutationsComponent>(uid, out var mutations))
            {
                // to catch cases when something else changed it after the mutation was applied
                if (BloodReagent != null && BloodReagent == bloodstreamComponent.BloodReagent)
                {
                    bloodstreamComponent.BloodReagent = mutations.SuppressedBloodReagent;
                    SwapBlood(bloodstreamComponent, BloodReagent, mutations.SuppressedBloodReagent);
                }
                bloodstreamComponent.BloodRefreshAmount /= BloodRefreshBonusMultiplier;
                bloodstreamComponent.MaxBleedAmount /= MaxBleedAmountMultiplier;
                bloodstreamComponent.BloodMaxVolume /= BloodMaxVolumeMultiplier;
            }
        }

        private void SwapBlood(BloodstreamComponent bloodstream, string oldBlood, string newBlood)
        {
            bloodstream.BloodReagent = newBlood;
            var bloodAmount = bloodstream.BloodSolution.GetReagentQuantity(oldBlood);
            bloodstream.BloodSolution.RemoveReagent(oldBlood, bloodAmount);
            bloodstream.BloodSolution.AddReagent(newBlood, bloodAmount);
        }
    }
}
