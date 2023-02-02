using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems
{
    [UsedImplicitly]
    public sealed class QueuedSpawnSystem : EntitySystem
    {
        private readonly Queue<(string?, MapCoordinates, Delegate?)> _queuedEntitySpawns = new();

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_queuedEntitySpawns.TryDequeue(out (string? uid, MapCoordinates coordinates, Delegate? doAfter) t))
            {
                var uid = EntityManager.SpawnEntity(t.uid, t.coordinates);
                if (t.doAfter != null)
                    t.doAfter.DynamicInvoke(uid);
            }
        }

        public void QueueSpawnEntity(string? protoName, EntityCoordinates coordinates, Delegate? doAfter = null)
        {
            if (!coordinates.IsValid(EntityManager))
                throw new InvalidOperationException($"Tried to spawn entity {protoName} on invalid coordinates {coordinates}.");

            var xforms = EntityManager.System<SharedTransformSystem>();
            coordinates.ToMap(EntityManager);
            var transform = EntityManager.GetComponent<TransformComponent>(coordinates.EntityId);
            var worldPos = xforms.GetWorldMatrix(coordinates.EntityId).Transform(coordinates.Position);
            var mapCoords = new MapCoordinates(worldPos, transform.MapID);
            _queuedEntitySpawns.Enqueue((protoName, mapCoords, doAfter));
        }
        public void QueueSpawnEntity(string? protoName, MapCoordinates coordinates, Delegate? doAfter = null)
        {
            _queuedEntitySpawns.Enqueue((protoName, coordinates, doAfter));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _queuedEntitySpawns.Clear();
        }

    }
}
