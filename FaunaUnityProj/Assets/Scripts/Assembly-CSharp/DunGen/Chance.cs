using System;

namespace DunGen
{
	[Serializable]
	public class Chance<T>
	{
		public T Value;

		public float Weight;

		public Chance()
			: this(default(T), 1f)
		{
		}

		public Chance(T value)
			: this(value, 1f)
		{
		}

		public Chance(T value, float weight)
		{
			Value = value;
			Weight = weight;
		}
	}
}
