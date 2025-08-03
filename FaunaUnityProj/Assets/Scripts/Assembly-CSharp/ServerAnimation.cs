using Unity.Netcode;

public struct ServerAnimation : INetworkSerializable
{
	public string animationString;

	public NetworkObjectReference animatorObj;

	public bool isTrigger;

	public bool setTrue;

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref animatorObj, default(FastBufferWriter.ForNetworkSerializable));
		serializer.SerializeValue(ref isTrigger, default(FastBufferWriter.ForPrimitives));
		if (!isTrigger)
		{
			serializer.SerializeValue(ref setTrue, default(FastBufferWriter.ForPrimitives));
		}
	}
}
