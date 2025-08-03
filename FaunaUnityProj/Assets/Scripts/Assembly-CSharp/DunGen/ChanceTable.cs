using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	public class ChanceTable<T>
	{
		[SerializeField]
		public List<Chance<T>> Weights = new List<Chance<T>>();

		public void Add(T value, float weight)
		{
			Weights.Add(new Chance<T>(value, weight));
		}

		public void Remove(T value)
		{
			for (int i = 0; i < Weights.Count; i++)
			{
				if (Weights[i].Value.Equals(value))
				{
					Weights.RemoveAt(i);
				}
			}
		}

		public T GetRandom(RandomStream random)
		{
			float num = Weights.Select((Chance<T> x) => x.Weight).Sum();
			float num2 = (float)(random.NextDouble() * (double)num);
			foreach (Chance<T> weight in Weights)
			{
				if (num2 < weight.Weight)
				{
					return weight.Value;
				}
				num2 -= weight.Weight;
			}
			return default(T);
		}

		public static TVal GetCombinedRandom<TVal, TChance>(RandomStream random, params ChanceTable<TVal>[] tables)
		{
			float num = tables.SelectMany((ChanceTable<TVal> x) => x.Weights.Select((Chance<TVal> y) => y.Weight)).Sum();
			float num2 = (float)(random.NextDouble() * (double)num);
			foreach (Chance<TVal> item in tables.SelectMany((ChanceTable<TVal> x) => x.Weights))
			{
				if (num2 < item.Weight)
				{
					return item.Value;
				}
				num2 -= item.Weight;
			}
			return default(TVal);
		}
	}
}
