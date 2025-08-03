using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace __GEN
{
	internal class NetworkVariableSerializationHelper
	{
		[RuntimeInitializeOnLoadMethod]
		internal static void InitializeSerialization()
		{
			NetworkVariableSerializationTypes.InitializeSerializer_FixedString<FixedString128Bytes>();
			NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedIEquatable<FixedString128Bytes>();
		}
	}
}
