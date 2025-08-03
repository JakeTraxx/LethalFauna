using Unity.Netcode;

internal struct SerializedList : INetworkSerializable
{
	public ulong[] Value;

	void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
	{
		serializer.SerializeValue(ref Value, default(FastBufferWriter.ForPrimitives));
	}
}
