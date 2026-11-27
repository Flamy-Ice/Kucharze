#if KUCHARZE_ONLINE
using Unity.Collections;
using Unity.Netcode;

namespace ModularMovement
{
    // Sends animation readings together so different packets cannot mix dash phases.
    public struct PlayerPresentationFrame : INetworkSerializable
    {
        public PlayerPresentationTimeline.Sample Sample;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sample.Time);
            serializer.SerializeValue(ref Sample.Velocity);
            serializer.SerializeValue(ref Sample.Facing);
            serializer.SerializeValue(ref Sample.Moving);
            serializer.SerializeValue(ref Sample.Grounded);
            serializer.SerializeValue(ref Sample.DashProgress);
            serializer.SerializeValue(ref Sample.DashDuration);
            serializer.SerializeValue(ref Sample.DashId);
            var source = new FixedString64Bytes(Sample.Source ?? "None");
            serializer.SerializeValue(ref source);
            if (serializer.IsReader) Sample.Source = source.ToString();
        }
    }
}
#endif
