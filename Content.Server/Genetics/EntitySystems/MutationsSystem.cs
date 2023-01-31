using Content.Server.Administration.Logs;
using Content.Server.Atmos;
using Content.Shared.Damage;
using Content.Shared.Genetics;

namespace Content.Server.Genetics.EntitySystems;

public sealed class MutationsSystem : EntitySystem
{

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly GeneticsSystem _genetics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MutationsComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<MutationsComponent, LowPressureEvent>(OnLowPressureModify);
    }

    private void OnDamageModify(EntityUid uid, MutationsComponent mutations, DamageModifyEvent ev)
    {
        // apply any resistances to damage granted by mutations
        var damage = ev.Damage;
        foreach (var set in mutations.DamageModifiers.Values)
        {
            damage = DamageSpecifier.ApplyModifierSet(damage, set);
        }
        ev.Damage = damage;
    }

    private void OnLowPressureModify(EntityUid uid, MutationsComponent mutations, LowPressureEvent ev)
    {
        // take the highest value from all mutations
        float multiplier = 1.0f;
        foreach (var value in mutations.LowPressureResistances.Values)
        {
            multiplier = Math.Max(value, multiplier);
        }
        ev.Multiplier *= multiplier;
    }

}
