using Content.Shared.Damage;

namespace Content.Shared.Genetics
{

    [RegisterComponent]
    public sealed class DamageResistanceMutationComponent : Component
    {
        [DataField("modifiers", required: true)]
        public Dictionary<string, DamageModifierSet> Modifiers = new();
    }
}
