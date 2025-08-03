using UnityEngine;

public class facePlayerOnAxis : MonoBehaviour
{
	private Transform playerCamera;

	public Transform turnAxis;

	private bool gotPlayer;

	private void Update()
	{
		if (StartOfRound.Instance.audioListener != null)
		{
			base.transform.LookAt(StartOfRound.Instance.audioListener.transform.position, turnAxis.up);
		}
	}
}
