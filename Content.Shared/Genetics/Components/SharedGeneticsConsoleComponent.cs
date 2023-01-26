using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Genetics.GeneticsConsole;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Genetics.Components
{
    [NetworkedComponent()]
    public abstract class SharedGeneticsConsoleComponent : Component
    {

        [Serializable, NetSerializable]
        public sealed class GeneticsConsoleMessage : BoundUserInterfaceMessage
        {
            public EntityUid DeviceUid;
            public string? Error;

            public GeneticsConsoleMessage(EntityUid deviceUid, string? error = null)
            {
                DeviceUid = deviceUid;
                Error = error;
            }
        }

    }
}
