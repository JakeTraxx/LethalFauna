using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class LightningFieldScript : LightningBoltPrefabScriptBase
	{
		[Header("Lightning Field Properties")]
		[Tooltip("The minimum length for a field segment")]
		public float MinimumLength = 0.01f;

		private float minimumLengthSquared;

		[Tooltip("The bounds to put the field in.")]
		public Bounds FieldBounds;

		[Tooltip("Optional light for the lightning field to emit")]
		public Light Light;

		private Vector3 RandomPointInBounds()
		{
			float x = Random.Range(FieldBounds.min.x, FieldBounds.max.x);
			float y = Random.Range(FieldBounds.min.y, FieldBounds.max.y);
			float z = Random.Range(FieldBounds.min.z, FieldBounds.max.z);
			return new Vector3(x, y, z);
		}

		protected override void Start()
		{
			base.Start();
			if (Light != null)
			{
				Light.enabled = false;
			}
		}

		protected override void Update()
		{
			base.Update();
			if (!(Time.timeScale <= 0f) && Light != null)
			{
				Light.transform.position = FieldBounds.center;
				Light.intensity = Random.Range(2.8f, 3.2f);
			}
		}

		public override void CreateLightningBolt(LightningBoltParameters parameters)
		{
			minimumLengthSquared = MinimumLength * MinimumLength;
			for (int i = 0; i < 16; i++)
			{
				parameters.Start = RandomPointInBounds();
				parameters.End = RandomPointInBounds();
				if ((parameters.End - parameters.Start).sqrMagnitude >= minimumLengthSquared)
				{
					break;
				}
			}
			if (Light != null)
			{
				Light.enabled = true;
			}
			base.CreateLightningBolt(parameters);
		}
	}
}
