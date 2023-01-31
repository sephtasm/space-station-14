using Content.Shared.Damage;

namespace Content.Shared.Genetics
{

    [RegisterComponent]
    public sealed class MutationsComponent : Component
    {
        [DataField("activeMutationIDs")]
        public HashSet<string> ActiveMutationIDs = new();

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
