using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ManualCameraRenderer : NetworkBehaviour
{
	public Camera cam;

	public CameraView[] cameraViews;

	public int cameraViewIndex;

	public bool currentCameraDisabled;

	[Space(5f)]
	public MeshRenderer mesh;

	public Material offScreenMat;

	public Material onScreenMat;

	public int materialIndex;

	private bool isScreenOn;

	public bool overrideCameraForOtherUse;

	public bool renderAtLowerFramerate;

	public float fps = 60f;

	private float elapsed;

	public PlayerControllerB targetedPlayer;

	public List<TransformAndName> radarTargets = new List<TransformAndName>();

	public int targetTransformIndex;

	public Camera mapCamera;

	public Animator mapCameraAnimator;

	private bool mapCameraMaxFramerate;

	private Coroutine updateMapCameraCoroutine;

	private bool syncingTargetPlayer;

	private bool syncingSwitchScreen;

	private bool screenEnabledOnLocalClient;

	private Vector3 targetDeathPosition;

	public Transform mapCameraStationaryUI;

	public Transform shipArrowPointer;

	public GameObject shipArrowUI;

	[Header("Radar camera debugging")]
	public float cameraNearPlane = -2.47f;

	public float cameraFarPlane = 7.52f;

	public bool overrideRadarCameraOnAlways;

	public Volume lensDistortionVolume;

	private RaycastHit rayHit;

	[Space(5f)]
	private Transform headMountedCamTarget;

	public bool enableHeadMountedCam;

	public Camera headMountedCam;

	public RawImage headMountedCamUI;

	public Vector3 headMountedCamPositionOffset;

	public Vector3 headMountedCamRotationOffset;

	public Image localPlayerPlaceholder;

	private NavMeshPath path1;

	public LineRenderer lineFromRadarTargetToExit;

	private float updateLineInterval;

	private float setLineIntervalTo;

	public Material radarLineMaterial;

	private float dottedLineOffset;

	private GameObject[] caveNodes;

	public bool gotCaveNodes;

	private float checkCaveInterval;

	private bool playerIsInCaves;

	private int checkingCaveNode;

	public GameObject LostSignalUI;

	public Image compassRose;

	public GameObject contourMap;

	private bool checkedForContourMap;

	private void Start()
	{
		if (cam == null)
		{
			cam = GetComponent<Camera>();
		}
		if (!isScreenOn)
		{
			cam.enabled = false;
		}
		targetDeathPosition = new Vector3(0f, -100f, 0f);
		if (cam == mapCamera)
		{
			path1 = new NavMeshPath();
			lineFromRadarTargetToExit.positionCount = 20;
			checkingCaveNode = -1;
		}
	}

	private void Awake()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			radarTargets.Add(new TransformAndName(StartOfRound.Instance.allPlayerScripts[i].transform, StartOfRound.Instance.allPlayerScripts[i].playerUsername));
		}
		targetTransformIndex = 0;
		targetedPlayer = StartOfRound.Instance.allPlayerScripts[0];
	}

	public void SwitchScreenButton()
	{
		bool on = !isScreenOn;
		SwitchScreenOn(on);
		syncingSwitchScreen = true;
		SwitchScreenOnServerRpc(on);
	}

	public void SwitchScreenOn(bool on = true)
	{
		isScreenOn = on;
		currentCameraDisabled = !on;
		Material[] sharedMaterials = mesh.sharedMaterials;
		if (on)
		{
			sharedMaterials[materialIndex] = onScreenMat;
			mapCameraAnimator.SetTrigger("Transition");
		}
		else
		{
			sharedMaterials[materialIndex] = offScreenMat;
		}
		mesh.sharedMaterials = sharedMaterials;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchScreenOnServerRpc(bool on)
			{
				SwitchScreenOnClientRpc(on);
			}

	[ClientRpc]
	public void SwitchScreenOnClientRpc(bool on)
{		{
			if (syncingSwitchScreen)
			{
				syncingSwitchScreen = false;
			}
			else
			{
				SwitchScreenOn(on);
			}
		}
}
	public void SwitchCameraView(bool switchForward = true, int switchToView = -1)
	{
		cam.enabled = false;
		cameraViewIndex = (cameraViewIndex + 1) % cameraViews.Length;
		cam = cameraViews[cameraViewIndex].camera;
		onScreenMat = cameraViews[cameraViewIndex].cameraMaterial;
	}

	public string AddTransformAsTargetToRadar(Transform newTargetTransform, string targetName, bool isNonPlayer = false)
	{
		int num = 0;
		for (int i = 0; i < radarTargets.Count; i++)
		{
			if (radarTargets[i].transform == newTargetTransform)
			{
				return null;
			}
			if (radarTargets[i].name == targetName)
			{
				num++;
			}
		}
		if (num != 0)
		{
			targetName += num + 1;
		}
		if (!newTargetTransform.GetComponent<NetworkObject>())
		{
			return null;
		}
		radarTargets.Add(new TransformAndName(newTargetTransform, targetName, isNonPlayer));
		return targetName;
	}

	public void ChangeNameOfTargetTransform(Transform t, string newName)
	{
		for (int i = 0; i < radarTargets.Count; i++)
		{
			if (radarTargets[i].transform == t)
			{
				radarTargets[i].name = newName;
			}
		}
	}

	public void SyncOrderOfRadarBoostersInList()
	{
		radarTargets = radarTargets.OrderBy((TransformAndName x) => x.transform.gameObject.GetComponent<NetworkObject>().NetworkObjectId).ToList();
	}

	public void FlashRadarBooster(int targetId)
	{
		if (targetId < radarTargets.Count && radarTargets[targetId].isNonPlayer)
		{
			RadarBoosterItem component = radarTargets[targetId].transform.gameObject.GetComponent<RadarBoosterItem>();
			if (component != null)
			{
				component.FlashAndSync();
			}
		}
	}

	public void PingRadarBooster(int targetId)
	{
		if (targetId < radarTargets.Count && radarTargets[targetId].isNonPlayer)
		{
			RadarBoosterItem component = radarTargets[targetId].transform.gameObject.GetComponent<RadarBoosterItem>();
			if (component != null)
			{
				component.PlayPingAudioAndSync();
			}
		}
	}

	public void RemoveTargetFromRadar(Transform removeTransform)
	{
		for (int i = 0; i < radarTargets.Count; i++)
		{
			if (radarTargets[i].transform == removeTransform)
			{
				radarTargets.RemoveAt(i);
				if (targetTransformIndex >= radarTargets.Count)
				{
					targetTransformIndex--;
					SwitchRadarTargetForward(callRPC: false);
				}
			}
		}
	}

	public void SwitchRadarTargetForward(bool callRPC)
	{
		if (updateMapCameraCoroutine != null)
		{
			StopCoroutine(updateMapCameraCoroutine);
		}
		updateMapCameraCoroutine = StartCoroutine(updateMapTarget(GetRadarTargetIndexPlusOne(targetTransformIndex), !callRPC));
	}

	public void SwitchRadarTargetAndSync(int switchToIndex)
	{
		if (radarTargets.Count > switchToIndex)
		{
			if (updateMapCameraCoroutine != null)
			{
				StopCoroutine(updateMapCameraCoroutine);
			}
			updateMapCameraCoroutine = StartCoroutine(updateMapTarget(switchToIndex, calledFromRPC: false));
		}
	}

	private int GetRadarTargetIndexPlusOne(int index)
	{
		return (index + 1) % radarTargets.Count;
	}

	private int GetRadarTargetIndexMinusOne(int index)
	{
		if (index - 1 < 0)
		{
			return radarTargets.Count - 1;
		}
		return index - 1;
	}

	private IEnumerator updateMapTarget(int setRadarTargetIndex, bool calledFromRPC = true)
	{
		if (screenEnabledOnLocalClient)
		{
			mapCameraMaxFramerate = true;
			mapCameraAnimator.SetTrigger("Transition");
		}
		yield return new WaitForSeconds(0.035f);
		if (radarTargets.Count <= setRadarTargetIndex)
		{
			setRadarTargetIndex = radarTargets.Count - 1;
		}
		PlayerControllerB component = radarTargets[setRadarTargetIndex].transform.gameObject.GetComponent<PlayerControllerB>();
		if (!calledFromRPC)
		{
			for (int i = 0; i < radarTargets.Count; i++)
			{
				Debug.Log($"radar target index {i}");
				if (radarTargets[setRadarTargetIndex] == null)
				{
					setRadarTargetIndex = GetRadarTargetIndexPlusOne(setRadarTargetIndex);
					continue;
				}
				component = radarTargets[setRadarTargetIndex].transform.gameObject.GetComponent<PlayerControllerB>();
				if (!(component != null) || component.isPlayerControlled || component.isPlayerDead || !(component.redirectToEnemy == null))
				{
					break;
				}
				setRadarTargetIndex = GetRadarTargetIndexPlusOne(setRadarTargetIndex);
			}
		}
		if (radarTargets[setRadarTargetIndex] == null)
		{
			Debug.Log($"Radar attempted to target object which doesn't exist; index {setRadarTargetIndex}");
			yield break;
		}
		if (targetedPlayer != null)
		{
			targetedPlayer.nightVisionRadar.enabled = false;
			if (targetedPlayer.deadBody != null && targetedPlayer.deadBody.nightVisionRadar != null)
			{
				targetedPlayer.deadBody.nightVisionRadar.enabled = false;
			}
		}
		targetTransformIndex = setRadarTargetIndex;
		targetedPlayer = component;
		checkingCaveNode = -1;
		checkCaveInterval = 0f;
		StartOfRound.Instance.mapScreenPlayerName.text = radarTargets[targetTransformIndex].name ?? "";
		mapCameraMaxFramerate = false;
		if (!calledFromRPC)
		{
			SwitchRadarTargetServerRpc(targetTransformIndex);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchRadarTargetServerRpc(int targetIndex)
			{
				SwitchRadarTargetClientRpc(targetIndex);
			}

	[ClientRpc]
	public void SwitchRadarTargetClientRpc(int switchToIndex)
{		if (syncingTargetPlayer)
		{
			syncingTargetPlayer = false;
		}
		else
		{
			if (radarTargets.Count <= switchToIndex)
			{
				return;
			}
			if (!isScreenOn)
			{
				if (switchToIndex == -1)
				{
					return;
				}
				SwitchScreenOn();
			}
			if (updateMapCameraCoroutine != null)
			{
				StopCoroutine(updateMapCameraCoroutine);
			}
			updateMapCameraCoroutine = StartCoroutine(updateMapTarget(switchToIndex));
		}
}
	private void MapCameraFocusOnPosition(Vector3 pos)
	{
		if (!(GameNetworkManager.Instance.localPlayerController == null))
		{
			_ = radarTargets[targetTransformIndex].transform.position;
			if (targetedPlayer != null && targetedPlayer.isInHangarShipRoom)
			{
				mapCamera.nearClipPlane = -0.96f;
				mapCamera.farClipPlane = 7.52f;
				StartOfRound.Instance.radarCanvas.planeDistance = -0.93f;
			}
			else
			{
				mapCamera.nearClipPlane = cameraNearPlane;
				mapCamera.farClipPlane = cameraFarPlane;
				StartOfRound.Instance.radarCanvas.planeDistance = -0.93f;
			}
			mapCamera.transform.position = new Vector3(pos.x, pos.y + 3.636f, pos.z);
		}
	}

	private void SetLineToExitFromRadarTarget()
	{
		if (!screenEnabledOnLocalClient || playerIsInCaves)
		{
			lineFromRadarTargetToExit.enabled = false;
			return;
		}
		if (!targetedPlayer.isInsideFactory)
		{
			lineFromRadarTargetToExit.enabled = false;
			return;
		}
		lineFromRadarTargetToExit.enabled = true;
		if (updateLineInterval > 0f)
		{
			updateLineInterval -= Time.deltaTime;
			dottedLineOffset -= Time.deltaTime;
			lineFromRadarTargetToExit.material.SetTextureOffset("_MainTex", new Vector2(dottedLineOffset, 0f));
			if (setLineIntervalTo < 1f)
			{
				lineFromRadarTargetToExit.SetPosition(0, mapCamera.transform.position - Vector3.up * 2.5f);
			}
			return;
		}
		Vector3 vector = RoundManager.FindMainEntrancePosition(getTeleportPosition: true);
		if (vector == Vector3.zero)
		{
			return;
		}
		if (!NavMesh.CalculatePath(mapCamera.transform.position - Vector3.up * 3.75f, vector, -1, path1))
		{
			Debug.Log("Path to exit could not be calculated");
			return;
		}
		if (path1.corners.Length > 50)
		{
			setLineIntervalTo = 2f;
		}
		else if (path1.corners.Length < 36)
		{
			setLineIntervalTo = 0.4f;
		}
		lineFromRadarTargetToExit.SetPositions(path1.corners);
		for (int i = 0; i < lineFromRadarTargetToExit.positionCount; i++)
		{
			lineFromRadarTargetToExit.SetPosition(i, lineFromRadarTargetToExit.GetPosition(i) + Vector3.up * 1.25f);
		}
		updateLineInterval = setLineIntervalTo;
	}

	private void CheckIfPlayerIsInCaves(Vector3 targetPosition)
	{
		if (!screenEnabledOnLocalClient && !overrideRadarCameraOnAlways)
		{
			return;
		}
		if (!targetedPlayer.isInsideFactory)
		{
			playerIsInCaves = false;
		}
		else if (checkingCaveNode != -1 && gotCaveNodes)
		{
			if (caveNodes[checkingCaveNode] == null)
			{
				gotCaveNodes = false;
				checkingCaveNode = -1;
				return;
			}
			if (Vector3.Distance(targetPosition, caveNodes[checkingCaveNode].transform.position) < 8f)
			{
				playerIsInCaves = true;
				checkingCaveNode = -1;
				return;
			}
			checkingCaveNode++;
			if (checkingCaveNode >= caveNodes.Length)
			{
				checkingCaveNode = -1;
				playerIsInCaves = false;
			}
		}
		else if (checkCaveInterval <= 0f)
		{
			checkCaveInterval = 1f;
			Debug.DrawRay(targetPosition + Vector3.up * 0.25f, Vector3.down, Color.red, 1f);
			if (!targetedPlayer.isInsideFactory || RoundManager.Instance.currentDungeonType != 4 || !Physics.Raycast(targetPosition + Vector3.up * 0.25f, Vector3.down, out rayHit, 4f, 256, QueryTriggerInteraction.Ignore))
			{
				return;
			}
			Debug.Log(string.Format("rayhit collider tag: {0}; {1} ; {2}; {3}", rayHit.collider.tag, rayHit.collider.CompareTag("Rock"), rayHit.transform.CompareTag("Rock"), rayHit.transform.gameObject.CompareTag("Rock")));
			bool flag = rayHit.collider.CompareTag("Rock");
			if (!playerIsInCaves)
			{
				if (flag)
				{
					if (!gotCaveNodes)
					{
						caveNodes = GameObject.FindGameObjectsWithTag("CaveNode");
						gotCaveNodes = true;
					}
					checkingCaveNode = 0;
				}
			}
			else if (!flag)
			{
				if (!gotCaveNodes)
				{
					caveNodes = GameObject.FindGameObjectsWithTag("CaveNode");
					gotCaveNodes = true;
				}
				checkingCaveNode = 0;
			}
		}
		else
		{
			checkCaveInterval -= Time.deltaTime;
		}
	}

	private void Update()
	{
		if (GameNetworkManager.Instance.localPlayerController == null || NetworkManager.Singleton == null)
		{
			return;
		}
		if (lensDistortionVolume != null)
		{
			lensDistortionVolume.weight = 0f;
		}
		if (overrideCameraForOtherUse)
		{
			if (shipArrowUI != null)
			{
				shipArrowUI.SetActive(value: false);
			}
			return;
		}
		if (cam == mapCamera)
		{
			if (lensDistortionVolume != null)
			{
				lensDistortionVolume.weight = 1f;
			}
			if (targetedPlayer != null)
			{
				if (targetedPlayer.isPlayerDead)
				{
					if ((bool)targetedPlayer.redirectToEnemy)
					{
						MapCameraFocusOnPosition(targetedPlayer.redirectToEnemy.transform.position);
						CheckIfPlayerIsInCaves(targetedPlayer.redirectToEnemy.transform.position);
						MaskedPlayerEnemy maskedPlayerEnemy = targetedPlayer.redirectToEnemy as MaskedPlayerEnemy;
						if ((bool)maskedPlayerEnemy)
						{
							headMountedCamTarget = maskedPlayerEnemy.headTransform;
							enableHeadMountedCam = true;
						}
						else
						{
							enableHeadMountedCam = false;
						}
					}
					else if (targetedPlayer.deadBody != null)
					{
						CheckIfPlayerIsInCaves(targetedPlayer.deadBody.transform.position);
						MapCameraFocusOnPosition(targetedPlayer.deadBody.transform.position);
						targetDeathPosition = targetedPlayer.deadBody.spawnPosition;
						headMountedCamTarget = targetedPlayer.deadBody.bodyParts[0].transform;
						enableHeadMountedCam = true;
					}
					else
					{
						CheckIfPlayerIsInCaves(targetedPlayer.placeOfDeath);
						MapCameraFocusOnPosition(targetedPlayer.placeOfDeath);
						enableHeadMountedCam = false;
					}
				}
				else
				{
					CheckIfPlayerIsInCaves(targetedPlayer.transform.position);
					Vector3 pos = targetedPlayer.transform.position;
					if (Physics.Raycast(targetedPlayer.transform.position + Vector3.up * 0.1f, Vector3.down, out rayHit, 5f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
					{
						pos = rayHit.point + Vector3.up * 0.06f;
					}
					headMountedCamTarget = targetedPlayer.playerGlobalHead.transform;
					enableHeadMountedCam = true;
					MapCameraFocusOnPosition(pos);
				}
				SetLineToExitFromRadarTarget();
			}
			else
			{
				enableHeadMountedCam = true;
				playerIsInCaves = false;
				headMountedCamTarget = radarTargets[targetTransformIndex].transform;
				lineFromRadarTargetToExit.enabled = false;
				MapCameraFocusOnPosition(radarTargets[targetTransformIndex].transform.position);
			}
			if (mapCameraMaxFramerate)
			{
				mapCamera.enabled = true;
				return;
			}
		}
		else
		{
			enableHeadMountedCam = false;
		}
		PlayerControllerB player = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
		if (!MeetsCameraEnabledConditions(player))
		{
			screenEnabledOnLocalClient = false;
			cam.enabled = false;
			enableHeadMountedCam = false;
			return;
		}
		if (cam == mapCamera)
		{
			if (!checkedForContourMap && contourMap == null)
			{
				contourMap = GameObject.FindGameObjectWithTag("TerrainContourMap");
				checkedForContourMap = true;
			}
			if (contourMap != null)
			{
				if ((targetedPlayer != null && !targetedPlayer.isInsideFactory) || (targetedPlayer == null && headMountedCamTarget.transform.position.y > -80f))
				{
					contourMap.SetActive(value: true);
					contourMap.transform.position = new Vector3(contourMap.transform.position.x, headMountedCamTarget.transform.position.y - 1.5f, contourMap.transform.position.z);
				}
				else
				{
					contourMap.SetActive(value: false);
				}
			}
			if (radarTargets[targetTransformIndex].transform != null)
			{
				if (!(radarTargets[targetTransformIndex].transform.position.y < -80f) && Vector3.Distance(radarTargets[targetTransformIndex].transform.position, StartOfRound.Instance.elevatorTransform.transform.position) > 16f)
				{
					shipArrowPointer.LookAt(StartOfRound.Instance.elevatorTransform);
					shipArrowPointer.eulerAngles = new Vector3(0f, shipArrowPointer.eulerAngles.y, 0f);
					shipArrowUI.SetActive(value: true);
				}
				else
				{
					shipArrowUI.SetActive(value: false);
				}
			}
		}
		screenEnabledOnLocalClient = true;
		if (renderAtLowerFramerate)
		{
			cam.enabled = false;
			elapsed += Time.deltaTime;
			if (elapsed > 1f / fps)
			{
				elapsed = 0f;
				cam.Render();
			}
		}
		else
		{
			cam.enabled = true;
		}
	}

	private void LateUpdate()
	{
		if (GameNetworkManager.Instance.localPlayerController == null || !(headMountedCam != null))
		{
			return;
		}
		if (targetedPlayer == GameNetworkManager.Instance.localPlayerController && !GameNetworkManager.Instance.localPlayerController.isPlayerDead && !overrideRadarCameraOnAlways)
		{
			enableHeadMountedCam = false;
			if (!StartOfRound.Instance.inShipPhase)
			{
				if (StartOfRound.Instance.mapScreenPlayerName.enabled)
				{
					localPlayerPlaceholder.enabled = true;
				}
			}
			else
			{
				localPlayerPlaceholder.enabled = false;
			}
		}
		else
		{
			localPlayerPlaceholder.enabled = false;
		}
		if (StartOfRound.Instance.inShipPhase)
		{
			enableHeadMountedCam = false;
		}
		if (targetedPlayer != null && !targetedPlayer.isPlayerControlled && !targetedPlayer.isPlayerDead)
		{
			enableHeadMountedCam = false;
		}
		if (headMountedCamTarget == null)
		{
			enableHeadMountedCam = false;
		}
		headMountedCam.enabled = enableHeadMountedCam;
		headMountedCamUI.enabled = enableHeadMountedCam;
		if (targetedPlayer != null)
		{
			PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
			{
				playerControllerB = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
			}
			targetedPlayer.nightVisionRadar.enabled = playerControllerB.isInHangarShipRoom && targetedPlayer != playerControllerB && Vector3.Distance(targetedPlayer.transform.position, StartOfRound.Instance.elevatorTransform.position) > 20f;
			if (targetedPlayer.isPlayerDead && targetedPlayer.deadBody != null)
			{
				if (targetedPlayer.nightVisionRadar.enabled && Vector3.Distance(targetedPlayer.deadBody.bodyParts[0].transform.position, StartOfRound.Instance.elevatorTransform.position) > 20f)
				{
					if (targetedPlayer.deadBody.nightVisionRadar != null)
					{
						targetedPlayer.deadBody.nightVisionRadar.enabled = true;
					}
				}
				else
				{
					targetedPlayer.deadBody.nightVisionRadar.enabled = false;
				}
			}
		}
		if (enableHeadMountedCam)
		{
			if (targetedPlayer == null)
			{
				headMountedCam.transform.position = headMountedCamTarget.transform.position + headMountedCamTarget.up * 1.557f + headMountedCamTarget.forward * 0.449f;
				headMountedCam.transform.rotation = headMountedCamTarget.transform.rotation;
				headMountedCam.transform.Rotate(8.941f, -177.83f, 0f, Space.Self);
			}
			else
			{
				headMountedCam.transform.position = headMountedCamTarget.transform.position + headMountedCamTarget.up * 0.237f + headMountedCamTarget.forward * 0.4f;
				headMountedCam.transform.rotation = headMountedCamTarget.transform.rotation;
				headMountedCam.transform.Rotate(14.656f, -184.93f, 0f, Space.Self);
			}
		}
		if (playerIsInCaves)
		{
			headMountedCam.enabled = false;
			headMountedCamUI.enabled = false;
		}
	}

	private bool MeetsCameraEnabledConditions(PlayerControllerB player)
	{
		if (currentCameraDisabled)
		{
			return false;
		}
		if (mesh != null && !mesh.isVisible)
		{
			return false;
		}
		if (!StartOfRound.Instance.inShipPhase)
		{
			if (LostSignalUI != null)
			{
				LostSignalUI.SetActive(playerIsInCaves);
			}
			if ((!player.isInHangarShipRoom && !overrideRadarCameraOnAlways) || (!StartOfRound.Instance.shipDoorsEnabled && (StartOfRound.Instance.currentPlanetPrefab == null || !StartOfRound.Instance.currentPlanetPrefab.activeSelf)))
			{
				return false;
			}
		}
		else
		{
			gotCaveNodes = false;
			checkingCaveNode = -1;
			enableHeadMountedCam = false;
			checkedForContourMap = false;
			if (LostSignalUI != null)
			{
				LostSignalUI.SetActive(value: false);
			}
		}
		return true;
	}
}
