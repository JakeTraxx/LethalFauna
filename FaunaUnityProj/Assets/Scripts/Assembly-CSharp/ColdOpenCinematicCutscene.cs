using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ColdOpenCinematicCutscene : MonoBehaviour
{
	public Camera cam;

	public Transform camContainer;

	public Transform camTarget;

	private InputActionAsset inputAsset;

	public float cameraUp;

	public float maxCameraUp = 40f;

	public float minCameraUp = -60f;

	[Space(5f)]
	public float cameraTurn;

	public float maxCameraTurn = 80f;

	public float minCameraTurn = -80f;

	private float startInputTimer;

	public Animator cameraAnimator;

	public float lookSens = 0.008f;

	private void TurnCamera(Vector2 input)
	{
		input = input * lookSens * IngamePlayerSettings.Instance.settings.lookSensitivity;
		cameraTurn += input.x;
		cameraTurn = Mathf.Clamp(cameraTurn, minCameraTurn, maxCameraTurn);
		cameraUp -= input.y;
		cameraUp = Mathf.Clamp(cameraUp, -60f, 40f);
		camTarget.transform.localEulerAngles = new Vector3(cameraUp, cameraTurn, camTarget.transform.localEulerAngles.z);
		camTarget.eulerAngles = new Vector3(camTarget.eulerAngles.x, camTarget.eulerAngles.y, 0f);
		camContainer.transform.rotation = Quaternion.Lerp(camContainer.transform.rotation, camTarget.rotation, 12f * Time.deltaTime);
	}

	public void Start()
	{
		inputAsset = IngamePlayerSettings.Instance.playerInput.actions;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		cameraTurn = camTarget.localEulerAngles.y;
		AudioListener.volume = Mathf.Max(IngamePlayerSettings.Instance.settings.masterVolume, 0.3f);
	}

	public void Update()
	{
		if (inputAsset == null)
		{
			Debug.LogError("Input asset not found!");
			return;
		}
		startInputTimer += Time.deltaTime;
		if (startInputTimer > 0.5f)
		{
			TurnCamera(inputAsset.FindAction("Look").ReadValue<Vector2>());
		}
	}

	public void ShakeCameraSmall()
	{
		cameraAnimator.SetTrigger("shake");
	}

	public void ShakeCameraLong()
	{
		cameraAnimator.SetTrigger("vibrateLong");
	}

	public void EndColdOpenCutscene()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		SceneManager.LoadScene("MainMenu");
	}
}
