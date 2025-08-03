using System.Collections;
using UnityEngine;

public class ExtensionLadderItem : GrabbableObject
{
	private bool ladderActivated;

	private bool ladderAnimationBegun;

	private Coroutine ladderAnimationCoroutine;

	public Animator ladderAnimator;

	public Animator ladderRotateAnimator;

	public Transform baseNode;

	public Transform topNode;

	public Transform moveableNode;

	private RaycastHit hit;

	private int layerMask = 268437761;

	public AudioClip hitRoof;

	public AudioClip fullExtend;

	public AudioClip hitWall;

	public AudioClip ladderExtendSFX;

	public AudioClip ladderFallSFX;

	public AudioClip ladderShrinkSFX;

	public AudioClip blinkWarningSFX;

	public AudioClip lidOpenSFX;

	public AudioSource ladderAudio;

	public InteractTrigger ladderScript;

	private float rotateAmount;

	private float extendAmount;

	private float ladderTimer;

	private bool ladderBlinkWarning;

	private bool ladderShrunkAutomatically;

	public Collider interactCollider;

	public Collider bridgeCollider;

	public Collider killTrigger;

	public override void Update()
	{
		base.Update();
		if (playerHeldBy == null && !isHeld && !isHeldByEnemy && reachedFloorTarget && ladderActivated)
		{
			if (!ladderAnimationBegun)
			{
				ladderTimer = 0f;
				StartLadderAnimation();
			}
			else if (ladderAnimationBegun)
			{
				ladderTimer += Time.deltaTime;
				if (!ladderBlinkWarning && ladderTimer > 15f)
				{
					ladderBlinkWarning = true;
					ladderAnimator.SetBool("blinkWarning", value: true);
					ladderAudio.clip = blinkWarningSFX;
					ladderAudio.Play();
				}
				else if (ladderTimer >= 20f)
				{
					ladderActivated = false;
					ladderBlinkWarning = false;
					ladderAudio.Stop();
					ladderAnimator.SetBool("blinkWarning", value: false);
				}
			}
			return;
		}
		if (ladderAnimationBegun)
		{
			ladderAnimationBegun = false;
			ladderAudio.Stop();
			killTrigger.enabled = false;
			bridgeCollider.enabled = false;
			interactCollider.enabled = false;
			if (ladderAnimationCoroutine != null)
			{
				StopCoroutine(ladderAnimationCoroutine);
			}
			ladderAnimator.SetBool("blinkWarning", value: false);
			ladderAudio.transform.position = base.transform.position;
			ladderAudio.PlayOneShot(ladderShrinkSFX);
			ladderActivated = false;
		}
		killTrigger.enabled = false;
		ladderScript.interactable = false;
		if (GameNetworkManager.Instance.localPlayerController != null && GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith == ladderScript)
		{
			ladderScript.CancelAnimationExternally();
		}
		if (rotateAmount > 0f)
		{
			rotateAmount = Mathf.Max(rotateAmount - Time.deltaTime * 2f, 0f);
			ladderRotateAnimator.SetFloat("rotationAmount", rotateAmount);
		}
		else
		{
			ladderRotateAnimator.SetFloat("rotationAmount", 0f);
		}
		if (extendAmount > 0f)
		{
			extendAmount = Mathf.Max(extendAmount - Time.deltaTime * 2f, 0f);
			ladderAnimator.SetFloat("extensionAmount", extendAmount);
		}
		else
		{
			ladderAnimator.SetBool("openLid", value: false);
			ladderAnimator.SetBool("extend", value: false);
			ladderAnimator.SetFloat("extensionAmount", 0f);
		}
	}

	private void StartLadderAnimation()
	{
		ladderAnimationBegun = true;
		ladderScript.interactable = false;
		if (ladderAnimationCoroutine != null)
		{
			StopCoroutine(ladderAnimationCoroutine);
		}
		ladderAnimationCoroutine = StartCoroutine(LadderAnimation());
	}

