using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Netcode;

namespace Dissonance.Integrations.Unity_NFGO
{
	public class NfgoCommsNetwork : BaseCommsNetwork<NfgoServer, NfgoClient, NfgoConn, Unit, Unit>
	{
		private readonly ConcurrentPool<byte[]> _loopbackBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);

		private readonly List<ArraySegment<byte>> _loopbackQueueToServer = new List<ArraySegment<byte>>();

		private readonly List<ArraySegment<byte>> _loopbackQueueToClient = new List<ArraySegment<byte>>();

		protected override NfgoClient CreateClient(Unit connectionParameters)
		{
			return new NfgoClient(this);
		}

		protected override NfgoServer CreateServer(Unit connectionParameters)
		{
			return new NfgoServer(this);
		}

		protected override void Update()
		{
			if (base.IsInitialized)
			{
				bool flag = NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient;
				bool isServer = NetworkManager.Singleton.IsServer;
				if (NetworkManager.Singleton.isActiveAndEnabled && (flag || isServer))
				{
					bool isServer2 = NetworkManager.Singleton.IsServer;
					bool isClient = NetworkManager.Singleton.IsClient;
					if (base.Mode.IsServerEnabled() != isServer2 || base.Mode.IsClientEnabled() != isClient)
					{
						if (isServer2 && isClient)
						{
							RunAsHost(Unit.None, Unit.None);
						}
						else if (isServer2)
						{
							RunAsDedicatedServer(Unit.None);
						}
						else if (isClient)
						{
							RunAsClient(Unit.None);
						}
					}
				}
				else if (base.Mode != 0)
				{
					Stop();
					_loopbackQueueToClient.Clear();
					_loopbackQueueToServer.Clear();
				}
				if (base.Client != null)
				{
					foreach (ArraySegment<byte> item in _loopbackQueueToClient)
					{
						if (item.Array != null)
						{
							base.Client.NetworkReceivedPacket(item);
							_loopbackBuffers.Put(item.Array);
						}
					}
				}
				_loopbackQueueToClient.Clear();
				if (base.Server != null)
				{
					foreach (ArraySegment<byte> item2 in _loopbackQueueToServer)
					{
						if (item2.Array != null)
						{
							base.Server.NetworkReceivedPacket(new NfgoConn(NetworkManager.Singleton.LocalClientId), item2);
							_loopbackBuffers.Put(item2.Array);
						}
					}
				}
				_loopbackQueueToServer.Clear();
			}
			base.Update();
		}

		internal void SendToServer(ArraySegment<byte> packet, bool reliable, [NotNull] NetworkManager netManager)
		{
			if (packet.Array == null)
			{
				throw new ArgumentException("packet is null");
			}
			if (netManager.IsHost)
			{
				_loopbackQueueToServer.Add(packet.CopyToSegment(_loopbackBuffers.Get()));
				return;
			}
			using FastBufferWriter messageStream = WritePacket(packet);
			netManager.CustomMessagingManager.SendNamedMessage("DissonanceToServer", 0uL, messageStream, reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.Unreliable);
		}

		internal void SendToClient(ArraySegment<byte> packet, NfgoConn client, bool reliable, [NotNull] NetworkManager netManager)
		{
			if (packet.Array == null)
			{
				throw new ArgumentException("packet is null");
			}
			if (netManager.LocalClientId == client.ClientId)
			{
				_loopbackQueueToClient.Add(packet.CopyToSegment(_loopbackBuffers.Get()));
			}
			else if (reliable || netManager.ConnectedClients.ContainsKey(client.ClientId))
			{
				using (FastBufferWriter messageStream = WritePacket(packet))
				{
					netManager.CustomMessagingManager.SendNamedMessage("DissonanceToClient", client.ClientId, messageStream, reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.Unreliable);
				}
			}
		}

		private static FastBufferWriter WritePacket(ArraySegment<byte> packet)
		{
			FastBufferWriter result = new FastBufferWriter(packet.Count + 4, Allocator.Temp);
			uint value = (uint)packet.Count;
			result.WriteValueSafe(in value, default(FastBufferWriter.ForPrimitives));
			result.WriteBytesSafe(packet.Array, packet.Count, packet.Offset);
			return result;
		}

		internal static ArraySegment<byte> ReadPacket(ref FastBufferReader reader, [CanBeNull] ref byte[] buffer)
		{
			reader.ReadValueSafe(out uint value, default(FastBufferWriter.ForPrimitives));
			if (buffer == null || buffer.Length < value)
			{
				buffer = new byte[Math.Max(1024u, value)];
			}
			for (int i = 0; i < value; i++)
			{
				reader.ReadByteSafe(out var value2);
				buffer[i] = value2;
			}
			return new ArraySegment<byte>(buffer, 0, (int)value);
		}
	}
}
