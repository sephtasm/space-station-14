using Content.Shared.Damage;
using System.Linq;

namespace Content.Shared.Genetics
{

    [RegisterComponent]
    public sealed class MutationsComponent : Component
    {
        [DataField("activeMutationIDs")]
        public HashSet<string> ActiveMutationIDs = new();

        [DataField("activeMutationEffects")]
        public Dictionary<string, HashSet<string>> ActiveMutationEffectsBySource = new();

        public HashSet<string> AllActiveMutationEffects { get => ActiveMutationEffectsBySource.Values.SelectMany(x => x).ToHashSet(); }

        [DataField("modifiers")]
        public Dictionary<string, DamageModifierSet> DamageModifiers = new();

        [DataField("lowPressureResistances")]
        public Dictionary<string, float> LowPressureResistances = new();

        /// <summary>
        /// Any blood reagent that the parent entity previously had before a mutation was applied.
        /// This will be restored if the entity loses the mutation.
        /// </summary>
        [DataField("suppressedBloodReagent", required: true)]
        public string SuppressedBloodReagent = default!;
    }
}
