using System;

namespace DunGen
{
	[Serializable]
	public class IntRange
	{
		public int Min;

		public int Max;

		public IntRange()
		{
		}

		public IntRange(int min, int max)
		{
			Min = min;
			Max = max;
		}

		public int GetRandom(RandomStream random)
		{
			if (Min > Max)
			{
				Max = Min;
			}
			return random.Next(Min, Max + 1);
		}

		public override string ToString()
		{
			return Min + " - " + Max;
		}
	}
}
