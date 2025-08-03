using UnityEngine;

public class SetLineRendererPoints : MonoBehaviour
{
	private LineRenderer lineRenderer;

	public Transform anchor;

	public Transform target;

	private void Start()
	{
		lineRenderer = base.gameObject.GetComponent<LineRenderer>();
	}

	private void LateUpdate()
	{
		lineRenderer.SetPosition(0, anchor.position);
		lineRenderer.SetPosition(1, target.position);
	}
}
