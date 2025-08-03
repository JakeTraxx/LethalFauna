using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dissonance;
using GameNetcodeStuff;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class HUDManager : NetworkBehaviour
{
	public Camera UICamera;

	public HUDElement Inventory;

	public HUDElement Chat;

	public HUDElement PlayerInfo;

	public HUDElement Tooltips;

	public HUDElement InstabilityCounter;

	public HUDElement Clock;

	public HUDElement Compass;

	private HUDElement[] HUDElements;

	public GameObject HUDContainer;

	public Animator playerScreenShakeAnimator;

	public RawImage playerScreenTexture;

	public Volume playerGraphicsVolume;

	public TextMeshProUGUI weightCounter;

	public Animator weightCounterAnimator;

	[Header("Item UI")]
	public UnityEngine.UI.Image[] itemSlotIcons;

	public UnityEngine.UI.Image[] itemSlotIconFrames;

	[Header("Tooltips")]
	public TextMeshProUGUI[] controlTipLines;

	[Header("Object Scanner")]
	private RaycastHit[] scanNodesHit;

	public RectTransform[] scanElements;

	private bool scanElementsHidden;

	private float playerPingingScan;

	private float updateScanInterval;

	private Dictionary<RectTransform, ScanNodeProperties> scanNodes = new Dictionary<RectTransform, ScanNodeProperties>();

	private List<ScanNodeProperties> nodesOnScreen = new List<ScanNodeProperties>();

	private TextMeshProUGUI[] scanElementText = new TextMeshProUGUI[2];

	public Animator scanEffectAnimator;

	public AudioClip scanSFX;

	public AudioClip addToScrapTotalSFX;

	public AudioClip finishAddingToTotalSFX;

	private float addToDisplayTotalInterval;

	private bool addingToDisplayTotal;

	[Space(3f)]
	public TextMeshProUGUI totalValueText;

	public Animator scanInfoAnimator;

	public int totalScrapScanned;

	private int totalScrapScannedDisplayNum;

	private int scannedScrapNum;

	private bool addedToScrapCounterThisFrame;

	[Header("Batteries")]
	public UnityEngine.UI.Image batteryIcon;

	public TextMeshProUGUI batteryInventoryNumber;

	public UnityEngine.UI.Image batteryMeter;

	[Header("Audio")]
	public AudioSource UIAudio;

	public AudioClip criticalInjury;

	public AudioLowPassFilter audioListenerLowPass;

	public AudioClip globalNotificationSFX;

	[Header("Misc UI elements")]
	public RawImage compassImage;

	public float compassOffset;

	public TextMeshProUGUI debugText;

	public GameObject errorLogPanel;

	public TextMeshProUGUI errorLogText;

	private string previousErrorReceived;

	public UnityEngine.UI.Image PTTIcon;

	public Animator batteryBlinkUI;

	public TextMeshProUGUI holdingTwoHandedItem;

	public CanvasGroup selfRedCanvasGroup;

	public UnityEngine.UI.Image holdInteractionFillAmount;

	public CanvasGroup holdInteractionCanvasGroup;

	public float holdFillAmount;

	public EndOfGameStatUIElements statsUIElements;

	public Animator gameOverAnimator;

	public Animator quotaAnimator;

	public TextMeshProUGUI HUDQuotaNumerator;

	public TextMeshProUGUI HUDQuotaDenominator;

	public Animator planetIntroAnimator;

	public Animator endgameStatsAnimator;

	public TextMeshProUGUI loadingText;

	public UnityEngine.UI.Image loadingDarkenScreen;

	public TextMeshProUGUI planetInfoSummaryText;

	public TextMeshProUGUI planetInfoHeaderText;

	public TextMeshProUGUI planetRiskLevelText;

	[Header("Text chat")]
	public TextMeshProUGUI chatText;

	public TextMeshProUGUI typingIndicator;

	public TMP_InputField chatTextField;

	public string lastChatMessage = "";

	public List<string> ChatMessageHistory = new List<string>();

	public Animator playerCouldRecieveTextChatAnimator;

	public StartOfRound playersManager;

	public PlayerActions playerActions;

	public PlayerControllerB localPlayer;

	private bool playerIsCriticallyInjured;

	public TextMeshProUGUI instabilityCounterNumber;

	public TextMeshProUGUI instabilityCounterText;

	private int previousInstability;

	private Terminal terminalScript;

	[Header("Special Graphics")]
	public bool retrievingSteamLeaderboard;

	public Animator signalTranslatorAnimator;

	public TextMeshProUGUI signalTranslatorText;

	public Animator alarmHornEffect;

	public AudioClip shipAlarmHornSFX;

	public Animator deviceChangeAnimator;

	public TextMeshProUGUI deviceChangeText;

	public Animator saveDataIconAnimatorB;

	public Animator HUDAnimator;

	public Animator radiationGraphicAnimator;

	public AudioClip radiationWarningAudio;

	public Animator meteorShowerGraphicAnimator;

	public AudioClip meteorShowerWarningAudio;

	public UnityEngine.UI.Image shipLeavingEarlyIcon;

	private float timeSinceDisplayingStatusEffect;

	public Animator statusEffectAnimator;

	public TextMeshProUGUI statusEffectText;

	[Space(3f)]
	public bool increaseHelmetCondensation;

	public Material helmetCondensationMaterial;

	[Space(3f)]
	public Animator moneyRewardsAnimator;

	public TextMeshProUGUI moneyRewardsTotalText;

	public TextMeshProUGUI moneyRewardsListText;

	private Coroutine scrollRewardTextCoroutine;

	public Scrollbar rewardsScrollbar;

	[Space(3f)]
	public CanvasGroup shockTutorialLeftAlpha;

	[Space(3f)]
	public CanvasGroup shockTutorialRightAlpha;

	public int tutorialArrowState;

	public bool setTutorialArrow;

	[Space(3f)]
	public Animator tipsPanelAnimator;

	public TextMeshProUGUI tipsPanelBody;

	public TextMeshProUGUI tipsPanelHeader;

	public AudioClip[] tipsSFX;

	public AudioClip[] warningSFX;

	private Coroutine tipsPanelCoroutine;

	private bool isDisplayingWarning;

	public Animator globalNotificationAnimator;

	public TextMeshProUGUI globalNotificationText;

	public bool sinkingCoveredFace;

	public Animator sinkingUnderAnimator;

	[Header("Dialogue Box")]
	private Coroutine readDialogueCoroutine;

	public TextMeshProUGUI dialogeBoxHeaderText;

	public TextMeshProUGUI dialogeBoxText;

	public Animator dialogueBoxAnimator;

	public AudioSource dialogueBoxSFX;

	public AudioClip[] dialogueBleeps;

	private Coroutine forceChangeTextCoroutine;

	private bool hudHidden;

	[Header("Advertisement UI")]
	public Transform advertItemParent;

	public AudioClip advertMusic;

	public AudioClip advertMusic2;

	public Animator advertAnimator;

	public TextMeshProUGUI advertTopText;

	public TextMeshProUGUI advertBottomText;

	public GameObject emptySuitPrefab;

	public GameObject advertItem;

	private Coroutine displayAdCoroutine;

	[Space(3f)]
	private bool hasLoadedSpectateUI;

	private bool hasGottenPlayerSteamProfilePictures;

	[Header("Spectate UI")]
	public GameObject spectatingPlayerBoxPrefab;

	public Transform SpectateBoxesContainer;

	private Dictionary<Animator, PlayerControllerB> spectatingPlayerBoxes = new Dictionary<Animator, PlayerControllerB>();

	private float updateSpectateBoxesInterval;

	private float yOffsetAmount;

	private int boxesAdded;

	public TextMeshProUGUI spectatingPlayerText;

	private bool displayedSpectatorAFKTip;

	public TextMeshProUGUI spectatorTipText;

	public TextMeshProUGUI holdButtonToEndGameEarlyText;

	public TextMeshProUGUI holdButtonToEndGameEarlyVotesText;

	public UnityEngine.UI.Image holdButtonToEndGameEarlyMeter;

	private float holdButtonToEndGameEarlyHoldTime;

	[Header("Time of day UI")]
	public TextMeshProUGUI clockNumber;

	public UnityEngine.UI.Image clockIcon;

	public Sprite[] clockIcons;

	private string amPM;

	private string newLine;

	[Space(5f)]
	public Animator gasHelmetAnimator;

	public Volume drunknessFilter;

	public CanvasGroup gasImageAlpha;

	public Volume insanityScreenFilter;

	public Volume flashbangScreenFilter;

	public float flashFilter;

	public Volume underwaterScreenFilter;

	public bool setUnderwaterFilter;

	public AudioSource breathingUnderwaterAudio;

	[Header("Player levelling")]
	public PlayerLevel[] playerLevels;

	public int localPlayerLevel;

	public int localPlayerXP;

	public TextMeshProUGUI playerLevelText;

	public TextMeshProUGUI playerLevelXPCounter;

	public UnityEngine.UI.Image playerLevelMeter;

	public AudioClip levelIncreaseSFX;

	public AudioClip levelDecreaseSFX;

	public AudioClip decreaseXPSFX;

	public AudioClip increaseXPSFX;

	public Animator playerLevelBoxAnimator;

	public AudioSource LevellingAudio;

	[Header("Profit quota/deadline")]
	public Animator reachedProfitQuotaAnimator;

	public TextMeshProUGUI newProfitQuotaText;

	public TextMeshProUGUI reachedProfitQuotaBonusText;

	public TextMeshProUGUI profitQuotaDaysLeftText;

	public TextMeshProUGUI profitQuotaDaysLeftText2;

	public AudioClip newProfitQuotaSFX;

	public AudioClip reachedQuotaSFX;

	public AudioClip OneDayToMeetQuotaSFX;

	public AudioClip profitQuotaDaysLeftCalmSFX;

	[Space(3f)]
	public Animator playersFiredAnimator;

	public TextMeshProUGUI EndOfRunStatsText;

	public bool displayingNewQuota;

	[Header("Displaying collected scrap")]
	public List<GrabbableObject> itemsToBeDisplayed = new List<GrabbableObject>();

	public ScrapItemHUDDisplay[] ScrapItemBoxes;

	private int boxesDisplaying;

	public Coroutine displayingItemCoroutine;

	private int bottomBoxIndex;

	public int bottomBoxYPosition;

	public Material hologramMaterial;

	public AudioClip displayCollectedScrapSFX;

	public AudioClip displayCollectedScrapSFXSmall;

	private int nextBoxIndex;

	[Space(5f)]
	public TextMeshProUGUI buildModeControlTip;

	public bool hasSetSavedValues;

	private float noLivingPlayersAtKeyboardTimer;

	public SpecialHUDMenuObject[] specialHUDMenus;

	public SpecialHUDMenu currentSpecialMenu;

	public Texture2D defaultCursorTex;

	public Texture2D handOpenCursorTex;

	public Texture2D handClosedCursorTex;

	public static HUDManager Instance { get; private set; }

	public void SetMouseCursorSprite(Texture2D cursorSprite, Vector2 hotspot = default(Vector2))
	{
		if (cursorSprite == null)
		{
			Cursor.SetCursor(defaultCursorTex, new Vector2(0f, 0f), CursorMode.Auto);
		}
		else
		{
			Cursor.SetCursor(cursorSprite, hotspot, CursorMode.Auto);
		}
	}

	public void OpenSpecialMenu(SpecialHUDMenu menu)
	{
		ShipBuildModeManager.Instance.CancelBuildMode();
		CloseSpecialMenus();
		currentSpecialMenu = menu;
		specialHUDMenus[(int)(menu - 1)].rootObject.SetActive(value: true);
		specialHUDMenus[(int)(menu - 1)].SpecialHUDAnimator.SetBool("Opened", value: true);
		if (StartOfRound.Instance.localPlayerUsingController)
		{
			EventSystem.current.SetSelectedGameObject(specialHUDMenus[(int)(menu - 1)].defaultSelectedGameObject);
		}
	}

	public void CloseSpecialMenus()
	{
		currentSpecialMenu = SpecialHUDMenu.None;
		for (int i = 0; i < specialHUDMenus.Length; i++)
		{
			specialHUDMenus[i].SpecialHUDAnimator.SetBool("Opened", value: false);
			specialHUDMenus[i].rootObject.SetActive(value: false);
		}
		EventSystem.current.SetSelectedGameObject(null);
	}

	private void OnEnable()
	{
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("EnableChat").performed += EnableChat_performed;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("OpenMenu").performed += OpenMenu_performed;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("SubmitChat").performed += SubmitChat_performed;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("PingScan").performed += PingScan_performed;
		InputSystem.onDeviceChange += OnDeviceChange;
		playerActions.Movement.Enable();
	}

	private void OnDisable()
	{
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("EnableChat").performed -= EnableChat_performed;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("OpenMenu").performed -= OpenMenu_performed;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("SubmitChat").performed -= SubmitChat_performed;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("PingScan").performed -= PingScan_performed;
		InputSystem.onDeviceChange -= OnDeviceChange;
		playerActions.Movement.Disable();
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			playerActions = new PlayerActions();
			playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
			HUDElements = new HUDElement[7] { Inventory, Chat, PlayerInfo, Tooltips, InstabilityCounter, Clock, Compass };
			scanNodesHit = new RaycastHit[13];
			StartCoroutine(waitUntilLocalPlayerControllerInitialized());
		}
		else
		{
			if (Instance.gameObject != null)
			{
				UnityEngine.Object.Destroy(Instance.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(Instance);
			}
			Instance = this;
		}
	}

	private void Start()
	{
		terminalScript = UnityEngine.Object.FindObjectOfType<Terminal>();
	}

	private void OnDeviceChange(InputDevice device, InputDeviceChange deviceChange)
	{
		bool flag = false;
		switch (deviceChange)
		{
		case InputDeviceChange.Disconnected:
			flag = true;
			deviceChangeText.text = "Controller disconnected";
			break;
		case InputDeviceChange.Reconnected:
			flag = true;
			deviceChangeText.text = "Controller connected";
			break;
		}
		if (flag)
		{
			deviceChangeAnimator.SetTrigger("display");
		}
	}

	public void SetSavedValues(int playerObjectId = -1)
	{
		if (playerObjectId == -1)
		{
			playerObjectId = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
		}
		if (!hasSetSavedValues)
		{
			hasSetSavedValues = true;
			localPlayerLevel = ES3.Load("PlayerLevel", "LCGeneralSaveData", 0);
			localPlayerXP = ES3.Load("PlayerXPNum", "LCGeneralSaveData", 0);
			bool flag = ES3.Load("playedDuringBeta", "LCGeneralSaveData", defaultValue: true);
			StartOfRound.Instance.allPlayerScripts[playerObjectId].playerBetaBadgeMesh.enabled = flag;
			Debug.Log("Has beta?: {hasBeta}");
			Debug.Log(string.Format("Has beta save data: {0}", ES3.Load("playedDuringBeta", "LCGeneralSaveData", defaultValue: true)));
			if (ES3.Load("FinishedShockMinigame", "LCGeneralSaveData", 0) < 2)
			{
				setTutorialArrow = true;
			}
		}
	}

	private IEnumerator waitUntilLocalPlayerControllerInitialized()
	{
		yield return new WaitUntil(() => GameNetworkManager.Instance.localPlayerController != null);
		SetSavedValues();
	}

	public void SetNearDepthOfFieldEnabled(bool enabled)
	{
		float value = ((!enabled) ? 0.2f : 0.5f);
		if (playerGraphicsVolume.sharedProfile.TryGet<DepthOfField>(out var component))
		{
			component.nearFocusEnd.SetValue(new MinFloatParameter(value, 0f, overrideState: true));
		}
	}

	private void SetAdvertisementItemToCorrectPosition()
	{
		if (advertItem != null)
		{
			advertItem.transform.localPosition = Vector3.zero;
		}
	}

	public void ChooseAdItem()
	{
		if (!base.IsServer)
		{
			return;
		}
		for (int num = advertItemParent.transform.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(advertItemParent.transform.GetChild(num).gameObject);
		}
		advertItem = null;
		bool flag = false;
		int num2 = 100;
		int num3 = -1;
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		for (int i = 0; i < terminal.itemSalesPercentages.Length && i < terminal.buyableItemsList.Length; i++)
		{
			Item item = terminal.buyableItemsList[i];
			if (terminal.itemSalesPercentages[i] < num2 && item.itemId != 6 && item.itemId != 7 && item.itemId != 1 && item.itemId != 19)
			{
				num2 = terminal.itemSalesPercentages[i];
				num3 = i;
			}
		}
		Debug.Log($"Steepest sale found : {num2}; index: {num3}");
		string itemName = "";
		string saleText = ChooseSaleText();
		if (num2 <= 70)
		{
			Debug.Log("Putting a tool in the ad");
			Item item = terminal.buyableItemsList[num3];
			CreateToolAdModelAndDisplayAdClientRpc(num2, num3);
			CreateToolAdModel(num2, item);
			saleText = $"{100 - num2}% OFF!";
			itemName = item.itemName;
			flag = true;
		}
		else
		{
			Debug.Log("Putting furniture in the ad");
			List<TerminalNode> list = new List<TerminalNode>();
			for (int j = 0; j < terminal.ShipDecorSelection.Count; j++)
			{
				if (!StartOfRound.Instance.unlockablesList.unlockables[terminal.ShipDecorSelection[j].shipUnlockableID].hasBeenUnlockedByPlayer)
				{
					list.Add(terminal.ShipDecorSelection[j]);
				}
			}
			if (list.Count > 0)
			{
				int num4 = UnityEngine.Random.Range(0, list.Count);
				int num5 = -1;
				for (int k = 0; k < terminal.ShipDecorSelection.Count; k++)
				{
					if (terminal.ShipDecorSelection[k].shipUnlockableID == list[num4].shipUnlockableID)
					{
						num5 = k;
						Debug.Log($"Randomly chose item index : {num5} / {num4}");
						break;
					}
				}
				CreateFurnitureAdModelAndDisplayAdClientRpc(num5);
				CreateFurnitureAdModel(StartOfRound.Instance.unlockablesList.unlockables[list[num4].shipUnlockableID]);
				itemName = StartOfRound.Instance.unlockablesList.unlockables[list[num4].shipUnlockableID].unlockableName;
				flag = true;
			}
			else
			{
				Debug.Log("All furniture already purchased. Not displaying ad");
			}
		}
		if (flag)
		{
			BeginDisplayAd(itemName, saleText);
		}
	}

	private string ChooseSaleText()
	{
		string result = "AVAILABLE NOW!";
		int num = new System.Random(StartOfRound.Instance.randomMapSeed).Next(0, 100);
		if (num < 3)
		{
			result = "CURES CANCER!";
		}
		else if (num < 6)
		{
			result = "NO WAY!";
		}
		else if (num < 30)
		{
			result = "LIMITED TIME ONLY!";
		}
		else if (num < 60)
		{
			result = "GET YOURS TODAY!";
		}
		return result;
	}

	[ClientRpc]
	public void CreateFurnitureAdModelAndDisplayAdClientRpc(int indexInShipDecorList)
{if(!base.IsServer)		{
			for (int num = advertItemParent.transform.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(advertItemParent.transform.GetChild(num).gameObject);
			}
			advertItem = null;
			if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				UnlockableItem unlockable = StartOfRound.Instance.unlockablesList.unlockables[terminalScript.ShipDecorSelection[indexInShipDecorList].shipUnlockableID];
				CreateFurnitureAdModel(unlockable);
			}
			BeginDisplayAd(StartOfRound.Instance.unlockablesList.unlockables[terminalScript.ShipDecorSelection[indexInShipDecorList].shipUnlockableID].unlockableName, ChooseSaleText());
		}
}
	[ClientRpc]
	public void CreateToolAdModelAndDisplayAdClientRpc(int steepestSale, int itemIndex)
{if(!base.IsServer)		{
			for (int num = advertItemParent.transform.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(advertItemParent.transform.GetChild(num).gameObject);
			}
			advertItem = null;
			Item item = terminalScript.buyableItemsList[itemIndex];
			if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				CreateToolAdModel(steepestSale, item);
			}
			BeginDisplayAd(item.itemName, $"{steepestSale}% OFF!");
		}
}
	public void CreateFurnitureAdModel(UnlockableItem unlockable)
	{
		Debug.Log("Unlockable name: " + unlockable.unlockableName);
		if (unlockable.unlockableType == 0)
		{
			advertItem = UnityEngine.Object.Instantiate(emptySuitPrefab, advertItemParent, worldPositionStays: false);
			advertItem.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = unlockable.suitMaterial;
			advertItem.transform.localPosition = Vector3.zero;
			advertItem.transform.localScale = advertItem.transform.localScale * 58f;
			foreach (Transform item in advertItem.transform)
			{
				if (item.CompareTag("Gravel"))
				{
					GameObject obj = UnityEngine.Object.Instantiate(unlockable.headCostumeObject, item.transform.position, item.transform.rotation);
					obj.transform.parent = item;
					obj.transform.localScale = Vector3.one;
					obj.GetComponentInChildren<Renderer>().gameObject.layer = 5;
				}
				else if (item.CompareTag("Puddle"))
				{
					GameObject obj2 = UnityEngine.Object.Instantiate(unlockable.lowerTorsoCostumeObject, item.transform.position, item.transform.rotation);
					obj2.transform.parent = item;
					obj2.transform.localScale = Vector3.one;
					obj2.GetComponentInChildren<Renderer>().gameObject.layer = 5;
				}
			}
		}
		else
		{
			advertItem = UnityEngine.Object.Instantiate(unlockable.prefabObject, advertItemParent, worldPositionStays: false);
			advertItem.transform.localPosition = Vector3.zero;
			advertItem.transform.localScale = advertItem.transform.localScale * 58f;
			UnityEngine.Object.Destroy(advertItem.GetComponent<AutoParentToShip>());
			UnityEngine.Object.Destroy(advertItem.GetComponent<NetworkObject>());
			UnityEngine.Object.Destroy(advertItem.GetComponentInChildren<PlaceableShipObject>());
			Renderer[] componentsInChildren = advertItem.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].gameObject.layer == 9 || componentsInChildren[i].gameObject.layer == 26)
				{
					UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
				}
				else
				{
					componentsInChildren[i].gameObject.layer = 5;
				}
			}
		}
		advertItem.SetActive(value: true);
	}

	public void CreateToolAdModel(int salePercentage, Item item)
	{
		advertItem = UnityEngine.Object.Instantiate(item.spawnPrefab, advertItemParent);
		UnityEngine.Object.Destroy(advertItem.GetComponent<NetworkObject>());
		UnityEngine.Object.Destroy(advertItem.GetComponent<GrabbableObject>());
		UnityEngine.Object.Destroy(advertItem.GetComponent<Collider>());
		Collider[] componentsInChildren = advertItem.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		advertItem.transform.localPosition = Vector3.zero;
		advertItem.transform.localScale = advertItem.transform.localScale * 155f;
		advertItem.transform.rotation = Quaternion.Euler(item.restingRotation);
		Renderer[] componentsInChildren2 = advertItem.GetComponentsInChildren<Renderer>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if (componentsInChildren2[j].gameObject.layer == 22)
			{
				UnityEngine.Object.Destroy(componentsInChildren2[j].gameObject);
			}
			else if (componentsInChildren2[j].gameObject.layer == 14)
			{
				UnityEngine.Object.Destroy(componentsInChildren2[j].gameObject);
			}
			else
			{
				componentsInChildren2[j].gameObject.layer = 5;
			}
		}
		advertItem.SetActive(value: true);
	}

	public void BeginDisplayAd(string itemName, string saleText)
	{
		advertTopText.text = itemName;
		advertBottomText.text = saleText;
		if (displayAdCoroutine != null)
		{
			StopCoroutine(displayAdCoroutine);
		}
		displayAdCoroutine = StartCoroutine(displayAd());
	}

	private IEnumerator displayAd()
	{
		if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			advertAnimator.SetTrigger("PopUpAd");
		}
		if (new System.Random(StartOfRound.Instance.randomMapSeed).Next(0, 10) < 7)
		{
			UIAudio.PlayOneShot(advertMusic, 0.6f);
		}
		else
		{
			UIAudio.PlayOneShot(advertMusic2, 0.6f);
		}
		yield return new WaitForSeconds(14f);
		for (int num = advertItemParent.transform.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(advertItemParent.transform.GetChild(num).gameObject);
		}
	}

	public void UpdateHealthUI(int health, bool hurtPlayer = true)
	{
		if (health < 100)
		{
			selfRedCanvasGroup.alpha = (float)(100 - health) / 100f;
			if (health >= 20 && playerIsCriticallyInjured)
			{
				playerIsCriticallyInjured = false;
				HUDAnimator.SetTrigger("HealFromCritical");
			}
		}
		else
		{
			selfRedCanvasGroup.alpha = 0f;
		}
		if (hurtPlayer && health > 0)
		{
			if (health < 20)
			{
				playerIsCriticallyInjured = true;
				HUDAnimator.SetTrigger("CriticalHit");
				UIAudio.PlayOneShot(criticalInjury, 1f);
			}
			else
			{
				HUDAnimator.SetTrigger("SmallHit");
			}
		}
	}

	private void AddChatMessage(string chatMessage, string nameOfUserWhoTyped = "", int playerWhoSent = -1, bool dontRepeat = false)
	{
		if ((!dontRepeat || !(lastChatMessage == chatMessage)) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerWhoSent)
		{
			lastChatMessage = chatMessage;
			PingHUDElement(Chat, 4f);
			if (ChatMessageHistory.Count >= 4)
			{
				chatText.text.Remove(0, ChatMessageHistory[0].Length);
				ChatMessageHistory.Remove(ChatMessageHistory[0]);
			}
			StringBuilder stringBuilder = new StringBuilder(chatMessage);
			stringBuilder.Replace("[playerNum0]", StartOfRound.Instance.allPlayerScripts[0].playerUsername);
			stringBuilder.Replace("[playerNum1]", StartOfRound.Instance.allPlayerScripts[1].playerUsername);
			stringBuilder.Replace("[playerNum2]", StartOfRound.Instance.allPlayerScripts[2].playerUsername);
			stringBuilder.Replace("[playerNum3]", StartOfRound.Instance.allPlayerScripts[3].playerUsername);
			chatMessage = stringBuilder.ToString();
			string item = ((!string.IsNullOrEmpty(nameOfUserWhoTyped)) ? ("<color=#FF0000>" + nameOfUserWhoTyped + "</color>: <color=#FFFF00>'" + chatMessage + "'</color>") : ("<color=#7069ff>" + chatMessage + "</color>"));
			ChatMessageHistory.Add(item);
			chatText.text = "";
			for (int i = 0; i < ChatMessageHistory.Count; i++)
			{
				TextMeshProUGUI textMeshProUGUI = chatText;
				textMeshProUGUI.text = textMeshProUGUI.text + "\n" + ChatMessageHistory[i];
			}
		}
	}

	public void AddTextToChatOnServer(string chatMessage, int playerId = -1)
	{
		if (playerId != -1)
		{
			AddChatMessage(chatMessage, playersManager.allPlayerScripts[playerId].playerUsername);
			AddPlayerChatMessageServerRpc(chatMessage, playerId);
		}
		else
		{
			AddTextMessageServerRpc(chatMessage);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void AddPlayerChatMessageServerRpc(string chatMessage, int playerId)
{if(chatMessage.Length <= 50)		{
			AddPlayerChatMessageClientRpc(chatMessage, playerId);
		}
}
	[ClientRpc]
	private void AddPlayerChatMessageClientRpc(string chatMessage, int playerId)
{if(playersManager.allPlayerScripts[playerId].isPlayerDead == GameNetworkManager.Instance.localPlayerController.isPlayerDead)		{
			bool flag = GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie && StartOfRound.Instance.allPlayerScripts[playerId].holdingWalkieTalkie;
			if (!(Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, playersManager.allPlayerScripts[playerId].transform.position) > 25f) || flag)
			{
				AddChatMessage(chatMessage, playersManager.allPlayerScripts[playerId].playerUsername, playerId);
			}
		}
}
	[ServerRpc(RequireOwnership = false)]
	private void AddTextMessageServerRpc(string chatMessage)
{		{
			AddTextMessageClientRpc(chatMessage);
		}
}
	[ClientRpc]
	private void AddTextMessageClientRpc(string chatMessage)
{		{
			AddChatMessage(chatMessage, "", -1, dontRepeat: true);
		}
}
	private void SubmitChat_performed(InputAction.CallbackContext context)
	{
		localPlayer = GameNetworkManager.Instance.localPlayerController;
		if (!context.performed || localPlayer == null || !localPlayer.isTypingChat || ((!localPlayer.IsOwner || (base.IsServer && !localPlayer.isHostPlayerObject)) && !localPlayer.isTestingPlayer) || localPlayer.isPlayerDead)
		{
			return;
		}
		if (!string.IsNullOrEmpty(chatTextField.text) && chatTextField.text.Length < 50)
		{
			AddTextToChatOnServer(chatTextField.text, (int)localPlayer.playerClientId);
		}
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) > 24.4f && (!GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie || !StartOfRound.Instance.allPlayerScripts[i].holdingWalkieTalkie))
			{
				playerCouldRecieveTextChatAnimator.SetTrigger("ping");
				break;
			}
		}
		localPlayer.isTypingChat = false;
		chatTextField.text = "";
		EventSystem.current.SetSelectedGameObject(null);
		PingHUDElement(Chat);
		typingIndicator.enabled = false;
	}

	private void EnableChat_performed(InputAction.CallbackContext context)
	{
		localPlayer = GameNetworkManager.Instance.localPlayerController;
		if (context.performed && !(localPlayer == null) && ((localPlayer.IsOwner && (!base.IsServer || localPlayer.isHostPlayerObject)) || localPlayer.isTestingPlayer) && !localPlayer.isPlayerDead && !localPlayer.inTerminalMenu)
		{
			ShipBuildModeManager.Instance.CancelBuildMode();
			localPlayer.isTypingChat = true;
			chatTextField.Select();
			PingHUDElement(Chat, 0.1f, 1f, 1f);
			typingIndicator.enabled = true;
		}
	}

	private void OpenMenu_performed(InputAction.CallbackContext context)
	{
		localPlayer = GameNetworkManager.Instance.localPlayerController;
		if (!(localPlayer == null) && localPlayer.isTypingChat && context.performed && ((localPlayer.IsOwner && (!base.IsServer || localPlayer.isHostPlayerObject)) || localPlayer.isTestingPlayer))
		{
			localPlayer.isTypingChat = false;
			EventSystem.current.SetSelectedGameObject(null);
			chatTextField.text = "";
			PingHUDElement(Chat, 1f);
			typingIndicator.enabled = false;
		}
	}

	private void PingScan_performed(InputAction.CallbackContext context)
	{
		if (!(GameNetworkManager.Instance.localPlayerController == null) && context.performed && CanPlayerScan() && playerPingingScan <= -1f)
		{
			playerPingingScan = 0.3f;
			scanEffectAnimator.transform.position = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position;
			scanEffectAnimator.SetTrigger("scan");
			PingHUDElement(Compass, 1f, 0.8f, 0.12f);
			UIAudio.PlayOneShot(scanSFX);
		}
	}

	private bool CanPlayerScan()
	{
		if (!GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation)
		{
			return !GameNetworkManager.Instance.localPlayerController.isPlayerDead;
		}
		return false;
	}

	public void UpdateBoxesSpectateUI()
	{
		PlayerControllerB playerScript;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			playerScript = StartOfRound.Instance.allPlayerScripts[i];
			if (!playerScript.isPlayerDead)
			{
				if (playerScript.isPlayerControlled || !spectatingPlayerBoxes.Values.Contains(playerScript))
				{
					continue;
				}
				Debug.Log("Removing player spectate box since they disconnected");
				Animator key = spectatingPlayerBoxes.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == playerScript).Key;
				if (key.gameObject.activeSelf)
				{
					for (int j = 0; j < spectatingPlayerBoxes.Count; j++)
					{
						RectTransform component = spectatingPlayerBoxes.ElementAt(j).Key.gameObject.GetComponent<RectTransform>();
						if (component.anchoredPosition.y <= -70f * (float)boxesAdded + 1f)
						{
							component.anchoredPosition = new Vector2(component.anchoredPosition.x, component.anchoredPosition.y + 70f);
						}
					}
					yOffsetAmount += 70f;
				}
				spectatingPlayerBoxes.Remove(key);
				UnityEngine.Object.Destroy(key.gameObject);
			}
			else if (spectatingPlayerBoxes.Values.Contains(playerScript))
			{
				GameObject gameObject = spectatingPlayerBoxes.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == playerScript).Key.gameObject;
				if (!gameObject.activeSelf)
				{
					RectTransform component2 = gameObject.GetComponent<RectTransform>();
					component2.anchoredPosition = new Vector2(component2.anchoredPosition.x, yOffsetAmount);
					boxesAdded++;
					gameObject.SetActive(value: true);
					yOffsetAmount -= 70f;
				}
			}
			else
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(spectatingPlayerBoxPrefab, SpectateBoxesContainer, worldPositionStays: false);
				gameObject.SetActive(value: true);
				RectTransform component3 = gameObject.GetComponent<RectTransform>();
				component3.anchoredPosition = new Vector2(component3.anchoredPosition.x, yOffsetAmount);
				yOffsetAmount -= 70f;
				boxesAdded++;
				spectatingPlayerBoxes.Add(gameObject.GetComponent<Animator>(), playerScript);
				gameObject.GetComponentInChildren<TextMeshProUGUI>().text = playerScript.playerUsername;
				if (!GameNetworkManager.Instance.disableSteam)
				{
					FillImageWithSteamProfile(gameObject.GetComponent<RawImage>(), playerScript.playerSteamId);
				}
			}
		}
	}

	public static async void FillImageWithSteamProfile(RawImage image, SteamId steamId, bool large = true)
	{
		if (SteamClient.IsValid)
		{
			if (large)
			{
				image.texture = GetTextureFromImage(await SteamFriends.GetLargeAvatarAsync(steamId));
			}
			else
			{
				image.texture = GetTextureFromImage(await SteamFriends.GetSmallAvatarAsync(steamId));
			}
		}
	}

	public static Texture2D GetTextureFromImage(Steamworks.Data.Image? image)
	{
		Texture2D texture2D = new Texture2D((int)image.Value.Width, (int)image.Value.Height);
		Debug.Log("Slot K");
		for (int i = 0; i < image.Value.Width; i++)
		{
			for (int j = 0; j < image.Value.Height; j++)
			{
				Steamworks.Data.Color pixel = image.Value.GetPixel(i, j);
				texture2D.SetPixel(i, (int)image.Value.Height - j, new UnityEngine.Color((float)(int)pixel.r / 255f, (float)(int)pixel.g / 255f, (float)(int)pixel.b / 255f, (float)(int)pixel.a / 255f));
			}
		}
		Debug.Log("Slot L");
		texture2D.Apply();
		return texture2D;
	}

	public void RemoveSpectateUI()
	{
		for (int i = 0; i < spectatingPlayerBoxes.Count; i++)
		{
			spectatingPlayerBoxes.ElementAt(i).Key.gameObject.SetActive(value: false);
			boxesAdded--;
		}
		yOffsetAmount = 0f;
		hasGottenPlayerSteamProfilePictures = false;
		hasLoadedSpectateUI = false;
	}

	private void UpdateSpectateBoxSpeakerIcons()
	{
		if (StartOfRound.Instance.voiceChatModule == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < spectatingPlayerBoxes.Count; i++)
		{
			PlayerControllerB value = spectatingPlayerBoxes.ElementAt(i).Value;
			if (!value.isPlayerControlled && !value.isPlayerDead)
			{
				continue;
			}
			if (value == GameNetworkManager.Instance.localPlayerController)
			{
				if (!string.IsNullOrEmpty(StartOfRound.Instance.voiceChatModule.LocalPlayerName))
				{
					VoicePlayerState voicePlayerState = StartOfRound.Instance.voiceChatModule.FindPlayer(StartOfRound.Instance.voiceChatModule.LocalPlayerName);
					if (voicePlayerState != null)
					{
						spectatingPlayerBoxes.ElementAt(i).Key.SetBool("speaking", voicePlayerState.IsSpeaking && voicePlayerState.Amplitude > 0.005f);
					}
				}
			}
			else if (value.voicePlayerState == null)
			{
				if (!flag)
				{
					flag = true;
					StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
				}
			}
			else
			{
				VoicePlayerState voicePlayerState = value.voicePlayerState;
				spectatingPlayerBoxes.ElementAt(i).Key.SetBool("speaking", voicePlayerState.IsSpeaking && voicePlayerState.Amplitude > 0.005f && !voicePlayerState.IsLocallyMuted);
			}
		}
	}

	public void SetSpectatingTextToPlayer(PlayerControllerB playerScript)
	{
		if (playerScript == null)
		{
			spectatingPlayerText.text = "";
		}
		else
		{
			spectatingPlayerText.text = "(Spectating: " + playerScript.playerUsername + ")";
		}
	}

	private void DisplayScrapItemsOnHud()
	{
		if (boxesDisplaying < ScrapItemBoxes.Length && itemsToBeDisplayed.Count > 0)
		{
			DisplayNewScrapFound();
		}
		if (boxesDisplaying <= 0 || !(ScrapItemBoxes[bottomBoxIndex].UIContainer.anchoredPosition.y < (float)bottomBoxYPosition))
		{
			return;
		}
		for (int i = 0; i < ScrapItemBoxes.Length; i++)
		{
			ScrapItemBoxes[i].UIContainer.anchoredPosition += Vector2.up * (Time.deltaTime * 325f);
		}
		if (ScrapItemBoxes[bottomBoxIndex].UIContainer.anchoredPosition.y > (float)bottomBoxYPosition)
		{
			float num = ScrapItemBoxes[bottomBoxIndex].UIContainer.anchoredPosition.y - (float)bottomBoxYPosition;
			num -= 0.01f;
			for (int j = 0; j < ScrapItemBoxes.Length; j++)
			{
				ScrapItemBoxes[j].UIContainer.anchoredPosition -= Vector2.up * num;
			}
		}
	}

	private void SetScreenFilters()
	{
		UnderwaterScreenFilters();
		drunknessFilter.weight = Mathf.Lerp(drunknessFilter.weight, StartOfRound.Instance.drunknessSideEffect.Evaluate(GameNetworkManager.Instance.localPlayerController.drunkness), 5f * Time.deltaTime);
		gasImageAlpha.alpha = drunknessFilter.weight * 1.5f;
		if (StartOfRound.Instance.fearLevel > 0.4f)
		{
			insanityScreenFilter.weight = Mathf.Lerp(insanityScreenFilter.weight, StartOfRound.Instance.fearLevel, 5f * Time.deltaTime);
		}
		else
		{
			insanityScreenFilter.weight = Mathf.Lerp(insanityScreenFilter.weight, 0f, 2f * Time.deltaTime);
		}
		sinkingUnderAnimator.SetBool("cover", sinkingCoveredFace);
		if (flashFilter > 0f)
		{
			flashFilter -= Time.deltaTime * 0.16f;
		}
		flashbangScreenFilter.weight = Mathf.Min(1f, flashFilter);
		HelmetCondensationDrops();
	}

	private void HelmetCondensationDrops()
	{
		if (!increaseHelmetCondensation)
		{
			if (helmetCondensationMaterial.color.a > 0f)
			{
				UnityEngine.Color color = helmetCondensationMaterial.color;
				color.a = Mathf.Clamp(color.a - Time.deltaTime / 2f, 0f, 0.27f);
				helmetCondensationMaterial.color = color;
			}
		}
		else
		{
			if (helmetCondensationMaterial.color.a < 1f)
			{
				UnityEngine.Color color2 = helmetCondensationMaterial.color;
				color2.a = Mathf.Clamp(color2.a + Time.deltaTime / 2f, 0f, 0.27f);
				helmetCondensationMaterial.color = color2;
			}
			increaseHelmetCondensation = false;
		}
		if ((TimeOfDay.Instance.effects[1].effectEnabled || TimeOfDay.Instance.effects[2].effectEnabled) && !TimeOfDay.Instance.insideLighting && Vector3.Angle(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, Vector3.up) < 45f)
		{
			increaseHelmetCondensation = true;
		}
	}

	private void UnderwaterScreenFilters()
	{
		bool flag = false;
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
		{
			PlayerControllerB spectatedPlayerScript = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
			if (spectatedPlayerScript.underwaterCollider != null && spectatedPlayerScript.underwaterCollider.bounds.Contains(StartOfRound.Instance.spectateCamera.transform.position))
			{
				flag = true;
			}
		}
		if (setUnderwaterFilter || flag)
		{
			audioListenerLowPass.enabled = true;
			audioListenerLowPass.cutoffFrequency = Mathf.Lerp(audioListenerLowPass.cutoffFrequency, 700f, 10f * Time.deltaTime);
			underwaterScreenFilter.weight = 1f;
			breathingUnderwaterAudio.volume = Mathf.Lerp(breathingUnderwaterAudio.volume, 1f, 10f * Time.deltaTime);
			if (!flag && !breathingUnderwaterAudio.isPlaying)
			{
				breathingUnderwaterAudio.Play();
			}
			return;
		}
		if (audioListenerLowPass.cutoffFrequency >= 19000f)
		{
			audioListenerLowPass.enabled = false;
		}
		else
		{
			audioListenerLowPass.cutoffFrequency = Mathf.Lerp(audioListenerLowPass.cutoffFrequency, 20000f, 10f * Time.deltaTime);
		}
		if (underwaterScreenFilter.weight < 0.05f)
		{
			underwaterScreenFilter.weight = 0f;
			breathingUnderwaterAudio.Stop();
		}
		else
		{
			breathingUnderwaterAudio.volume = Mathf.Lerp(breathingUnderwaterAudio.volume, 0f, 10f * Time.deltaTime);
			underwaterScreenFilter.weight = Mathf.Lerp(underwaterScreenFilter.weight, 0f, 10f * Time.deltaTime);
		}
	}

	private void Update()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		SetAdvertisementItemToCorrectPosition();
		DisplayScrapItemsOnHud();
		SetScreenFilters();
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			if (!hasLoadedSpectateUI)
			{
				hasLoadedSpectateUI = true;
				Debug.Log("Adding boxes");
				UpdateBoxesSpectateUI();
			}
			if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.currentLevel.planetHasTime)
			{
				holdButtonToEndGameEarlyHoldTime = 0f;
				holdButtonToEndGameEarlyMeter.gameObject.SetActive(value: false);
				holdButtonToEndGameEarlyText.text = "";
				holdButtonToEndGameEarlyVotesText.text = "";
			}
			else if (!TimeOfDay.Instance.shipLeavingAlertCalled)
			{
				holdButtonToEndGameEarlyText.enabled = true;
				if (!TimeOfDay.Instance.votedShipToLeaveEarlyThisRound)
				{
					DisplaySpectatorVoteTip();
					if (StartOfRound.Instance.localPlayerUsingController)
					{
						holdButtonToEndGameEarlyText.text = "Tell autopilot ship to leave early : [R-trigger] (Hold)";
					}
					else
					{
						holdButtonToEndGameEarlyText.text = "Tell autopilot ship to leave early : [RMB] (Hold)";
					}
					if (playerActions.Movement.PingScan.IsPressed())
					{
						holdButtonToEndGameEarlyHoldTime += Time.deltaTime;
						holdButtonToEndGameEarlyMeter.gameObject.SetActive(value: true);
						if (holdButtonToEndGameEarlyHoldTime > 3f)
						{
							TimeOfDay.Instance.VoteShipToLeaveEarly();
							holdButtonToEndGameEarlyText.text = "Voted for ship to leave early";
						}
					}
					else
					{
						holdButtonToEndGameEarlyHoldTime = 0f;
						holdButtonToEndGameEarlyMeter.gameObject.SetActive(value: false);
					}
					holdButtonToEndGameEarlyMeter.fillAmount = holdButtonToEndGameEarlyHoldTime / 3f;
				}
				else
				{
					holdButtonToEndGameEarlyText.text = "Voted for ship to leave early";
					holdButtonToEndGameEarlyMeter.gameObject.SetActive(value: false);
				}
				int num = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
				holdButtonToEndGameEarlyText.enabled = true;
				holdButtonToEndGameEarlyVotesText.text = $"({TimeOfDay.Instance.votesForShipToLeaveEarly}/{num} Votes)";
			}
			else
			{
				holdButtonToEndGameEarlyText.text = "Ship leaving in one hour";
				if (TimeOfDay.Instance.votesForShipToLeaveEarly <= 0)
				{
					holdButtonToEndGameEarlyVotesText.text = "";
				}
				holdButtonToEndGameEarlyMeter.gameObject.SetActive(value: false);
			}
			if (updateSpectateBoxesInterval >= 0.35f)
			{
				updateSpectateBoxesInterval = 0f;
				UpdateSpectateBoxSpeakerIcons();
			}
			else
			{
				updateSpectateBoxesInterval += Time.deltaTime;
			}
		}
		else
		{
			float num2 = Mathf.RoundToInt(Mathf.Clamp(GameNetworkManager.Instance.localPlayerController.carryWeight - 1f, 0f, 100f) * 105f);
			weightCounter.text = $"{num2} lb";
			weightCounterAnimator.SetFloat("weight", num2 / 130f);
		}
		if (CanPlayerScan())
		{
			UpdateScanNodes(GameNetworkManager.Instance.localPlayerController);
			scanElementsHidden = false;
			if (scannedScrapNum >= 2 && totalScrapScannedDisplayNum < totalScrapScanned)
			{
				addingToDisplayTotal = true;
				if (addToDisplayTotalInterval <= 0.03f)
				{
					addToDisplayTotalInterval += Time.deltaTime;
				}
				else
				{
					addToDisplayTotalInterval = 0f;
					totalScrapScannedDisplayNum = (int)Mathf.Clamp(Mathf.MoveTowards(totalScrapScannedDisplayNum, totalScrapScanned, 1500f * Time.deltaTime), 20f, 10000f);
					totalValueText.text = $"${totalScrapScannedDisplayNum}";
					UIAudio.PlayOneShot(addToScrapTotalSFX);
				}
			}
			else if (addingToDisplayTotal)
			{
				addingToDisplayTotal = false;
				UIAudio.PlayOneShot(finishAddingToTotalSFX);
			}
		}
		else if (!scanElementsHidden)
		{
			scanElementsHidden = true;
			DisableAllScanElements();
		}
		if (playerPingingScan >= -1f)
		{
			playerPingingScan -= Time.deltaTime;
		}
		for (int i = 0; i < HUDElements.Length; i++)
		{
			HUDElements[i].canvasGroup.alpha = Mathf.Lerp(HUDElements[i].canvasGroup.alpha, HUDElements[i].targetAlpha, 10f * Time.deltaTime);
		}
		compassImage.uvRect = new Rect(GameNetworkManager.Instance.localPlayerController.transform.eulerAngles.y / 360f + compassOffset, 0f, 1f, 1f);
		if (holdFillAmount > 0f)
		{
			holdInteractionCanvasGroup.alpha = Mathf.Lerp(holdInteractionCanvasGroup.alpha, 1f, 20f * Time.deltaTime);
		}
		else
		{
			holdInteractionCanvasGroup.alpha = Mathf.Lerp(holdInteractionCanvasGroup.alpha, 0f, 20f * Time.deltaTime);
		}
		if (tutorialArrowState == 0 || !setTutorialArrow)
		{
			shockTutorialLeftAlpha.alpha = Mathf.Lerp(shockTutorialLeftAlpha.alpha, 0f, 17f * Time.deltaTime);
			shockTutorialRightAlpha.alpha = Mathf.Lerp(shockTutorialRightAlpha.alpha, 0f, 17f * Time.deltaTime);
		}
		else if (tutorialArrowState == 1)
		{
			shockTutorialLeftAlpha.alpha = Mathf.Lerp(shockTutorialLeftAlpha.alpha, 1f, 17f * Time.deltaTime);
			shockTutorialRightAlpha.alpha = Mathf.Lerp(shockTutorialRightAlpha.alpha, 0f, 17f * Time.deltaTime);
		}
		else
		{
			shockTutorialRightAlpha.alpha = Mathf.Lerp(shockTutorialRightAlpha.alpha, 1f, 17f * Time.deltaTime);
			shockTutorialLeftAlpha.alpha = Mathf.Lerp(shockTutorialLeftAlpha.alpha, 0f, 17f * Time.deltaTime);
		}
	}

	public void SetShipLeaveEarlyVotesText(int votes)
	{
		int num = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
		holdButtonToEndGameEarlyVotesText.text = $"({votes}/{num} Votes)";
	}

	private void UpdateScanNodes(PlayerControllerB playerScript)
	{
		Vector3 zero = Vector3.zero;
		if (updateScanInterval <= 0f)
		{
			updateScanInterval = 0.25f;
			AssignNewNodes(playerScript);
		}
		updateScanInterval -= Time.deltaTime;
		bool flag = false;
		for (int i = 0; i < scanElements.Length; i++)
		{
			if (scanNodes.Count > 0 && scanNodes.TryGetValue(scanElements[i], out var value) && value != null)
			{
				try
				{
					if (NodeIsNotVisible(value, i))
					{
						continue;
					}
					if (!scanElements[i].gameObject.activeSelf)
					{
						scanElements[i].gameObject.SetActive(value: true);
						scanElements[i].GetComponent<Animator>().SetInteger("colorNumber", value.nodeType);
						if (value.creatureScanID != -1)
						{
							AttemptScanNewCreature(value.creatureScanID);
						}
					}
					goto IL_00f7;
				}
				catch (Exception arg)
				{
					Debug.LogError($"Error in updatescanNodes A: {arg}");
					goto IL_00f7;
				}
			}
			scanNodes.Remove(scanElements[i]);
			scanElements[i].gameObject.SetActive(value: false);
			continue;
			IL_00f7:
			try
			{
				scanElementText = scanElements[i].gameObject.GetComponentsInChildren<TextMeshProUGUI>();
				if (scanElementText.Length > 1)
				{
					scanElementText[0].text = value.headerText;
					scanElementText[1].text = value.subText;
				}
				if (value.nodeType == 2)
				{
					flag = true;
				}
				zero = playerScript.gameplayCamera.WorldToScreenPoint(value.transform.position);
				scanElements[i].anchoredPosition = new Vector2(zero.x - 439.48f, zero.y - 244.8f);
			}
			catch (Exception arg2)
			{
				Debug.LogError($"Error in updatescannodes B: {arg2}");
			}
		}
		try
		{
			if (!flag)
			{
				totalScrapScanned = 0;
				totalScrapScannedDisplayNum = 0;
				addToDisplayTotalInterval = 0.35f;
			}
			scanInfoAnimator.SetBool("display", scannedScrapNum >= 2 && flag);
		}
		catch (Exception arg3)
		{
			Debug.LogError($"Error in updatescannodes C: {arg3}");
		}
	}

	private void AssignNewNodes(PlayerControllerB playerScript)
	{
		int num = Physics.SphereCastNonAlloc(new Ray(playerScript.gameplayCamera.transform.position + playerScript.gameplayCamera.transform.forward * 20f, playerScript.gameplayCamera.transform.forward), 20f, scanNodesHit, 80f, 4194304);
		if (num > scanElements.Length)
		{
			num = scanElements.Length;
		}
		nodesOnScreen.Clear();
		scannedScrapNum = 0;
		if (num > scanElements.Length)
		{
			for (int i = 0; i < num; i++)
			{
				ScanNodeProperties component = scanNodesHit[i].transform.gameObject.GetComponent<ScanNodeProperties>();
				if (component.nodeType == 1 || component.nodeType == 2)
				{
					AttemptScanNode(component, i, playerScript);
				}
			}
		}
		if (nodesOnScreen.Count < scanElements.Length)
		{
			for (int j = 0; j < num; j++)
			{
				ScanNodeProperties component = scanNodesHit[j].transform.gameObject.GetComponent<ScanNodeProperties>();
				AttemptScanNode(component, j, playerScript);
			}
		}
	}

	private void AttemptScanNode(ScanNodeProperties node, int i, PlayerControllerB playerScript)
	{
		if (MeetsScanNodeRequirements(node, playerScript))
		{
			if (node.nodeType == 2)
			{
				scannedScrapNum++;
			}
			if (!nodesOnScreen.Contains(node))
			{
				nodesOnScreen.Add(node);
			}
			if (playerPingingScan >= 0f)
			{
				AssignNodeToUIElement(node);
			}
		}
	}

	private bool MeetsScanNodeRequirements(ScanNodeProperties node, PlayerControllerB playerScript)
	{
		if (node == null)
		{
			return false;
		}
		float num = Vector3.Distance(playerScript.transform.position, node.transform.position);
		if (num < (float)node.maxRange && num > (float)node.minRange)
		{
			if (node.requiresLineOfSight)
			{
				return !Physics.Linecast(playerScript.gameplayCamera.transform.position, node.transform.position, 134217984, QueryTriggerInteraction.Ignore);
			}
			return true;
		}
		return false;
	}

	private bool NodeIsNotVisible(ScanNodeProperties node, int elementIndex)
	{
		if (!nodesOnScreen.Contains(node))
		{
			if (scanNodes[scanElements[elementIndex]].nodeType == 2)
			{
				totalScrapScanned = Mathf.Clamp(totalScrapScanned - scanNodes[scanElements[elementIndex]].scrapValue, 0, 100000);
			}
			scanElements[elementIndex].gameObject.SetActive(value: false);
			scanNodes.Remove(scanElements[elementIndex]);
			return true;
		}
		return false;
	}

	private void AssignNodeToUIElement(ScanNodeProperties node)
	{
		if (scanNodes.ContainsValue(node))
		{
			return;
		}
		for (int i = 0; i < scanElements.Length; i++)
		{
			if (scanNodes.TryAdd(scanElements[i], node))
			{
				if (node.nodeType == 2)
				{
					totalScrapScanned += node.scrapValue;
					addedToScrapCounterThisFrame = true;
				}
				break;
			}
		}
	}

	private void DisableAllScanElements()
	{
		for (int i = 0; i < scanElements.Length; i++)
		{
			scanElements[i].gameObject.SetActive(value: false);
			totalScrapScanned = 0;
			totalScrapScannedDisplayNum = 0;
		}
	}

	private void AttemptScanNewCreature(int enemyID)
	{
		if (!terminalScript.scannedEnemyIDs.Contains(enemyID))
		{
			ScanNewCreatureServerRpc(enemyID);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ScanNewCreatureServerRpc(int enemyID)
{if(!terminalScript.scannedEnemyIDs.Contains(enemyID))			{
				terminalScript.scannedEnemyIDs.Add(enemyID);
				terminalScript.newlyScannedEnemyIDs.Add(enemyID);
				DisplayGlobalNotification("New creature data sent to terminal!");
				ScanNewCreatureClientRpc(enemyID);
			}
}
	[ClientRpc]
	public void ScanNewCreatureClientRpc(int enemyID)
{if(!terminalScript.scannedEnemyIDs.Contains(enemyID))			{
				terminalScript.scannedEnemyIDs.Add(enemyID);
				terminalScript.newlyScannedEnemyIDs.Add(enemyID);
				DisplayGlobalNotification("New creature data sent to terminal!");
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void GetNewStoryLogServerRpc(int logID)
{if(!terminalScript.unlockedStoryLogs.Contains(logID))			{
				terminalScript.unlockedStoryLogs.Add(logID);
				terminalScript.newlyUnlockedStoryLogs.Add(logID);
				DisplayGlobalNotification("Found journal entry: '" + terminalScript.logEntryFiles[logID].creatureName);
				GetNewStoryLogClientRpc(logID);
			}
}
	[ClientRpc]
	public void GetNewStoryLogClientRpc(int logID)
{if(!terminalScript.unlockedStoryLogs.Contains(logID))			{
				terminalScript.unlockedStoryLogs.Add(logID);
				terminalScript.newlyUnlockedStoryLogs.Add(logID);
				DisplayGlobalNotification("Found journal entry: '" + terminalScript.logEntryFiles[logID].creatureName + "'");
			}
}
	private void DisplayGlobalNotification(string displayText)
	{
		globalNotificationAnimator.SetTrigger("TriggerNotif");
		globalNotificationText.text = displayText;
		UIAudio.PlayOneShot(globalNotificationSFX);
	}

	public void PingHUDElement(HUDElement element, float delay = 2f, float startAlpha = 1f, float endAlpha = 0.2f)
	{
		if (delay == 0f && startAlpha == endAlpha)
		{
			element.targetAlpha = endAlpha;
			return;
		}
		element.targetAlpha = startAlpha;
		if (element.fadeCoroutine != null)
		{
			StopCoroutine(element.fadeCoroutine);
		}
		element.fadeCoroutine = StartCoroutine(FadeUIElement(element, delay, endAlpha));
	}

	private IEnumerator FadeUIElement(HUDElement element, float delay, float endAlpha)
	{
		yield return new WaitForSeconds(delay);
		element.targetAlpha = endAlpha;
	}

	public void HideHUD(bool hide)
	{
		if (hudHidden != hide)
		{
			hudHidden = hide;
			if (hide)
			{
				HUDAnimator.SetTrigger("hideHud");
				scanInfoAnimator.SetBool("display", value: false);
			}
			else
			{
				HUDAnimator.SetTrigger("revealHud");
			}
		}
	}

	public void ReadDialogue(DialogueSegment[] dialogueArray)
	{
		if (readDialogueCoroutine != null)
		{
			StopCoroutine(readDialogueCoroutine);
		}
		readDialogueCoroutine = StartCoroutine(ReadOutDialogue(dialogueArray));
	}

	private IEnumerator ReadOutDialogue(DialogueSegment[] dialogueArray)
	{
		dialogueBoxAnimator.SetBool("Open", value: true);
		for (int i = 0; i < dialogueArray.Length; i++)
		{
			dialogeBoxHeaderText.text = dialogueArray[i].speakerText;
			dialogeBoxText.text = dialogueArray[i].bodyText;
			dialogueBoxSFX.PlayOneShot(dialogueBleeps[UnityEngine.Random.Range(0, dialogueBleeps.Length)]);
			yield return new WaitForSeconds(dialogueArray[i].waitTime);
		}
		dialogueBoxAnimator.SetBool("Open", value: false);
	}

	public void DisplayCreditsEarning(int creditsEarned, GrabbableObject[] objectsSold, int newGroupCredits)
	{
		Debug.Log($"Earned {creditsEarned}; sold {objectsSold.Length} items; new credits amount: {newGroupCredits}");
		List<Item> list = new List<Item>();
		for (int i = 0; i < objectsSold.Length; i++)
		{
			list.Add(objectsSold[i].itemProperties);
		}
		Item[] array = list.Distinct().ToArray();
		string text = "";
		int num = 0;
		int num2 = 0;
		for (int j = 0; j < array.Length; j++)
		{
			num = 0;
			num2 = 0;
			for (int k = 0; k < objectsSold.Length; k++)
			{
				if (objectsSold[k].itemProperties == array[j])
				{
					num += objectsSold[k].scrapValue;
					num2++;
				}
			}
			text += $"{array[j].itemName} (x{num2}) : {num} \n";
		}
		moneyRewardsListText.text = text;
		moneyRewardsTotalText.text = $"TOTAL: ${creditsEarned}";
		moneyRewardsAnimator.SetTrigger("showRewards");
		rewardsScrollbar.value = 1f;
		if (list.Count > 8)
		{
			if (scrollRewardTextCoroutine != null)
			{
				StopCoroutine(scrollRewardTextCoroutine);
			}
			scrollRewardTextCoroutine = StartCoroutine(scrollRewardsListText());
		}
	}

	private IEnumerator scrollRewardsListText()
	{
		yield return new WaitForSeconds(0.3f);
		float num = 3f;
		while (num >= 0f)
		{
			num -= Time.deltaTime;
			rewardsScrollbar.value -= Time.deltaTime / num;
		}
	}

	public void DisplayNewDeadline(int overtimeBonus)
	{
		reachedProfitQuotaAnimator.SetBool("display", value: true);
		newProfitQuotaText.text = "$0";
		UIAudio.PlayOneShot(reachedQuotaSFX);
		displayingNewQuota = true;
		if (overtimeBonus < 0)
		{
			reachedProfitQuotaBonusText.text = "";
		}
		else
		{
			reachedProfitQuotaBonusText.text = $"Overtime bonus: ${overtimeBonus}";
		}
		StartCoroutine(rackUpNewQuotaText());
	}

	private IEnumerator rackUpNewQuotaText()
	{
		yield return new WaitForSeconds(3.5f);
		int quotaTextAmount = 0;
		while (quotaTextAmount < TimeOfDay.Instance.profitQuota)
		{
			quotaTextAmount = (int)Mathf.Clamp((float)quotaTextAmount + Time.deltaTime * 250f, quotaTextAmount + 3, TimeOfDay.Instance.profitQuota + 10);
			newProfitQuotaText.text = "$" + quotaTextAmount;
			yield return null;
		}
		newProfitQuotaText.text = "$" + TimeOfDay.Instance.profitQuota;
		TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
		UIAudio.PlayOneShot(newProfitQuotaSFX);
		yield return new WaitForSeconds(1.25f);
		displayingNewQuota = false;
		reachedProfitQuotaAnimator.SetBool("display", value: false);
	}

	public void DisplayDaysLeft(int daysLeft)
	{
		if (daysLeft >= 0)
		{
			string text = ((daysLeft != 1) ? $"{daysLeft} Days Left" : $"{daysLeft} Day Left");
			profitQuotaDaysLeftText.text = text;
			profitQuotaDaysLeftText2.text = text;
			if (daysLeft <= 1)
			{
				reachedProfitQuotaAnimator.SetTrigger("displayDaysLeft");
				UIAudio.PlayOneShot(OneDayToMeetQuotaSFX);
			}
			else
			{
				reachedProfitQuotaAnimator.SetTrigger("displayDaysLeftCalm");
				UIAudio.PlayOneShot(profitQuotaDaysLeftCalmSFX);
			}
		}
	}

	public void ShowPlayersFiredScreen(bool show)
	{
		playersFiredAnimator.SetBool("gameOver", show);
	}

	public void ShakeCamera(ScreenShakeType shakeType)
	{
		switch (shakeType)
		{
		case ScreenShakeType.Big:
			playerScreenShakeAnimator.SetTrigger("bigShake");
			break;
		case ScreenShakeType.Small:
			playerScreenShakeAnimator.SetTrigger("smallShake");
			break;
		case ScreenShakeType.Long:
			playerScreenShakeAnimator.SetTrigger("longShake");
			break;
		case ScreenShakeType.VeryStrong:
			playerScreenShakeAnimator.SetTrigger("veryStrongShake");
			break;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void UseSignalTranslatorServerRpc(string signalMessage)
{if((bool)UnityEngine.Object.FindObjectOfType<SignalTranslator>() && !string.IsNullOrEmpty(signalMessage) && signalMessage.Length <= 12)		{
			SignalTranslator signalTranslator = UnityEngine.Object.FindObjectOfType<SignalTranslator>();
			if (!(Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator < 8f))
			{
				signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
				signalTranslator.timesSendingMessage++;
				UseSignalTranslatorClientRpc(signalMessage, signalTranslator.timesSendingMessage);
			}
		}
}
	[ClientRpc]
	public void UseSignalTranslatorClientRpc(string signalMessage, int timesSendingMessage)
{if(!string.IsNullOrEmpty(signalMessage) && (bool)UnityEngine.Object.FindObjectOfType<SignalTranslator>())		{
			SignalTranslator signalTranslator = UnityEngine.Object.FindObjectOfType<SignalTranslator>();
			signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
			if (signalTranslator.signalTranslatorCoroutine != null)
			{
				StopCoroutine(signalTranslator.signalTranslatorCoroutine);
			}
			string signalMessage2 = signalMessage.Substring(0, Mathf.Min(signalMessage.Length, 10));
			signalTranslator.timesSendingMessage = timesSendingMessage;
			signalTranslator.signalTranslatorCoroutine = StartCoroutine(DisplaySignalTranslatorMessage(signalMessage2, timesSendingMessage, signalTranslator));
		}
}
	private IEnumerator DisplaySignalTranslatorMessage(string signalMessage, int seed, SignalTranslator signalTranslator)
	{
		System.Random signalMessageRandom = new System.Random(seed + StartOfRound.Instance.randomMapSeed);
		signalTranslatorAnimator.SetBool("transmitting", value: true);
		signalTranslator.localAudio.Play();
		UIAudio.PlayOneShot(signalTranslator.startTransmissionSFX, 1f);
		signalTranslatorText.text = "";
		yield return new WaitForSeconds(1.21f);
		for (int i = 0; i < signalMessage.Length; i++)
		{
			if (signalTranslator == null)
			{
				break;
			}
			if (!signalTranslator.gameObject.activeSelf)
			{
				break;
			}
			UIAudio.PlayOneShot(signalTranslator.typeTextClips[UnityEngine.Random.Range(0, signalTranslator.typeTextClips.Length)]);
			signalTranslatorText.text += signalMessage[i];
			float num = Mathf.Min((float)signalMessageRandom.Next(-1, 4) * 0.5f, 0f);
			yield return new WaitForSeconds(0.7f + num);
		}
		if (signalTranslator != null)
		{
			UIAudio.PlayOneShot(signalTranslator.finishTypingSFX);
			signalTranslator.localAudio.Stop();
		}
		yield return new WaitForSeconds(0.5f);
		signalTranslatorAnimator.SetBool("transmitting", value: false);
	}

	public void ToggleHUD(bool enable)
	{
		HUDContainer.SetActive(enable);
	}

	public void FillChallengeResultsStats(int scrapCollected)
	{
		statsUIElements.challengeCollectedText.text = $"${scrapCollected} Collected";
		if (GameNetworkManager.Instance.disableSteam)
		{
			statsUIElements.challengeRankText.text = "---";
			return;
		}
		Debug.Log($"Scrap collected B: {scrapCollected}");
		GetRankAndSubmitScore(scrapCollected);
	}

	public async void GetRankAndSubmitScore(int scrapCollected)
	{
		Debug.Log("GetRankAndSubmitScore called");
		if (!StartOfRound.Instance.isChallengeFile)
		{
			return;
		}
		Debug.Log("GetRankAndSubmitScore called A");
		try
		{
			retrievingSteamLeaderboard = true;
			int weekNum = GameNetworkManager.Instance.GetWeekNumber();
			Leaderboard? leaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync($"challenge{weekNum}", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
			Debug.Log($"Found or created leaderboard 'challenge{weekNum}'");
			Debug.Log("Did not submit score yet...");
			LeaderboardUpdate? leaderboardUpdate;
			if (StartOfRound.Instance.allPlayersDead)
			{
				Debug.Log("All players dead");
				leaderboardUpdate = await leaderboard.Value.ReplaceScore(0, new int[1] { 3 });
				Debug.Log("Replaced score! A");
			}
			else
			{
				leaderboardUpdate = await leaderboard.Value.ReplaceScore(scrapCollected);
				Debug.Log($"Replaced score! B: scrapCollected: {scrapCollected}");
			}
			ES3.Save("SubmittedScore", value: true, "LCChallengeFile");
			if (leaderboardUpdate.HasValue && leaderboardUpdate.HasValue)
			{
				ES3.Save("SubmittedScore", value: true, "LCChallengeFile");
				statsUIElements.challengeRankText.text = $"#{leaderboardUpdate.Value.NewGlobalRank}";
			}
			else
			{
				Debug.Log($"Updated leaderboard returned null, unable to replace score?; {!leaderboardUpdate.HasValue}");
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while submitting leaderboard score: {arg}");
		}
		retrievingSteamLeaderboard = false;
	}

	public void FillEndGameStats(EndOfGameStats stats, int scrapCollected = 0)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < playersManager.allPlayerScripts.Length; i++)
		{
			PlayerControllerB playerControllerB = playersManager.allPlayerScripts[i];
			statsUIElements.playerNamesText[i].text = "";
			statsUIElements.playerStates[i].enabled = false;
			statsUIElements.playerNotesText[i].text = "Notes: \n";
			if (playerControllerB.disconnectedMidGame || playerControllerB.isPlayerDead || playerControllerB.isPlayerControlled)
			{
				if (playerControllerB.isPlayerDead)
				{
					num++;
				}
				else if (playerControllerB.isPlayerControlled)
				{
					num2++;
				}
				statsUIElements.playerNamesText[i].text = playersManager.allPlayerScripts[i].playerUsername;
				statsUIElements.playerStates[i].enabled = true;
				if (playersManager.allPlayerScripts[i].isPlayerDead)
				{
					if (playersManager.allPlayerScripts[i].causeOfDeath == CauseOfDeath.Abandoned)
					{
						statsUIElements.playerStates[i].sprite = statsUIElements.missingIcon;
					}
					else
					{
						statsUIElements.playerStates[i].sprite = statsUIElements.deceasedIcon;
					}
				}
				else
				{
					statsUIElements.playerStates[i].sprite = statsUIElements.aliveIcon;
				}
				for (int j = 0; j < 3 && j < stats.allPlayerStats[i].playerNotes.Count; j++)
				{
					TextMeshProUGUI obj = statsUIElements.playerNotesText[i];
					obj.text = obj.text + "* " + stats.allPlayerStats[i].playerNotes[j] + "\n";
				}
			}
			else
			{
				statsUIElements.playerNotesText[i].text = "";
			}
		}
		statsUIElements.quotaNumerator.text = scrapCollected.ToString();
		statsUIElements.quotaDenominator.text = RoundManager.Instance.totalScrapValueInLevel.ToString();
		if (StartOfRound.Instance.allPlayersDead)
		{
			statsUIElements.allPlayersDeadOverlay.enabled = true;
			statsUIElements.gradeLetter.text = "F";
			return;
		}
		statsUIElements.allPlayersDeadOverlay.enabled = false;
		int num3 = 0;
		float num4 = (float)RoundManager.Instance.scrapCollectedInLevel / RoundManager.Instance.totalScrapValueInLevel;
		if (num2 == StartOfRound.Instance.connectedPlayersAmount + 1)
		{
			num3++;
		}
		else if (num > 1)
		{
			num3--;
		}
		if (num4 >= 0.99f)
		{
			num3 += 2;
		}
		else if (num4 >= 0.6f)
		{
			num3++;
		}
		else if (num4 <= 0.25f)
		{
			num3--;
		}
		switch (num3)
		{
		case -1:
			statsUIElements.gradeLetter.text = "D";
			break;
		case 0:
			statsUIElements.gradeLetter.text = "C";
			break;
		case 1:
			statsUIElements.gradeLetter.text = "B";
			break;
		case 2:
			statsUIElements.gradeLetter.text = "A";
			break;
		case 3:
			statsUIElements.gradeLetter.text = "S";
			break;
		}
	}

	[ServerRpc]
	public void SyncAllPlayerLevelsServerRpc()
{		int[] array = new int[4];
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (!StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled)
			{
				array[i] = -1;
			}
			else
			{
				array[i] = StartOfRound.Instance.allPlayerScripts[i].playerLevelNumber;
			}
		}
		bool[] array2 = new bool[4];
		for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
		{
			if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
			{
				array2[j] = false;
			}
			else
			{
				array2[j] = StartOfRound.Instance.allPlayerScripts[j].playerBetaBadgeMesh.enabled;
			}
		}
		SyncAllPlayerLevelsClientRpc(array, array2);
}
	[ClientRpc]
	public void SyncAllPlayerLevelsClientRpc(int[] playerLevelNumbers, bool[] playersHaveBeta)
{		try
		{
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				SetLevelOfPlayer(StartOfRound.Instance.allPlayerScripts[i], playerLevelNumbers[i], playersHaveBeta[i]);
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while syncing player level from server: {arg}");
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void SyncPlayerLevelServerRpc(int playerId, int playerLevelIndex, bool hasBeta = false)
			{
				SyncPlayerLevelClientRpc(playerId, playerLevelIndex, hasBeta);
			}

	[ServerRpc(RequireOwnership = false)]
	public void SyncAllPlayerLevelsServerRpc(int newPlayerLevel, int playerClientId)
{		List<int> list = new List<int>();
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled || StartOfRound.Instance.allPlayerScripts[i].isPlayerDead)
			{
				if (i == playerClientId)
				{
					list.Add(newPlayerLevel);
				}
				else
				{
					list.Add(StartOfRound.Instance.allPlayerScripts[i].playerLevelNumber);
				}
			}
		}
		SyncAllPlayerLevelsClientRpc(list.ToArray(), StartOfRound.Instance.connectedPlayersAmount);
}
	[ClientRpc]
	public void SyncAllPlayerLevelsClientRpc(int[] allPlayerLevels, int connectedPlayers)
{if(StartOfRound.Instance.connectedPlayersAmount != connectedPlayers)		{
			return;
		}
		int num = 0;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled || StartOfRound.Instance.allPlayerScripts[i].isPlayerDead)
			{
				if (StartOfRound.Instance.allPlayerScripts[i] == GameNetworkManager.Instance.localPlayerController)
				{
					num++;
					continue;
				}
				SetLevelOfPlayer(StartOfRound.Instance.allPlayerScripts[i], allPlayerLevels[num], hasBeta: true);
				num++;
			}
		}
}
	[ClientRpc]
	public void SyncPlayerLevelClientRpc(int playerId, int playerLevelIndex, bool hasBeta)
{		try
		{
			if (!(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)
			{
				if (playerLevelIndex >= playerLevels.Length)
				{
					Debug.LogError("Error: Player level synced in client RPC was above the max player level!");
				}
				else
				{
					SetLevelOfPlayer(StartOfRound.Instance.allPlayerScripts[playerId], playerLevelIndex, hasBeta);
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while syncing player level from client #{playerId}: {arg}");
		}
}
	public void SetLevelOfPlayer(PlayerControllerB playerScript, int playerLevelIndex, bool hasBeta)
	{
		playerScript.playerLevelNumber = playerLevelIndex;
		if (GameNetworkManager.Instance.localPlayerController != null && playerScript == GameNetworkManager.Instance.localPlayerController)
		{
			playerScript.playerBetaBadgeMesh.enabled = false;
			playerScript.playerBadgeMesh.gameObject.GetComponent<MeshRenderer>().enabled = false;
		}
		else
		{
			playerScript.playerBetaBadgeMesh.enabled = hasBeta;
			playerScript.playerBadgeMesh.mesh = playerLevels[playerLevelIndex].badgeMesh;
		}
	}

	public void DisableLocalBadgeMesh()
	{
		if (GameNetworkManager.Instance.localPlayerController != null)
		{
			GameNetworkManager.Instance.localPlayerController.playerBetaBadgeMesh.enabled = false;
			GameNetworkManager.Instance.localPlayerController.playerBadgeMesh.gameObject.GetComponent<MeshRenderer>().enabled = false;
		}
	}

	public void SetPlayerLevel(bool isDead, bool mostProfitable, bool allPlayersDead)
	{
		int num = 0;
		num = ((!isDead) ? (num + 10) : (num - 3));
		if (mostProfitable)
		{
			num += 15;
		}
		if (allPlayersDead)
		{
			num -= 5;
		}
		if (num > 0)
		{
			Debug.Log($"XP gain before scaling to scrap returned: {num}");
			Debug.Log((float)RoundManager.Instance.scrapCollectedInLevel / RoundManager.Instance.totalScrapValueInLevel);
			float num2 = (float)RoundManager.Instance.scrapCollectedInLevel / RoundManager.Instance.totalScrapValueInLevel;
			Debug.Log(num2);
			num = (int)((float)num * num2);
		}
		if (num == 0)
		{
			Debug.Log("Gained no XP");
			playerLevelMeter.fillAmount = localPlayerXP / playerLevels[localPlayerLevel].XPMax;
			playerLevelXPCounter.text = localPlayerXP.ToString();
			playerLevelText.text = playerLevels[localPlayerLevel].levelName;
		}
		else
		{
			StartCoroutine(SetPlayerLevelSmoothly(num));
		}
	}

	private IEnumerator SetPlayerLevelSmoothly(int XPGain)
	{
		float changingPlayerXP = localPlayerXP;
		int changingPlayerLevel = localPlayerLevel;
		int targetXPLevel = Mathf.Max(localPlayerXP + XPGain, 0);
		bool conditionMet = false;
		if (XPGain < 0)
		{
			LevellingAudio.clip = decreaseXPSFX;
		}
		else
		{
			LevellingAudio.clip = increaseXPSFX;
		}
		LevellingAudio.Play();
		float timeAtStart = Time.realtimeSinceStartup;
		while (!conditionMet && Time.realtimeSinceStartup - timeAtStart < 5f)
		{
			Debug.Log($"Level up timer: {Time.realtimeSinceStartup - timeAtStart}");
			if (XPGain < 0)
			{
				changingPlayerXP -= Time.deltaTime * 15f;
				if (changingPlayerXP < 0f)
				{
					changingPlayerXP = 0f;
				}
				if (changingPlayerXP <= (float)targetXPLevel)
				{
					conditionMet = true;
				}
				if (changingPlayerLevel - 1 >= 0 && changingPlayerXP < (float)playerLevels[changingPlayerLevel].XPMin)
				{
					changingPlayerLevel--;
					UIAudio.PlayOneShot(levelDecreaseSFX);
					playerLevelBoxAnimator.SetTrigger("Shake");
					yield return new WaitForSeconds(0.4f);
				}
			}
			else
			{
				changingPlayerXP += Time.deltaTime * 15f;
				if (changingPlayerXP >= (float)targetXPLevel)
				{
					conditionMet = true;
				}
				if (changingPlayerLevel + 1 < playerLevels.Length && changingPlayerXP >= (float)playerLevels[changingPlayerLevel].XPMax)
				{
					changingPlayerLevel++;
					UIAudio.PlayOneShot(levelIncreaseSFX);
					playerLevelBoxAnimator.SetTrigger("Shake");
					yield return new WaitForSeconds(0.4f);
				}
			}
			playerLevelMeter.fillAmount = (changingPlayerXP - (float)playerLevels[changingPlayerLevel].XPMin) / (float)playerLevels[changingPlayerLevel].XPMax;
			playerLevelText.text = playerLevels[changingPlayerLevel].levelName;
			playerLevelXPCounter.text = $"{Mathf.RoundToInt(changingPlayerXP)} EXP";
			yield return null;
		}
		LevellingAudio.Stop();
		int num = 0;
		for (int i = 0; i < playerLevels.Length; i++)
		{
			if (targetXPLevel >= playerLevels[i].XPMin && targetXPLevel < playerLevels[i].XPMax)
			{
				num = i;
				break;
			}
			if (i == playerLevels.Length - 1)
			{
				num = i;
			}
		}
		localPlayerXP = targetXPLevel;
		localPlayerLevel = num;
		playerLevelText.text = playerLevels[localPlayerLevel].levelName;
		playerLevelXPCounter.text = $"{Mathf.RoundToInt(localPlayerXP)} EXP";
		bool hasBeta = ES3.Load("playedDuringBeta", "LCGeneralSaveData", defaultValue: true);
		SyncPlayerLevelServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, localPlayerLevel, hasBeta);
	}

	public void ApplyPenalty(int playersDead, int bodiesInsured)
	{
		float num = 0.2f;
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		int groupCredits = terminal.groupCredits;
		bodiesInsured = Mathf.Max(bodiesInsured, 0);
		for (int i = 0; i < playersDead - bodiesInsured; i++)
		{
			terminal.groupCredits -= (int)((float)groupCredits * num);
		}
		for (int j = 0; j < bodiesInsured; j++)
		{
			terminal.groupCredits -= (int)((float)groupCredits * (num / 2.5f));
		}
		if (terminal.groupCredits < 0)
		{
			terminal.groupCredits = 0;
		}
		statsUIElements.penaltyAddition.text = $"{playersDead} casualties: -{num * 100f * (float)(playersDead - bodiesInsured)}%\n({bodiesInsured} bodies recovered)";
		statsUIElements.penaltyTotal.text = $"DUE: ${groupCredits - terminal.groupCredits}";
		Debug.Log($"New group credits after penalty: {terminal.groupCredits}");
	}

	public void SetQuota(int numerator, int denominator = -1)
	{
		HUDQuotaNumerator.text = numerator.ToString();
		if (denominator != -1)
		{
			HUDQuotaDenominator.text = denominator.ToString();
		}
	}

	public void AddNewScrapFoundToDisplay(GrabbableObject GObject)
	{
		if (itemsToBeDisplayed.Count <= 16)
		{
			itemsToBeDisplayed.Add(GObject);
		}
	}

	public void DisplayNewScrapFound()
	{
		if (itemsToBeDisplayed.Count <= 0)
		{
			return;
		}
		if (itemsToBeDisplayed[0] == null || itemsToBeDisplayed[0].itemProperties.spawnPrefab == null)
		{
			itemsToBeDisplayed.Clear();
			return;
		}
		if (itemsToBeDisplayed[0].scrapValue < 80)
		{
			UIAudio.PlayOneShot(displayCollectedScrapSFXSmall);
		}
		else
		{
			UIAudio.PlayOneShot(displayCollectedScrapSFX);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(itemsToBeDisplayed[0].itemProperties.spawnPrefab, ScrapItemBoxes[nextBoxIndex].itemObjectContainer);
		UnityEngine.Object.Destroy(gameObject.GetComponent<NetworkObject>());
		UnityEngine.Object.Destroy(gameObject.GetComponent<GrabbableObject>());
		UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localScale = gameObject.transform.localScale * 4f;
		gameObject.transform.rotation = Quaternion.Euler(itemsToBeDisplayed[0].itemProperties.restingRotation);
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].gameObject.layer != 22)
			{
				Material[] sharedMaterials = componentsInChildren[i].sharedMaterials;
				componentsInChildren[i].rendererPriority = 70;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					sharedMaterials[j] = hologramMaterial;
				}
				componentsInChildren[i].sharedMaterials = sharedMaterials;
				componentsInChildren[i].gameObject.layer = 5;
			}
		}
		ScrapItemBoxes[nextBoxIndex].itemDisplayAnimator.SetTrigger("collect");
		if (itemsToBeDisplayed[0] is RagdollGrabbableObject)
		{
			RagdollGrabbableObject ragdollGrabbableObject = itemsToBeDisplayed[0] as RagdollGrabbableObject;
			if (ragdollGrabbableObject != null && ragdollGrabbableObject.ragdoll != null && ragdollGrabbableObject.ragdoll.playerScript != null)
			{
				ScrapItemBoxes[nextBoxIndex].headerText.text = ragdollGrabbableObject.ragdoll.playerScript.playerUsername + " collected!";
			}
			else
			{
				ScrapItemBoxes[nextBoxIndex].headerText.text = "Body collected!";
			}
		}
		else
		{
			ScrapItemBoxes[nextBoxIndex].headerText.text = itemsToBeDisplayed[0].itemProperties.itemName + " collected!";
		}
		ScrapItemBoxes[nextBoxIndex].valueText.text = $"Value: ${itemsToBeDisplayed[0].scrapValue}";
		if (boxesDisplaying > 0)
		{
			ScrapItemBoxes[nextBoxIndex].UIContainer.anchoredPosition = new Vector2(ScrapItemBoxes[nextBoxIndex].UIContainer.anchoredPosition.x, ScrapItemBoxes[bottomBoxIndex].UIContainer.anchoredPosition.y - 124f);
		}
		else
		{
			ScrapItemBoxes[nextBoxIndex].UIContainer.anchoredPosition = new Vector2(ScrapItemBoxes[nextBoxIndex].UIContainer.anchoredPosition.x, bottomBoxYPosition);
		}
		bottomBoxIndex = nextBoxIndex;
		StartCoroutine(displayScrapTimer(gameObject));
		playScrapDisplaySFX();
		boxesDisplaying++;
		nextBoxIndex = (nextBoxIndex + 1) % 3;
		itemsToBeDisplayed.RemoveAt(0);
	}

	private IEnumerator playScrapDisplaySFX()
	{
		yield return new WaitForSeconds(0.05f * (float)boxesDisplaying);
	}

	private IEnumerator displayScrapTimer(GameObject displayingObject)
	{
		yield return new WaitForSeconds(3.5f);
		boxesDisplaying--;
		UnityEngine.Object.Destroy(displayingObject);
	}

	public void ChangeControlTip(int toolTipNumber, string changeTo, bool clearAllOther = false)
	{
		if (StartOfRound.Instance.localPlayerUsingController)
		{
			StringBuilder stringBuilder = new StringBuilder(changeTo);
			stringBuilder.Replace("[E]", "[D-pad up]");
			stringBuilder.Replace("[Q]", "[D-pad down]");
			stringBuilder.Replace("[LMB]", "[Y]");
			stringBuilder.Replace("[RMB]", "[R-Trigger]");
			stringBuilder.Replace("[G]", "[B]");
			changeTo = stringBuilder.ToString();
		}
		else
		{
			changeTo = changeTo.Replace("[RMB]", "[LMB]");
		}
		controlTipLines[toolTipNumber].text = changeTo;
		if (clearAllOther)
		{
			for (int i = 0; i < controlTipLines.Length; i++)
			{
				if (i != toolTipNumber)
				{
					controlTipLines[i].text = "";
				}
			}
		}
		if (forceChangeTextCoroutine != null)
		{
			StopCoroutine(forceChangeTextCoroutine);
		}
		forceChangeTextCoroutine = StartCoroutine(ForceChangeText(controlTipLines[toolTipNumber], changeTo));
	}

	private IEnumerator ForceChangeText(TextMeshProUGUI textToChange, string changeTextTo)
	{
		for (int i = 0; i < 5; i++)
		{
			yield return null;
			textToChange.text = changeTextTo;
		}
	}

	public void ClearControlTips()
	{
		for (int i = 0; i < controlTipLines.Length; i++)
		{
			controlTipLines[i].text = "";
		}
	}

	public void ChangeControlTipMultiple(string[] allLines, bool holdingItem = false, Item itemProperties = null)
	{
		if (holdingItem)
		{
			controlTipLines[0].text = "Drop " + itemProperties.itemName + " : [G]";
		}
		if (allLines == null)
		{
			return;
		}
		int num = 0;
		if (holdingItem)
		{
			num = 1;
		}
		for (int i = 0; i < allLines.Length && i + num < controlTipLines.Length; i++)
		{
			string text = allLines[i];
			if (StartOfRound.Instance.localPlayerUsingController)
			{
				StringBuilder stringBuilder = new StringBuilder(text);
				stringBuilder.Replace("[E]", "[D-pad up]");
				stringBuilder.Replace("[Q]", "[D-pad down]");
				stringBuilder.Replace("[LMB]", "[Y]");
				stringBuilder.Replace("[RMB]", "[R-Trigger]");
				stringBuilder.Replace("[G]", "[B]");
				text = stringBuilder.ToString();
			}
			else
			{
				text = text.Replace("[RMB]", "[LMB]");
			}
			controlTipLines[i + num].text = text;
		}
	}

	public void SetDebugText(string setText)
	{
		debugText.text = setText;
		debugText.enabled = true;
	}

	public void DisplayStatusEffect(string statusEffect)
	{
		statusEffectAnimator.SetTrigger("IndicateStatus");
		statusEffectText.text = statusEffect;
	}

	public void DisplayTip(string headerText, string bodyText, bool isWarning = false, bool useSave = false, string prefsKey = "LC_Tip1")
	{
		if (!CanTipDisplay(isWarning, useSave, prefsKey))
		{
			return;
		}
		if (useSave)
		{
			if (tipsPanelCoroutine != null)
			{
				StopCoroutine(tipsPanelCoroutine);
			}
			tipsPanelCoroutine = StartCoroutine(TipsPanelTimer(prefsKey));
		}
		tipsPanelHeader.text = headerText;
		tipsPanelBody.text = bodyText;
		if (isWarning)
		{
			tipsPanelAnimator.SetTrigger("TriggerWarning");
			RoundManager.PlayRandomClip(UIAudio, warningSFX, randomize: false);
		}
		else
		{
			tipsPanelAnimator.SetTrigger("TriggerHint");
			RoundManager.PlayRandomClip(UIAudio, tipsSFX, randomize: false);
		}
	}

	private void DisplaySpectatorVoteTip()
	{
		if (displayedSpectatorAFKTip)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (!StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && StartOfRound.Instance.allPlayerScripts[i].timeSincePlayerMoving < 10f)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			noLivingPlayersAtKeyboardTimer += Time.deltaTime;
			if (noLivingPlayersAtKeyboardTimer > 12f)
			{
				if (StartOfRound.Instance.localPlayerUsingController)
				{
					DisplaySpectatorTip("TIP!: Hold [R-Trigger] to vote for the autopilot ship to leave early.");
				}
				else
				{
					DisplaySpectatorTip("TIP!: Hold [RMB] to vote for the autopilot ship to leave early.");
				}
			}
		}
		else
		{
			noLivingPlayersAtKeyboardTimer = 0f;
		}
	}

	private void DisplaySpectatorTip(string tipText)
	{
		displayedSpectatorAFKTip = true;
		spectatorTipText.text = tipText;
		if (!spectatorTipText.enabled)
		{
			StartCoroutine(displayTipTextTimer());
		}
	}

	private IEnumerator displayTipTextTimer()
	{
		UIAudio.PlayOneShot(tipsSFX[0], 1f);
		spectatorTipText.enabled = true;
		yield return new WaitForSeconds(7f);
		spectatorTipText.enabled = false;
	}

	private bool CanTipDisplay(bool isWarning, bool useSave, string prefsKey)
	{
		if (useSave)
		{
			return !ES3.Load(prefsKey, "LCGeneralSaveData", defaultValue: false);
		}
		if (tipsPanelCoroutine != null)
		{
			if (isWarning && !isDisplayingWarning)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	private IEnumerator TipsPanelTimer(string prefsKey)
	{
		yield return new WaitForSeconds(5f);
		ES3.Save(prefsKey, value: true, "LCGeneralSaveData");
	}

	public string SetClock(float timeNormalized, float numberOfHours, bool createNewLine = true)
	{
		int num = (int)(timeNormalized * (60f * numberOfHours)) + 360;
		int num2 = (int)Mathf.Floor(num / 60);
		if (!createNewLine)
		{
			newLine = " ";
		}
		else
		{
			newLine = "\n";
		}
		amPM = newLine + "AM";
		if (num2 >= 24)
		{
			clockNumber.text = "12:00 " + newLine + " AM";
			return "12:00\nAM";
		}
		if (num2 < 12)
		{
			amPM = newLine + "AM";
		}
		else
		{
			amPM = newLine + "PM";
		}
		if (num2 > 12)
		{
			num2 %= 12;
		}
		int num3 = num % 60;
		string text = $"{num2:00}:{num3:00}".TrimStart('0') + amPM;
		clockNumber.text = text;
		return text;
	}

	public void SetClockIcon(DayMode dayMode)
	{
		clockIcon.sprite = clockIcons[(int)dayMode];
	}

	public void SetClockVisible(bool visible)
	{
		if (visible)
		{
			Clock.targetAlpha = 1f;
		}
		else
		{
			Clock.targetAlpha = 0f;
		}
	}

	public void TriggerAlarmHornEffect()
	{
		if (!(UnityEngine.Object.FindObjectOfType<AlarmButton>() == null))
		{
			AlarmHornServerRpc();
		}
	}

	[ServerRpc]
	public void AlarmHornServerRpc()
{		{
			AlarmButton alarmButton = UnityEngine.Object.FindObjectOfType<AlarmButton>();
			if (!(alarmButton == null) && !(alarmButton.timeSincePushing < 1f))
			{
				alarmButton.timeSincePushing = 0f;
				AlarmHornClientRpc();
			}
		}
}
	[ClientRpc]
	public void AlarmHornClientRpc()
{		{
			AlarmButton alarmButton = UnityEngine.Object.FindObjectOfType<AlarmButton>();
			if (!(alarmButton == null))
			{
				alarmButton.timeSincePushing = 0f;
				alarmHornEffect.SetTrigger("triggerAlarm");
				UIAudio.PlayOneShot(shipAlarmHornSFX, 1f);
			}
		}
}
	public void RadiationWarningHUD()
	{
		radiationGraphicAnimator.SetTrigger("RadiationWarning");
		UIAudio.PlayOneShot(radiationWarningAudio, 1f);
	}

	public void MeteorShowerWarningHUD()
	{
		meteorShowerGraphicAnimator.SetTrigger("MeteorShowerWarning");
		UIAudio.PlayOneShot(meteorShowerWarningAudio, 1f);
		StartCoroutine(weatherPSASoundDeafening());
	}

	private IEnumerator weatherPSASoundDeafening()
	{
		float timer = 0f;
		while (GameNetworkManager.Instance.localPlayerController != null && !GameNetworkManager.Instance.localPlayerController.isPlayerDead && timer < 10.4f)
		{
			timer += Time.deltaTime;
			SoundManager.Instance.SetDiageticMasterVolume(-13f);
			yield return null;
		}
	}

	public void UpdateInstabilityPercentage(int percentage)
	{
		if (previousInstability != percentage)
		{
			UpdateInstabilityClientRpc(percentage);
		}
	}

	[ClientRpc]
	public void UpdateInstabilityClientRpc(int percentage)
			{
				instabilityCounterNumber.text = $"{percentage}%";
				PingHUDElement(InstabilityCounter, 2f, 1f, 0.7f);
			}

	public void SetTutorialArrow(int state)
	{
		tutorialArrowState = state;
	}

	public bool HoldInteractionFill(float timeToHold, float speedMultiplier = 1f)
	{
		if (timeToHold == -1f)
		{
			return false;
		}
		holdFillAmount += Time.deltaTime * speedMultiplier;
		holdInteractionFillAmount.fillAmount = holdFillAmount / timeToHold;
		if (holdFillAmount > timeToHold)
		{
			holdFillAmount = 0f;
			return true;
		}
		return false;
	}

	public void ToggleErrorConsole()
	{
		errorLogPanel.SetActive(!Instance.errorLogPanel.activeSelf);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SendErrorMessageServerRpc(string errorMessage, int sentByPlayerNum)
{if(GameNetworkManager.Instance.SendExceptionsToServer && !(Instance == null))		{
			AddToErrorLog(errorMessage, sentByPlayerNum);
		}
}
	public void AddToErrorLog(string errorMessage, int sentByPlayerNum)
	{
		if (!(errorMessage == previousErrorReceived))
		{
			previousErrorReceived = errorMessage;
			string playerUsername = StartOfRound.Instance.allPlayerScripts[sentByPlayerNum].playerUsername;
			playerUsername = playerUsername.Substring(0, Mathf.Clamp(5, 1, playerUsername.Length));
			Instance.errorLogText.text = (Instance.errorLogText.text + "\n\n" + playerUsername + ": " + errorMessage).Substring(Mathf.Max(0, Instance.errorLogText.text.Length - 1000));
		}
	}
}
