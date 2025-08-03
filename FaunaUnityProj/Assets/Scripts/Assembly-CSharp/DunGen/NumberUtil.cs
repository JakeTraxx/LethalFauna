using UnityEngine;

namespace DunGen
{
	public static class NumberUtil
	{
		public static float ClampToNearest(float value, params float[] possibleValues)
		{
			float[] array = new float[possibleValues.Length];
			for (int i = 0; i < possibleValues.Length; i++)
			{
				array[i] = Mathf.Abs(value - possibleValues[i]);
			}
			float num = float.MaxValue;
			int num2 = 0;
			for (int j = 0; j < array.Length; j++)
			{
				float num3 = array[j];
				if (num3 < num)
				{
					num = num3;
					num2 = j;
				}
			}
			return possibleValues[num2];
		}
	}
}
