using Content.Server.Body.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.MutationEffects
{
    [UsedImplicitly]
    public sealed class ChangeFactionMutationEffect : MutationEffect
    {
        [DataField("faction")]
        public string Faction = default!;

        public override void Apply(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<FactionComponent>(uid, out var factionComponent))
            {
                if (Faction != null)
                {
                    var factionSystem = entityManager.EntitySysManager.GetEntitySystem<FactionSystem>();
                    factionSystem.AddFaction(uid, Faction);
                }
            }
        }

        public override void Remove(EntityUid uid, string source, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            if (entityManager.TryGetComponent<FactionComponent>(uid, out var factionComponent))
            {
                if (Faction != null)
                {
                    var factionSystem = entityManager.EntitySysManager.GetEntitySystem<FactionSystem>();
                    factionSystem.RemoveFaction(uid, Faction);
                }
            }
        }

    }
}
