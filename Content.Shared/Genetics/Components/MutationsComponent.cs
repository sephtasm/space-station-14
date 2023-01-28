using Content.Shared.Damage;

namespace Content.Shared.Genetics
{

    [RegisterComponent]
    public sealed class MutationsComponent : Component
    {
        [DataField("modifiers", required: true)]
        public Dictionary<string, DamageModifierSet> DamageModifiers = new();

        [DataField("lowPressureResistances", required: true)]
        public Dictionary<string, float> LowPressureResistances = new();
    }
}
