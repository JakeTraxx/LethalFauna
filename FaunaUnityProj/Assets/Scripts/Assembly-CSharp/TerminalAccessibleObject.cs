using System.Collections;
using GameNetcodeStuff;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TerminalAccessibleObject : NetworkBehaviour
{
	public string objectCode;

	public float codeAccessCooldownTimer;

	private float currentCooldownTimer;

	private bool inCooldown;

	public InteractEvent terminalCodeEvent;

	public InteractEvent terminalCodeCooldownEvent;

	public bool setCodeRandomlyFromRoundManager = true;

	[Space(3f)]
	public MeshRenderer[] codeMaterials;

	public int rows;

	public int columns;

	[Space(3f)]
	public bool isBigDoor = true;

	private TextMeshProUGUI mapRadarText;

	private Image mapRadarBox;

	private Image mapRadarBoxSlider;

	private bool initializedValues;

	private bool playerHitDoorTrigger;

	private bool isDoorOpen;

	private bool isPoweredOn = true;

	public GameObject mapRadarObject;

	public void CallFunctionFromTerminal()
	{
		if (!inCooldown)
		{
			terminalCodeEvent.Invoke(GameNetworkManager.Instance.localPlayerController);
			if (codeAccessCooldownTimer > 0f)
			{
				currentCooldownTimer = codeAccessCooldownTimer;
				StartCoroutine(countCodeAccessCooldown());
			}
			Debug.Log("calling terminal function for code : " + objectCode + "; object name: " + base.gameObject.name);
		}
	}

	public void TerminalCodeCooldownReached()
	{
		terminalCodeCooldownEvent.Invoke(null);
		Debug.Log("cooldown reached for object with code : " + objectCode + "; object name: " + base.gameObject.name);
	}

	private IEnumerator countCodeAccessCooldown()
	{
		inCooldown = true;
		if (!initializedValues)
		{
			InitializeValues();
		}
		Image cooldownBar = mapRadarBox;
		Image[] componentsInChildren = mapRadarText.gameObject.GetComponentsInChildren<Image>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].type == Image.Type.Filled)
			{
				cooldownBar = componentsInChildren[i];
			}
		}
		cooldownBar.enabled = true;
		mapRadarText.color = Color.red;
		mapRadarBox.color = Color.red;
		while (currentCooldownTimer > 0f)
		{
			yield return null;
			currentCooldownTimer -= Time.deltaTime;
			cooldownBar.fillAmount = currentCooldownTimer / codeAccessCooldownTimer;
		}
		TerminalCodeCooldownReached();
		mapRadarText.color = Color.green;
		mapRadarBox.color = Color.green;
		currentCooldownTimer = 1.5f;
		int frameNum = 0;
		while (currentCooldownTimer > 0f)
		{
			yield return null;
			currentCooldownTimer -= Time.deltaTime;
			cooldownBar.fillAmount = Mathf.Abs(currentCooldownTimer / 1.5f - 1f);
			frameNum++;
			if (frameNum % 7 == 0)
			{
				mapRadarText.enabled = !mapRadarText.enabled;
			}
		}
		mapRadarText.enabled = true;
		cooldownBar.enabled = false;
		inCooldown = false;
	}

	public void OnPowerSwitch(bool switchedOn)
	{
		isPoweredOn = switchedOn;
		if (!switchedOn)
		{
			mapRadarText.color = Color.gray;
			mapRadarBox.color = Color.gray;
			if (!isDoorOpen)
			{
				base.gameObject.GetComponent<AnimatedObjectTrigger>().SetBoolOnClientOnly(setTo: true);
			}
		}
		else if (!isDoorOpen)
		{
			mapRadarText.color = Color.red;
			mapRadarBox.color = Color.red;
			base.gameObject.GetComponent<AnimatedObjectTrigger>().SetBoolOnClientOnly(setTo: false);
		}
		else
		{
			mapRadarText.color = Color.green;
			mapRadarBox.color = Color.green;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetDoorOpenServerRpc(bool open)
			{
				SetDoorOpenClientRpc(open);
			}

	[ClientRpc]
	public void SetDoorOpenClientRpc(bool open)
			{
				SetDoorOpen(open);
			}

	public void SetDoorToggleLocalClient()
	{
		if (isPoweredOn)
		{
			SetDoorOpen(!isDoorOpen);
			SetDoorOpenServerRpc(isDoorOpen);
		}
	}

	public void SetDoorLocalClient(bool open)
	{
		SetDoorOpen(open);
		SetDoorOpenServerRpc(isDoorOpen);
	}

	public void SetDoorOpen(bool open)
	{
		if (isBigDoor && isDoorOpen != open && isPoweredOn)
		{
			isDoorOpen = open;
			if (open)
			{
				Debug.Log("Setting door " + base.gameObject.name + " with code " + objectCode + " to open");
				mapRadarText.color = Color.green;
				mapRadarBox.color = Color.green;
			}
			else
			{
				Debug.Log("Setting door " + base.gameObject.name + " with code " + objectCode + " to closed");
				mapRadarText.color = Color.red;
				mapRadarBox.color = Color.red;
			}
			Debug.Log($"setting big door open for door {base.gameObject.name}; {isDoorOpen}; {open}");
			base.gameObject.GetComponent<AnimatedObjectTrigger>().SetBoolOnClientOnly(open);
		}
	}

	public void SetCodeTo(int codeIndex)
	{
		if (!setCodeRandomlyFromRoundManager)
		{
			return;
		}
		if (codeIndex > RoundManager.Instance.possibleCodesForBigDoors.Length)
		{
			Debug.LogError("Attempted setting code to an index higher than the amount of possible codes in TerminalAccessibleObject");
			return;
		}
		objectCode = RoundManager.Instance.possibleCodesForBigDoors[codeIndex];
		SetMaterialUV(codeIndex);
		if (mapRadarText == null)
		{
			InitializeValues();
		}
		mapRadarText.text = objectCode;
	}

	private void Start()
	{
		InitializeValues();
	}

	private void Update()
	{
		if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom || StartOfRound.Instance.mapScreen.overrideRadarCameraOnAlways)
		{
			if (Vector3.Distance(StartOfRound.Instance.mapScreen.mapCamera.transform.position, mapRadarObject.transform.position) < 8f)
			{
				mapRadarObject.transform.localScale = Vector3.Lerp(mapRadarObject.transform.localScale, new Vector3(1.8f, 1.8f, 1.8f), Time.deltaTime * 10f);
			}
			else
			{
				mapRadarObject.transform.localScale = Vector3.Lerp(mapRadarObject.transform.localScale, new Vector3(1f, 1f, 1f), Time.deltaTime * 10f);
			}
		}
	}

	public void InitializeValues()
	{
		if (initializedValues)
		{
			return;
		}
		initializedValues = true;
		mapRadarObject = Object.Instantiate(StartOfRound.Instance.objectCodePrefab, StartOfRound.Instance.mapScreen.mapCameraStationaryUI, worldPositionStays: false);
		Debug.Log($"{base.gameObject.name} creating doorcode object in parent {StartOfRound.Instance.mapScreen.mapCameraStationaryUI}: {mapRadarObject.name}");
		RectTransform component = mapRadarObject.GetComponent<RectTransform>();
		component.position = base.transform.position + Vector3.up * 1.35f;
		component.position += component.up * 1.2f - component.right * 1.2f;
		mapRadarText = mapRadarObject.GetComponentInChildren<TextMeshProUGUI>();
		mapRadarText.text = objectCode;
		mapRadarBox = mapRadarObject.GetComponentInChildren<Image>();
		if (isBigDoor)
		{
			SetDoorOpen(base.gameObject.GetComponent<AnimatedObjectTrigger>().boolValue);
			if (base.gameObject.GetComponent<AnimatedObjectTrigger>().boolValue)
			{
				mapRadarText.color = Color.green;
				mapRadarBox.color = Color.green;
			}
			else
			{
				mapRadarText.color = Color.red;
				mapRadarBox.color = Color.red;
			}
		}
	}

	public override void OnDestroy()
	{
		if (mapRadarText != null && mapRadarText.gameObject != null)
		{
			Object.Destroy(mapRadarText.gameObject);
		}
		base.OnDestroy();
	}

	private void SetMaterialUV(int codeIndex)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < codeIndex; i++)
		{
			num += 1f / (float)columns;
			if (num >= 1f)
			{
				num = 0f;
				num2 += 1f / (float)rows;
				if (num2 >= 1f)
				{
					num2 = 0f;
				}
			}
		}
		if (codeMaterials != null && codeMaterials.Length != 0)
		{
			Material material = codeMaterials[0].material;
			material.SetTextureOffset("_BaseColorMap", new Vector2(num, num2));
			for (int j = 0; j < codeMaterials.Length; j++)
			{
				codeMaterials[j].sharedMaterial = material;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (isBigDoor && !playerHitDoorTrigger && (!isDoorOpen || !isPoweredOn) && other.CompareTag("Player") && other.gameObject.GetComponent<PlayerControllerB>() == GameNetworkManager.Instance.localPlayerController)
		{
			playerHitDoorTrigger = true;
			HUDManager.Instance.DisplayTip("TIP:", "Use the ship computer terminal to access secure doors.", isWarning: false, useSave: true, "LCTip_SecureDoors");
		}
	}
}
