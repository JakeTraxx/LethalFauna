using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class HauntedMaskItem : GrabbableObject, IVisibleThreat
{
	private bool maskOn;

	private bool attaching;

	private bool clampedToHead;

	private float lastIntervalCheck;

	private float attachTimer = 5f;

	private bool finishedAttaching;

	public AudioSource maskAudio;

	public AudioClip maskAttachAudio;

	public AudioClip maskAttachAudioLocal;

	public Animator maskAnimator;

	public MeshRenderer maskEyesFilled;

	public GameObject headMaskPrefab;

	public Transform currentHeadMask;

	public Vector3 headPositionOffset;

	public Vector3 headRotationOffset;

	private PlayerControllerB previousPlayerHeldBy;

	public EnemyType mimicEnemy;

	private bool holdingLastFrame;

	public bool maskIsHaunted = true;

	public int maskTypeId;

	ThreatType IVisibleThreat.type => ThreatType.Item;

	bool IVisibleThreat.IsThreatDead()
	{
		return false;
	}

	GrabbableObject IVisibleThreat.GetHeldObject()
	{
		return null;
	}

	int IVisibleThreat.SendSpecialBehaviour(int id)
	{
		return 0;
	}

	int IVisibleThreat.GetInterestLevel()
	{
		return 0;
	}

	int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
	{
		if (isHeld)
		{
			if (holdingLastFrame)
			{
				return 4;
			}
			return 2;
		}
		return 1;
	}

	Transform IVisibleThreat.GetThreatLookTransform()
	{
		return base.transform;
	}

	Transform IVisibleThreat.GetThreatTransform()
	{
		return base.transform;
	}

	Vector3 IVisibleThreat.GetThreatVelocity()
	{
		return Vector3.zero;
	}

	float IVisibleThreat.GetVisibility()
	{
		if (isPocketed)
		{
			return 0f;
		}
		return 1f;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (!attaching && !finishedAttaching && !(playerHeldBy == null) && base.IsOwner)
		{
			playerHeldBy.playerBodyAnimator.SetBool("HoldMask", buttonDown);
			Debug.Log("attaching: {attaching}; finishedAttaching: {finishedAttaching}");
			Debug.Log($"Setting maskOn {buttonDown}");
			maskOn = buttonDown;
			playerHeldBy.activatingItem = buttonDown;
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		lastIntervalCheck = Time.realtimeSinceStartup + 10f;
		previousPlayerHeldBy = playerHeldBy;
		holdingLastFrame = true;
	}

	public override void DiscardItem()
	{
		base.DiscardItem();
		if (currentHeadMask != null)
		{
			Debug.Log("Discard item called; not going through since headmask is not null");
			return;
		}
		Debug.Log($"Discard item called; headmask null: {currentHeadMask == null}");
		previousPlayerHeldBy.activatingItem = false;
		maskOn = false;
		CancelAttachToPlayerOnLocalClient();
	}

	public override void PocketItem()
	{
		base.PocketItem();
		if (currentHeadMask != null)
		{
			Debug.Log("Discard item called; not going through since headmask is not null");
			return;
		}
		Debug.Log($"Discard item called; headmask null: {currentHeadMask == null}");
		maskOn = false;
		playerHeldBy.activatingItem = false;
		CancelAttachToPlayerOnLocalClient();
	}

	private void CancelAttachToPlayerOnLocalClient()
	{
		attachTimer = 8f;
		attaching = false;
		maskAnimator.SetBool("attaching", value: false);
		if (previousPlayerHeldBy != null)
		{
			previousPlayerHeldBy.activatingItem = false;
			previousPlayerHeldBy.playerBodyAnimator.SetBool("HoldMask", value: false);
		}
		finishedAttaching = false;
		if (currentHeadMask != null)
		{
			UnityEngine.Object.Destroy(currentHeadMask.gameObject);
		}
		if (holdingLastFrame)
		{
			holdingLastFrame = false;
		}
		try
		{
			if (previousPlayerHeldBy.currentVoiceChatAudioSource == null)
			{
				StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
			}
			if (previousPlayerHeldBy.currentVoiceChatAudioSource != null)
			{
				previousPlayerHeldBy.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = 1f;
				OccludeAudio component = previousPlayerHeldBy.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
				component.overridingLowPass = false;
				component.lowPassOverride = 20000f;
				previousPlayerHeldBy.voiceMuffledByEnemy = false;
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Caught exception while attempting to unmuffle player voice from mask item: {arg}");
		}
	}

	public void BeginAttachment()
	{
		if (base.IsOwner)
		{
			AttachToPlayerOnLocalClient();
			AttachServerRpc();
		}
	}

	[ServerRpc]
	public void AttachServerRpc()
{		{
			AttachClientRpc();
		}
}
	[ClientRpc]
	public void AttachClientRpc()
{if(!base.IsOwner)			{
				AttachToPlayerOnLocalClient();
			}
}
	private void AttachToPlayerOnLocalClient()
	{
		attaching = true;
		maskAnimator.SetBool("attaching", value: true);
		maskEyesFilled.enabled = true;
		try
		{
			if (previousPlayerHeldBy.currentVoiceChatAudioSource == null)
			{
				StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
			}
			if (previousPlayerHeldBy.currentVoiceChatAudioSource != null)
			{
				previousPlayerHeldBy.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = 3f;
				OccludeAudio component = previousPlayerHeldBy.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
				component.overridingLowPass = true;
				component.lowPassOverride = 300f;
				previousPlayerHeldBy.voiceMuffledByEnemy = true;
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Caught exception while attempting to muffle player voice from mask item: {arg}");
		}
		if (base.IsOwner)
		{
			HUDManager.Instance.UIAudio.PlayOneShot(maskAttachAudioLocal, 1f);
		}
		else
		{
			previousPlayerHeldBy.movementAudio.PlayOneShot(maskAttachAudio, 1f);
			WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.movementAudio, maskAttachAudio);
		}
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.6f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
	}

	public void MaskClampToHeadAnimationEvent()
	{
		Debug.Log("Mask clamp animation event called");
		if (attaching && !(previousPlayerHeldBy == null))
		{
			Debug.Log("Creating currentHeadMask");
			currentHeadMask = UnityEngine.Object.Instantiate(headMaskPrefab, null).transform;
			PositionHeadMaskWithOffset();
			previousPlayerHeldBy.playerBodyAnimator.SetBool("HoldMask", value: false);
			Debug.Log($"Destroying object in hand; headmask null: {currentHeadMask == null}");
			DestroyObjectInHand(previousPlayerHeldBy);
			clampedToHead = true;
		}
	}

	private void FinishAttaching()
	{
		if (base.IsOwner && !finishedAttaching)
		{
			finishedAttaching = true;
			if (!previousPlayerHeldBy.AllowPlayerDeath())
			{
				Debug.Log("Player could not die so the mask did not spawn a mimic");
				CancelAttachToPlayerOnLocalClient();
				return;
			}
			bool isInsideFactory = previousPlayerHeldBy.isInsideFactory;
			Vector3 position = previousPlayerHeldBy.transform.position;
			previousPlayerHeldBy.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Suffocation, maskTypeId);
			CreateMimicServerRpc(isInsideFactory, position);
		}
	}

	[ServerRpc]
	public void CreateMimicServerRpc(bool inFactory, Vector3 playerPositionAtDeath)
{		if (previousPlayerHeldBy == null)
		{
			Debug.LogError("Previousplayerheldby is null so the mask mimic could not be spawned");
		}
		Debug.Log("Server creating mimic from mask");
		Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(playerPositionAtDeath, default(NavMeshHit), 10f);
		if (RoundManager.Instance.GotNavMeshPositionResult)
		{
			if (mimicEnemy == null)
			{
				Debug.Log("No mimic enemy set for mask");
				return;
			}
			NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, previousPlayerHeldBy.transform.eulerAngles.y, -1, mimicEnemy);
			if (netObjectRef.TryGet(out var networkObject))
			{
				Debug.Log("Got network object for mask enemy");
				MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
				component.SetSuit(previousPlayerHeldBy.currentSuitID);
				component.mimickingPlayer = previousPlayerHeldBy;
				component.SetEnemyOutside(!inFactory);
				component.SetVisibilityOfMaskedEnemy();
				component.SetMaskType(maskTypeId);
				previousPlayerHeldBy.redirectToEnemy = component;
				if (previousPlayerHeldBy.deadBody != null)
				{
					previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
				}
			}
			CreateMimicClientRpc(netObjectRef, inFactory);
		}
		else
		{
			Debug.Log("No nav mesh found; no mimic could be created");
		}
}
	[ClientRpc]
	public void CreateMimicClientRpc(NetworkObjectReference netObjectRef, bool inFactory)
{if(!base.IsServer)			{
				StartCoroutine(waitForMimicEnemySpawn(netObjectRef, inFactory));
			}
}
	private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef, bool inFactory)
	{
		NetworkObject netObject = null;
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || netObjectRef.TryGet(out netObject));
		if (previousPlayerHeldBy.deadBody == null)
		{
			startTime = Time.realtimeSinceStartup;
			yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || previousPlayerHeldBy.deadBody != null);
		}
		if (!(previousPlayerHeldBy.deadBody == null))
		{
			previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
			if (netObject != null)
			{
				Debug.Log("Got network object for mask enemy client");
				MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
				component.mimickingPlayer = previousPlayerHeldBy;
				component.SetSuit(previousPlayerHeldBy.currentSuitID);
				component.SetEnemyOutside(!inFactory);
				component.SetVisibilityOfMaskedEnemy();
				component.SetMaskType(maskTypeId);
				previousPlayerHeldBy.redirectToEnemy = component;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (!maskIsHaunted || !base.IsOwner || previousPlayerHeldBy == null || !maskOn || !holdingLastFrame || finishedAttaching)
		{
			return;
		}
		if (!attaching)
		{
			if (!StartOfRound.Instance.shipIsLeaving && (!StartOfRound.Instance.inShipPhase || !(StartOfRound.Instance.testRoom == null)) && Time.realtimeSinceStartup > lastIntervalCheck)
			{
				lastIntervalCheck = Time.realtimeSinceStartup + 5f;
				if (UnityEngine.Random.Range(0, 100) < 65)
				{
					Debug.Log("Got 15% chance");
					BeginAttachment();
				}
			}
		}
		else
		{
			attachTimer -= Time.deltaTime;
			if (previousPlayerHeldBy.isPlayerDead)
			{
				CancelAttachToPlayerOnLocalClient();
			}
			else if (attachTimer <= 0f)
			{
				FinishAttaching();
			}
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (currentHeadMask != null)
		{
			UnityEngine.Object.Destroy(currentHeadMask.gameObject);
		}
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if (!(previousPlayerHeldBy == null) && clampedToHead && currentHeadMask != null)
		{
			if (previousPlayerHeldBy.isPlayerDead)
			{
				UnityEngine.Object.Destroy(currentHeadMask.gameObject);
			}
			else
			{
				PositionHeadMaskWithOffset();
			}
		}
	}

	private void PositionHeadMaskWithOffset()
	{
		if (base.IsOwner)
		{
			currentHeadMask.rotation = previousPlayerHeldBy.gameplayCamera.transform.rotation;
			currentHeadMask.Rotate(headRotationOffset);
			currentHeadMask.position = previousPlayerHeldBy.gameplayCamera.transform.position;
			Vector3 vector = headPositionOffset;
			vector = previousPlayerHeldBy.gameplayCamera.transform.rotation * vector;
			currentHeadMask.position += vector;
		}
		else
		{
			currentHeadMask.rotation = previousPlayerHeldBy.playerGlobalHead.rotation;
			currentHeadMask.Rotate(headRotationOffset);
			currentHeadMask.position = previousPlayerHeldBy.playerGlobalHead.position;
			Vector3 vector2 = headPositionOffset + Vector3.up * 0.25f;
			vector2 = previousPlayerHeldBy.playerGlobalHead.rotation * vector2;
			currentHeadMask.position += vector2;
		}
	}
}
