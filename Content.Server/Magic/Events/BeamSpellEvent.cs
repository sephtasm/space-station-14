using Content.Shared.Actions;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

public sealed class BeamSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<HitscanPrototype>))]
    public string Prototype = default!;

    /// <summary>
    /// Gets the targeted spawn positions; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")] public MagicSpawnData Pos = new TargetCasterPos();
}
