using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Random Props/Global Prop")]
	public class GlobalProp : MonoBehaviour
	{
		public int PropGroupID;

		public float MainPathWeight = 1f;

		public float BranchPathWeight = 1f;

		public AnimationCurve DepthWeightScale = AnimationCurve.Linear(0f, 1f, 1f, 1f);
	}
}
