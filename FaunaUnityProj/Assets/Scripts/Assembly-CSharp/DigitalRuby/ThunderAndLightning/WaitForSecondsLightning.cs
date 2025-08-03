using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class WaitForSecondsLightning : CustomYieldInstruction
	{
		private float remaining;

		public override bool keepWaiting
		{
			get
			{
				if (remaining <= 0f)
				{
					return false;
				}
				remaining -= LightningBoltScript.DeltaTime;
				return true;
			}
		}

		public WaitForSecondsLightning(float time)
		{
			remaining = time;
		}
	}
}
