using System.Collections.Generic;
using UnityEngine;

internal class AudioSourceComparer : IEqualityComparer<AudioSource>
{
	public bool Equals(AudioSource x, AudioSource y)
	{
		return x.GetInstanceID() == y.GetInstanceID();
	}

	public int GetHashCode(AudioSource obj)
	{
		return obj.GetInstanceID().GetHashCode();
	}
}
