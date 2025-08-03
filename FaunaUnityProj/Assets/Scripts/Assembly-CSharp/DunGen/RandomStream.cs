using System;

namespace DunGen
{
	public sealed class RandomStream
	{
		private const int maxValue = int.MaxValue;

		private const int seed = 161803398;

		private int iNext;

		private int iNextP;

		private int[] seedArray = new int[56];

		public RandomStream()
			: this(Environment.TickCount)
		{
		}

		public RandomStream(int Seed)
		{
			int num = 161803398 - ((Seed == int.MinValue) ? int.MaxValue : Math.Abs(Seed));
			seedArray[55] = num;
			int num2 = 1;
			for (int i = 1; i < 55; i++)
			{
				int num3 = 21 * i % 55;
				seedArray[num3] = num2;
				num2 = num - num2;
				if (num2 < 0)
				{
					num2 += int.MaxValue;
				}
				num = seedArray[num3];
			}
			for (int j = 1; j < 5; j++)
			{
				for (int k = 1; k < 56; k++)
				{
					seedArray[k] -= seedArray[1 + (k + 30) % 55];
					if (seedArray[k] < 0)
					{
						seedArray[k] += int.MaxValue;
					}
				}
			}
			iNext = 0;
			iNextP = 21;
			Seed = 1;
		}

		private double Sample()
		{
			return (double)InternalSample() * 4.656612875245797E-10;
		}

		private int InternalSample()
		{
			int num = iNext;
			int num2 = iNextP;
			if (++num >= 56)
			{
				num = 1;
			}
			if (++num2 >= 56)
			{
				num2 = 1;
			}
			int num3 = seedArray[num] - seedArray[num2];
			if (num3 == int.MaxValue)
			{
				num3--;
			}
			if (num3 < 0)
			{
				num3 += int.MaxValue;
			}
			seedArray[num] = num3;
			iNext = num;
			iNextP = num2;
			return num3;
		}

		public int Next()
		{
			return InternalSample();
		}

		private double GetSampleForLargeRange()
		{
			int num = InternalSample();
			if (InternalSample() % 2 == 0)
			{
				num = -num;
			}
			return ((double)num + 2147483646.0) / 4294967293.0;
		}

		public int Next(int minValue, int maxValue)
		{
			if (minValue > maxValue)
			{
				throw new ArgumentOutOfRangeException("minValue");
			}
			long num = (long)maxValue - (long)minValue;
			if (num <= int.MaxValue)
			{
				return (int)(Sample() * (double)num) + minValue;
			}
			return (int)((long)(GetSampleForLargeRange() * (double)num) + minValue);
		}

		public int Next(int maxValue)
		{
			if (maxValue < 0)
			{
				throw new ArgumentOutOfRangeException("maxValue");
			}
			return (int)(Sample() * (double)maxValue);
		}

		public double NextDouble()
		{
			return Sample();
		}

		public void NextBytes(byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = (byte)(InternalSample() % 256);
			}
		}
	}
}
