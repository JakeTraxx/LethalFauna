using Unity.Netcode;

public struct ServerAudio : INetworkSerializable
{
	public NetworkObjectReference audioObj;

	public bool oneshot;

	public bool looped;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref audioObj, default(FastBufferWriter.ForNetworkSerializable));
		serializer.SerializeValue(ref oneshot, default(FastBufferWriter.ForPrimitives));
		if (!oneshot)
		{
			serializer.SerializeValue(ref looped, default(FastBufferWriter.ForPrimitives));
		}
	}
}
