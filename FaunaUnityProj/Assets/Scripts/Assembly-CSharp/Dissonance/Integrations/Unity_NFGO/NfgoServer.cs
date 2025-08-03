using System;
using Dissonance.Networking;
using Unity.Netcode;

namespace Dissonance.Integrations.Unity_NFGO
{
	public class NfgoServer : BaseServer<NfgoServer, NfgoClient, NfgoConn>
	{
		private readonly NfgoCommsNetwork _network;

		private byte[] _receiveBuffer = new byte[1024];

		private NetworkManager _networkManager;

		public NfgoServer(NfgoCommsNetwork network)
		{
			_network = network;
		}

		public override void Connect()
		{
			_networkManager = NetworkManager.Singleton;
			_networkManager.OnClientDisconnectCallback += Disconnected;
			_networkManager.CustomMessagingManager.RegisterNamedMessageHandler("DissonanceToServer", NamedMessageHandler);
			base.Connect();
		}

		public override void Disconnect()
		{
			if (_networkManager != null)
			{
				_networkManager.OnClientDisconnectCallback -= Disconnected;
				_networkManager.CustomMessagingManager?.UnregisterNamedMessageHandler("DissonanceToServer");
				_networkManager = null;
			}
			base.Disconnect();
		}

		private void Disconnected(ulong client)
		{
			ClientDisconnected(new NfgoConn(client));
		}

		private void NamedMessageHandler(ulong sender, FastBufferReader stream)
		{
			int length = stream.Length;
			if (_receiveBuffer.Length < length)
			{
				Array.Resize(ref _receiveBuffer, length);
			}
			ArraySegment<byte> data = NfgoCommsNetwork.ReadPacket(ref stream, ref _receiveBuffer);
			NetworkReceivedPacket(new NfgoConn(sender), data);
		}

		protected override void ReadMessages()
		{
		}

		protected override void SendReliable(NfgoConn destination, ArraySegment<byte> packet)
		{
			if (!(_networkManager == null))
			{
				_network.SendToClient(packet, destination, reliable: true, _networkManager);
			}
		}

		protected override void SendUnreliable(NfgoConn destination, ArraySegment<byte> packet)
		{
			if (!(_networkManager == null))
			{
				_network.SendToClient(packet, destination, reliable: false, _networkManager);
			}
		}
	}
}
