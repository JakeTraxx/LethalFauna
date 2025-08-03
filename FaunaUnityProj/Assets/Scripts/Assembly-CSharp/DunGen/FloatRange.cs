using System;

namespace DunGen
{
	[Serializable]
	public class FloatRange
	{
		public float Min;

		public float Max;

		public FloatRange()
		{
		}

		public FloatRange(float min, float max)
		{
			Min = min;
			Max = max;
		}

		public float GetRandom(RandomStream random)
		{
			if (Min > Max)
			{
				float min = Min;
				Min = Max;
				Max = min;
			}
			float num = Max - Min;
			return Min + (float)random.NextDouble() * num;
		}
	}
}
