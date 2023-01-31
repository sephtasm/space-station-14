using Content.Shared.Chemistry.Reagent;
using Content.Shared.Genetics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed class MobHasMutationCondition : ReagentEffectCondition
    {
        [DataField("mutationEffect", required:true)]
        public string MutationEffect = default!;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out MutationsComponent? mutations))
            {
                return mutations.AllActiveMutationEffects.Contains(MutationEffect);
            }

            return false;
        }
    }
}

