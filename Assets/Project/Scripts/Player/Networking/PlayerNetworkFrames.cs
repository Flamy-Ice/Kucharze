#if KUCHARZE_ONLINE
using Unity.Collections;
using Unity.Netcode;

namespace ModularMovement
{
    // Carries fixed-step controls and authoritative checkpoints; clients never send positions.
    public struct PlayerInputFrame : INetworkSerializable
    {
        public uint Sequence, Epoch;
        public PlayerInputSnapshot Input;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref Input.Move);
            serializer.SerializeValue(ref Input.RunHeld);
            serializer.SerializeValue(ref Input.RunPressCount);
            serializer.SerializeValue(ref Input.DashPressCount);
            serializer.SerializeValue(ref Input.ResetVersion);
            serializer.SerializeValue(ref Input.DashResetVersion);
            serializer.SerializeValue(ref Input.DashAvailable);
        }
    }

    public struct PlayerServerFrame : INetworkSerializable
    {
        public uint Sequence, Epoch;
        public bool CanSimulate;
        public PlayerMotorSnapshot Motor;
        public PlayerRun.SimulationState Run;
        public PlayerDash.SimulationState Dash;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref CanSimulate);
            serializer.SerializeValue(ref Motor.Position);
            serializer.SerializeValue(ref Motor.Velocity);
            serializer.SerializeValue(ref Motor.VerticalVelocity);
            serializer.SerializeValue(ref Motor.Grounded);
            var source = new FixedString64Bytes(Motor.Source ?? "None");
            serializer.SerializeValue(ref source);
            if (serializer.IsReader) Motor.Source = source.ToString();
            serializer.SerializeValue(ref Run.PressCount);
            serializer.SerializeValue(ref Run.ResetVersion);
            serializer.SerializeValue(ref Run.ToggledOn);
            serializer.SerializeValue(ref Run.Requested);
            serializer.SerializeValue(ref Dash.LastDirection);
            serializer.SerializeValue(ref Dash.Direction);
            serializer.SerializeValue(ref Dash.Remaining);
            serializer.SerializeValue(ref Dash.Duration);
            serializer.SerializeValue(ref Dash.Cooldown);
            serializer.SerializeValue(ref Dash.PressCount);
            serializer.SerializeValue(ref Dash.ResetVersion);
        }
    }
}
#endif
