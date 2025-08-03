using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InitializeGame : MonoBehaviour
{
	public bool runBootUpScreen = true;

	public Animator bootUpAnimation;

	public AudioSource bootUpAudio;

	public AudioClip bootUpSFXError;

	public AudioClip coldOpen2Audio;

	public PlayerActions playerActions;

	private bool canSkip;

	private bool hasSkipped;

	public bool playColdOpenCinematic;

	public bool playColdOpenCinematic2;

	private void OnEnable()
	{
		playerActions.Movement.OpenMenu.performed += OpenMenu_performed;
		playerActions.Movement.Enable();
	}

	private void OnDisable()
	{
		playerActions.Movement.OpenMenu.performed -= OpenMenu_performed;
		playerActions.Movement.Disable();
	}

	private void Awake()
	{
		playerActions = new PlayerActions();
		Application.backgroundLoadingPriority = ThreadPriority.Normal;
		int num = ES3.Load("LastVerPlayed", "LCGeneralSaveData", GameNetworkManager.Instance.gameVersionNum);
		bool flag = num < 50;
		float num2 = ES3.Load("TimesLoadedGame", "LCGeneralSaveData", 0);
		playColdOpenCinematic = flag || num2 == 7f;
		if (playColdOpenCinematic)
		{
			playColdOpenCinematic2 = false;
		}
		else if ((num2 > 25f || num < 60) && !ES3.Load("PlayedCinematic2", "LCGeneralSaveData", defaultValue: false))
		{
			ES3.Save("PlayedCinematic2", value: true, "LCGeneralSaveData");
			playColdOpenCinematic2 = true;
		}
		if (flag)
		{
			ES3.Save("TimesLoadedGame", 8, "LCGeneralSaveData");
		}
	}

	public void OpenMenu_performed(InputAction.CallbackContext context)
	{
		canSkip = !playColdOpenCinematic && !playColdOpenCinematic2;
		if (context.performed && canSkip && !hasSkipped)
		{
			hasSkipped = true;
			SceneManager.LoadScene("MainMenu");
		}
	}

	private IEnumerator SendToNextScene()
	{
		if (runBootUpScreen)
		{
			if (playColdOpenCinematic2)
			{
				bootUpAudio.PlayOneShot(bootUpSFXError);
			}
			else
			{
				bootUpAudio.Play();
			}
			yield return new WaitForSeconds(0.2f);
			canSkip = true;
			if (playColdOpenCinematic2)
			{
				bootUpAnimation.SetTrigger("playAnim2");
			}
			else
			{
				bootUpAnimation.SetTrigger("playAnim");
			}
			if (playColdOpenCinematic)
			{
				yield return new WaitForSeconds(1.5f);
				SceneManager.LoadScene("ColdOpen1");
				yield break;
			}
			if (playColdOpenCinematic2)
			{
				coldOpen2Audio.LoadAudioData();
				yield return new WaitForSeconds(5.6f);
				SceneManager.LoadScene("ColdOpen2");
				yield break;
			}
			yield return new WaitForSeconds(3f);
		}
		yield return new WaitForSeconds(0.2f);
		SceneManager.LoadScene("MainMenu");
	}

	private void Start()
	{
		StartCoroutine(SendToNextScene());
	}
}
