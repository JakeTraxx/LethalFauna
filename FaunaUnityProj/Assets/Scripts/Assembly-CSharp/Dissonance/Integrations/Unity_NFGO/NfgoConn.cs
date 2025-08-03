using System;

namespace Dissonance.Integrations.Unity_NFGO
{
	public readonly struct NfgoConn : IEquatable<NfgoConn>
	{
		public readonly ulong ClientId;

		public NfgoConn(ulong id)
		{
			ClientId = id;
		}

		public bool Equals(NfgoConn other)
		{
			return ClientId == other.ClientId;
		}

		public override bool Equals(object obj)
		{
			if (obj is NfgoConn other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ClientId.GetHashCode();
		}

		public static bool operator ==(NfgoConn left, NfgoConn right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(NfgoConn left, NfgoConn right)
		{
			return !left.Equals(right);
		}
	}
}
