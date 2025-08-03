using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class LightningBoltTransformTrackerScript : MonoBehaviour
	{
		[Tooltip("The lightning script to track.")]
		public LightningBoltPrefabScript LightningScript;

		[Tooltip("The transform to track which will be where the bolts are emitted from.")]
		public Transform StartTarget;

		[Tooltip("(Optional) The transform to track which will be where the bolts are emitted to. If no end target is specified, lightning will simply move to stay on top of the start target.")]
		public Transform EndTarget;

		[SingleLine("Scaling limits.")]
		public RangeOfFloats ScaleLimit = new RangeOfFloats
		{
			Minimum = 0.1f,
			Maximum = 10f
		};

		private readonly Dictionary<Transform, LightningCustomTransformStateInfo> transformStartPositions = new Dictionary<Transform, LightningCustomTransformStateInfo>();

		private void Start()
		{
			if (LightningScript != null)
			{
				LightningScript.CustomTransformHandler.RemoveAllListeners();
				LightningScript.CustomTransformHandler.AddListener(CustomTransformHandler);
			}
		}

		private static float AngleBetweenVector2(Vector2 vec1, Vector2 vec2)
		{
			Vector2 normalized = (vec2 - vec1).normalized;
			return Vector2.Angle(Vector2.right, normalized) * Mathf.Sign(vec2.y - vec1.y);
		}

		private static void UpdateTransform(LightningCustomTransformStateInfo state, LightningBoltPrefabScript script, RangeOfFloats scaleLimit)
		{
			if (state.Transform == null || state.StartTransform == null)
			{
				return;
			}
			if (state.EndTransform == null)
			{
				state.Transform.position = state.StartTransform.position - state.BoltStartPosition;
				return;
			}
			Quaternion quaternion;
			if ((script.CameraMode == CameraMode.Auto && script.Camera.orthographic) || script.CameraMode == CameraMode.OrthographicXY)
			{
				float num = AngleBetweenVector2(state.BoltStartPosition, state.BoltEndPosition);
				quaternion = Quaternion.AngleAxis(AngleBetweenVector2(state.StartTransform.position, state.EndTransform.position) - num, Vector3.forward);
			}
			if (script.CameraMode == CameraMode.OrthographicXZ)
			{
				float num2 = AngleBetweenVector2(new Vector2(state.BoltStartPosition.x, state.BoltStartPosition.z), new Vector2(state.BoltEndPosition.x, state.BoltEndPosition.z));
				quaternion = Quaternion.AngleAxis(AngleBetweenVector2(new Vector2(state.StartTransform.position.x, state.StartTransform.position.z), new Vector2(state.EndTransform.position.x, state.EndTransform.position.z)) - num2, Vector3.up);
			}
			else
			{
				Quaternion rotation = Quaternion.LookRotation((state.BoltEndPosition - state.BoltStartPosition).normalized);
				quaternion = Quaternion.LookRotation((state.EndTransform.position - state.StartTransform.position).normalized) * Quaternion.Inverse(rotation);
			}
			state.Transform.rotation = quaternion;
			float num3 = Vector3.Distance(state.BoltStartPosition, state.BoltEndPosition);
			float num4 = Vector3.Distance(state.EndTransform.position, state.StartTransform.position);
			float num5 = Mathf.Clamp((num3 < Mathf.Epsilon) ? 1f : (num4 / num3), scaleLimit.Minimum, scaleLimit.Maximum);
			state.Transform.localScale = new Vector3(num5, num5, num5);
			Vector3 vector = quaternion * (num5 * state.BoltStartPosition);
			state.Transform.position = state.StartTransform.position - vector;
		}

		public void CustomTransformHandler(LightningCustomTransformStateInfo state)
		{
			if (base.enabled)
			{
				if (LightningScript == null)
				{
					Debug.LogError("LightningScript property must be set to non-null.");
				}
				else if (state.State == LightningCustomTransformState.Executing)
				{
					UpdateTransform(state, LightningScript, ScaleLimit);
				}
				else if (state.State == LightningCustomTransformState.Started)
				{
					state.StartTransform = StartTarget;
					state.EndTransform = EndTarget;
					transformStartPositions[base.transform] = state;
				}
				else
				{
					transformStartPositions.Remove(base.transform);
				}
			}
		}
	}
}