	private IEnumerator LadderAnimation()
	{
		ladderAudio.volume = 1f;
		ladderScript.interactable = false;
		interactCollider.enabled = false;
		bridgeCollider.enabled = false;
		killTrigger.enabled = false;
		ladderAnimator.SetBool("openLid", value: false);
		ladderAnimator.SetBool("extend", value: false);
		yield return null;
		ladderAnimator.SetBool("openLid", value: true);
		ladderAudio.transform.position = base.transform.position;
		ladderAudio.PlayOneShot(lidOpenSFX, 1f);
		RoundManager.Instance.PlayAudibleNoise(ladderAudio.transform.position, 18f, 0.8f, 0, isInShipRoom);
		yield return new WaitForSeconds(1f);
		ladderAnimator.SetBool("extend", value: true);
		float ladderExtendAmountNormalized = GetLadderExtensionDistance() / 9.72f;
		float ladderRotateAmountNormalized = Mathf.Clamp(GetLadderRotationDegrees(ladderExtendAmountNormalized) / -90f, 0f, 0.99f);
		ladderAudio.clip = ladderExtendSFX;
		ladderAudio.Play();
		float currentNormalizedTime2 = 0f;
		float speedMultiplier2 = 0.1f;
		while (currentNormalizedTime2 < ladderExtendAmountNormalized)
		{
			speedMultiplier2 += Time.deltaTime * 2f;
			currentNormalizedTime2 = Mathf.Min(currentNormalizedTime2 + Time.deltaTime * speedMultiplier2, ladderExtendAmountNormalized);
			ladderAnimator.SetFloat("extensionAmount", currentNormalizedTime2);
			yield return null;
		}
		extendAmount = currentNormalizedTime2;
		interactCollider.enabled = true;
		bridgeCollider.enabled = false;
		killTrigger.enabled = false;
		ladderAudio.Stop();
		if (ladderExtendAmountNormalized == 1f)
		{
			ladderAudio.transform.position = baseNode.transform.position + baseNode.transform.up * 9.72f;
			ladderAudio.PlayOneShot(fullExtend, 0.7f);
			WalkieTalkie.TransmitOneShotAudio(ladderAudio, fullExtend, 0.7f);
			RoundManager.Instance.PlayAudibleNoise(ladderAudio.transform.position, 8f, 0.5f, 0, isInShipRoom);
		}
		else
		{
			ladderAudio.transform.position = baseNode.transform.position + baseNode.transform.up * (ladderExtendAmountNormalized * 9.72f);
			ladderAudio.PlayOneShot(hitRoof);
			WalkieTalkie.TransmitOneShotAudio(ladderAudio, hitRoof);
			RoundManager.Instance.PlayAudibleNoise(ladderAudio.transform.position, 17f, 0.8f, 0, isInShipRoom);
		}
		yield return new WaitForSeconds(0.4f);
		ladderAudio.clip = ladderFallSFX;
		ladderAudio.Play();
		ladderAudio.volume = 0f;
		speedMultiplier2 = 0.15f;
		currentNormalizedTime2 = 0f;
		while (currentNormalizedTime2 < ladderRotateAmountNormalized)
		{
			speedMultiplier2 += Time.deltaTime * 2f;
			currentNormalizedTime2 = Mathf.Min(currentNormalizedTime2 + Time.deltaTime * speedMultiplier2, ladderRotateAmountNormalized);
			if (ladderExtendAmountNormalized > 0.6f && currentNormalizedTime2 > 0.5f)
			{
				killTrigger.enabled = true;
			}
			ladderAudio.volume = Mathf.Min(ladderAudio.volume + Time.deltaTime * 1.75f, 1f);
			ladderRotateAnimator.SetFloat("rotationAmount", currentNormalizedTime2);
			yield return null;
		}
		rotateAmount = ladderRotateAmountNormalized;
		ladderAudio.volume = 1f;
		ladderAudio.Stop();
		ladderAudio.transform.position = moveableNode.transform.position;
		ladderAudio.PlayOneShot(hitWall, Mathf.Min(ladderRotateAmountNormalized + 0.3f, 1f));
		RoundManager.Instance.PlayAudibleNoise(ladderAudio.transform.position, 18f, 0.7f, 0, isInShipRoom);
		if (ladderRotateAmountNormalized * 90f < 45f)
		{
			ladderScript.interactable = true;
			interactCollider.enabled = true;
		}
		else
		{
			bridgeCollider.enabled = true;
		}
		killTrigger.enabled = false;
	}

	private float GetLadderExtensionDistance()
	{
		if (Physics.Raycast(baseNode.transform.position, Vector3.up, out hit, 9.72f, layerMask, QueryTriggerInteraction.Ignore))
		{
			return hit.distance;
		}
		return 9.72f;
	}

	private float GetLadderRotationDegrees(float topOfLadder)
	{
		float num = 90f;
		for (int num2 = 4; num2 >= 1; num2--)
		{
			float y = 2.43f * (float)num2;
			moveableNode.transform.localPosition = new Vector3(0f, y, 0f);
			baseNode.localEulerAngles = Vector3.zero;
			for (int i = 1; i < 20; i++)
			{
				Vector3 position = moveableNode.transform.position;
				baseNode.localEulerAngles = new Vector3((float)(-i) * 4.5f, 0f, 0f);
				if (Physics.Linecast(position, moveableNode.transform.position, layerMask, QueryTriggerInteraction.Ignore))
				{
					float num3 = (float)(i - 1) * 4.5f;
					if (num3 < num)
					{
						num = num3;
					}
					break;
				}
			}
			if (num < 12f)
			{
				break;
			}
		}
		return 0f - num;
	}

	public override void DiscardItem()
	{
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		base.EquipItem();
	}

	public override void DiscardItemFromEnemy()
	{
		base.DiscardItemFromEnemy();
		ladderActivated = true;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		ladderActivated = true;
		if (base.IsOwner)
		{
			playerHeldBy.DiscardHeldObject();
		}
	}
}
