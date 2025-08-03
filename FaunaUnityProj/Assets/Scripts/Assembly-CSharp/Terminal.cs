using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameNetcodeStuff;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class Terminal : NetworkBehaviour
{
	public Canvas terminalUIScreen;

	public PlayerActions playerActions;

	public bool terminalInUse;

	public float timeSinceTerminalInUse;

	private InteractTrigger terminalTrigger;

	public RawImage terminalImage;

	public RectMask2D terminalImageMask;

	public RenderTexture videoTexture;

	public VideoPlayer videoPlayer;

	public TMP_InputField screenText;

	public int textAdded;

	public string currentText;

	public TerminalNode currentNode;

	public TerminalNodesList terminalNodes;

	[Space(3f)]
	public Animator terminalUIAnimator;

	public PlaceableShipObject placeableObject;

	private bool usedTerminalThisSession;

	private bool modifyingText;

	public int playerDefinedAmount;

	private RoundManager roundManager;

	public int groupCredits;

	private int totalCostOfItems;

	public AudioSource terminalAudio;

	public AudioClip[] keyboardClips;

	public AudioClip[] syncedAudios;

	private float timeSinceLastKeyboardPress;

	public bool useCreditsCooldown;

	private Coroutine loadImageCoroutine;

	private bool hasGottenNoun;

	private bool hasGottenVerb;

	[Space(7f)]
	private bool broadcastedCodeThisFrame;

	public Animator codeBroadcastAnimator;

	public AudioClip codeBroadcastSFX;

	[Space(5f)]
	public List<int> scannedEnemyIDs = new List<int>();

	public List<TerminalNode> enemyFiles = new List<TerminalNode>();

	public List<int> newlyScannedEnemyIDs = new List<int>();

	[Space(3f)]
	public List<int> unlockedStoryLogs = new List<int>();

	public List<TerminalNode> logEntryFiles = new List<TerminalNode>();

	public List<int> newlyUnlockedStoryLogs = new List<int>();

	[Space(7f)]
	public List<TerminalNode> ShipDecorSelection = new List<TerminalNode>();

	private bool syncedTerminalValues;

	public int numberOfItemsInDropship;

	public bool vehicleInDropship;

	public Scrollbar scrollBarVertical;

	public TextMeshProUGUI inputFieldText;

	public CanvasGroup scrollBarCanvasGroup;

	public RenderTexture playerScreenTex;

	public RenderTexture playerScreenTexHighRes;

	public TextMeshProUGUI topRightText;

	public SelectableLevel[] moonsCatalogueList;

	[Header("Store-bought player items")]
	public Item[] buyableItemsList;

	public BuyableVehicle[] buyableVehicles;

	public int[] itemSalesPercentages;

	[Space(3f)]
	public List<int> orderedItemsFromTerminal;

	public int orderedVehicleFromTerminal = -1;

	[Space(5f)]
	private Coroutine selectTextFieldCoroutine;

	public AudioClip enterTerminalSFX;

	public AudioClip leaveTerminalSFX;

	public Light terminalLight;

	private Coroutine forceScrollbarCoroutine;

	public bool displayingSteamKeyboard;

	public Texture displayingPersistentImage;

	public int startingCreditsAmount;

	public bool hasWarrantyTicket;

	private void Update()
	{
		if (HUDManager.Instance == null || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		if (terminalInUse)
		{
			topRightText.text = $"${groupCredits}";
			screenText.caretPosition = screenText.text.Length;
			HUDManager.Instance.playerScreenTexture.texture = playerScreenTexHighRes;
			GameNetworkManager.Instance.localPlayerController.gameplayCamera.targetTexture = playerScreenTexHighRes;
			if (Keyboard.current.anyKey.wasPressedThisFrame)
			{
				if (timeSinceLastKeyboardPress > 0.07f)
				{
					timeSinceLastKeyboardPress = 0f;
					RoundManager.PlayRandomClip(terminalAudio, keyboardClips);
				}
				if (scrollBarVertical.value != 0f)
				{
					scrollBarVertical.value = 0f;
					if (forceScrollbarCoroutine != null)
					{
						StopCoroutine(forceScrollbarCoroutine);
					}
					forceScrollbarCoroutine = StartCoroutine(forceScrollbarDown());
				}
			}
			timeSinceLastKeyboardPress += Time.deltaTime;
			if (scrollBarVertical.value < 0.95f)
			{
				scrollBarCanvasGroup.alpha = Mathf.Lerp(scrollBarCanvasGroup.alpha, 1f, 10f * Time.deltaTime);
			}
			else
			{
				scrollBarCanvasGroup.alpha = 0f;
			}
		}
		else
		{
			timeSinceTerminalInUse += Time.deltaTime;
			HUDManager.Instance.playerScreenTexture.texture = playerScreenTex;
			GameNetworkManager.Instance.localPlayerController.gameplayCamera.targetTexture = playerScreenTex;
		}
	}

	private IEnumerator forceScrollbarDown()
	{
		for (int i = 0; i < 5; i++)
		{
			yield return null;
			scrollBarVertical.value = 0f;
		}
	}

	private IEnumerator forceScrollbarUp()
	{
		for (int i = 0; i < 5; i++)
		{
			yield return null;
			scrollBarVertical.value = 1f;
		}
	}

	public void LoadNewNode(TerminalNode node)
	{
		modifyingText = true;
		RunTerminalEvents(node);
		screenText.interactable = true;
		string text = "";
		if (node.clearPreviousText)
		{
			text = "\n\n\n" + node.displayText.ToString();
		}
		else
		{
			text = "\n\n" + screenText.text.ToString() + "\n\n" + node.displayText.ToString();
			int value = text.Length - 250;
			text = text.Substring(Mathf.Clamp(value, 0, text.Length)).ToString();
		}
		try
		{
			text = TextPostProcess(text, node);
		}
		catch (Exception arg)
		{
			Debug.LogError($"An error occured while post processing terminal text: {arg}");
		}
		screenText.text = text;
		currentText = screenText.text;
		textAdded = 0;
		if (node.playSyncedClip != -1)
		{
			PlayTerminalAudioServerRpc(node.playSyncedClip);
		}
		else if (node.playClip != null)
		{
			terminalAudio.PlayOneShot(node.playClip);
		}
		LoadTerminalImage(node);
		currentNode = node;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayTerminalAudioServerRpc(int clipIndex)
			{
				PlayTerminalAudioClientRpc(clipIndex);
			}

	[ClientRpc]
	public void PlayTerminalAudioClientRpc(int clipIndex)
{if(!(GameNetworkManager.Instance.localPlayerController == null))			{
				terminalAudio.PlayOneShot(syncedAudios[clipIndex]);
			}
}
	private IEnumerator loadTextAnimation()
	{
		screenText.textComponent.maxVisibleLines = 0;
		for (int i = 0; i < 30; i++)
		{
			screenText.textComponent.maxVisibleLines += 2;
			yield return null;
		}
		screenText.textComponent.maxVisibleLines = 100;
	}

	private string TextPostProcess(string modifiedDisplayText, TerminalNode node)
	{
		int num = modifiedDisplayText.Split("[planetTime]").Length - 1;
		if (num > 0)
		{
			Regex regex = new Regex(Regex.Escape("[planetTime]"));
			for (int i = 0; i < num && moonsCatalogueList.Length > i; i++)
			{
				Debug.Log($"isDemo:{GameNetworkManager.Instance.isDemo} ; {moonsCatalogueList[i].lockedForDemo}");
				string replacement = ((GameNetworkManager.Instance.isDemo && moonsCatalogueList[i].lockedForDemo) ? "(Locked)" : ((moonsCatalogueList[i].currentWeather != LevelWeatherType.None) ? ("(" + moonsCatalogueList[i].currentWeather.ToString() + ")") : ""));
				modifiedDisplayText = regex.Replace(modifiedDisplayText, replacement, 1);
			}
		}
		try
		{
			if (node.displayPlanetInfo != -1)
			{
				string replacement = ((StartOfRound.Instance.levels[node.displayPlanetInfo].currentWeather != LevelWeatherType.None) ? (StartOfRound.Instance.levels[node.displayPlanetInfo].currentWeather.ToString().ToLower() ?? "") : "mild weather");
				modifiedDisplayText = modifiedDisplayText.Replace("[currentPlanetTime]", replacement);
			}
		}
		catch
		{
			Debug.Log($"Exception occured on terminal while setting node planet info; current node displayPlanetInfo:{node.displayPlanetInfo}");
		}
		if (modifiedDisplayText.Contains("[warranty]"))
		{
			modifiedDisplayText = ((!hasWarrantyTicket) ? modifiedDisplayText.Replace("[warranty]", "") : modifiedDisplayText.Replace("[warranty]", "You have a free warranty!"));
		}
		if (modifiedDisplayText.Contains("[currentScannedEnemiesList]"))
		{
			if (scannedEnemyIDs == null || scannedEnemyIDs.Count <= 0)
			{
				modifiedDisplayText = modifiedDisplayText.Replace("[currentScannedEnemiesList]", "No data collected on wildlife. Scans are required.");
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int j = 0; j < scannedEnemyIDs.Count; j++)
				{
					Debug.Log($"scanID # {j}: {scannedEnemyIDs[j]}; {enemyFiles[scannedEnemyIDs[j]].creatureName}");
					Debug.Log($"scanID # {j}: {scannedEnemyIDs[j]}");
					stringBuilder.Append("\n" + enemyFiles[scannedEnemyIDs[j]].creatureName);
					if (newlyScannedEnemyIDs.Contains(scannedEnemyIDs[j]))
					{
						stringBuilder.Append(" (NEW)");
					}
				}
				modifiedDisplayText = modifiedDisplayText.Replace("[currentScannedEnemiesList]", stringBuilder.ToString());
			}
		}
		if (modifiedDisplayText.Contains("[buyableItemsList]"))
		{
			if (buyableItemsList == null || buyableItemsList.Length == 0)
			{
				modifiedDisplayText = modifiedDisplayText.Replace("[buyableItemsList]", "[No items in stock!]");
			}
			else
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				for (int k = 0; k < buyableItemsList.Length; k++)
				{
					stringBuilder2.Append("\n* " + buyableItemsList[k].itemName + "  //  Price: $" + (float)buyableItemsList[k].creditsWorth * ((float)itemSalesPercentages[k] / 100f));
					if (itemSalesPercentages[k] != 100)
					{
						stringBuilder2.Append($"   ({100 - itemSalesPercentages[k]}% OFF!)");
					}
				}
				modifiedDisplayText = modifiedDisplayText.Replace("[buyableItemsList]", stringBuilder2.ToString());
			}
		}
		if (modifiedDisplayText.Contains("[buyableVehiclesList]"))
		{
			if (buyableVehicles == null || buyableVehicles.Length == 0)
			{
				modifiedDisplayText = modifiedDisplayText.Replace("[buyableVehiclesList]", "[No items in stock!]");
			}
			else
			{
				StringBuilder stringBuilder3 = new StringBuilder();
				for (int l = 0; l < buyableVehicles.Length; l++)
				{
					stringBuilder3.Append("\n* " + buyableVehicles[l].vehicleDisplayName + "  //  Price: $" + (float)buyableVehicles[l].creditsWorth * ((float)itemSalesPercentages[l + buyableItemsList.Length] / 100f));
					if (itemSalesPercentages[l + buyableItemsList.Length] != 100)
					{
						stringBuilder3.Append($"   ({100 - itemSalesPercentages[l + buyableItemsList.Length]}% OFF!)");
					}
				}
				modifiedDisplayText = modifiedDisplayText.Replace("[buyableVehiclesList]", stringBuilder3.ToString());
			}
		}
		if (modifiedDisplayText.Contains("[currentUnlockedLogsList]"))
		{
			if (unlockedStoryLogs == null || unlockedStoryLogs.Count <= 0)
			{
				modifiedDisplayText = modifiedDisplayText.Replace("[currentUnlockedLogsList]", "[ALL DATA HAS BEEN CORRUPTED OR OVERWRITTEN]");
			}
			else
			{
				StringBuilder stringBuilder4 = new StringBuilder();
				for (int m = 0; m < unlockedStoryLogs.Count; m++)
				{
					stringBuilder4.Append("\n" + logEntryFiles[unlockedStoryLogs[m]].creatureName);
					if (newlyUnlockedStoryLogs.Contains(unlockedStoryLogs[m]))
					{
						stringBuilder4.Append(" (NEW)");
					}
				}
				modifiedDisplayText = modifiedDisplayText.Replace("[currentUnlockedLogsList]", stringBuilder4.ToString());
			}
		}
		if (modifiedDisplayText.Contains("[unlockablesSelectionList]"))
		{
			if (ShipDecorSelection == null || ShipDecorSelection.Count <= 0)
			{
				modifiedDisplayText = modifiedDisplayText.Replace("[unlockablesSelectionList]", "[No items available]");
			}
			else
			{
				StringBuilder stringBuilder5 = new StringBuilder();
				for (int n = 0; n < ShipDecorSelection.Count; n++)
				{
					stringBuilder5.Append($"\n{ShipDecorSelection[n].creatureName}  //  ${ShipDecorSelection[n].itemCost}");
				}
				modifiedDisplayText = modifiedDisplayText.Replace("[unlockablesSelectionList]", stringBuilder5.ToString());
			}
		}
		if (modifiedDisplayText.Contains("[storedUnlockablesList]"))
		{
			StringBuilder stringBuilder6 = new StringBuilder();
			bool flag = false;
			for (int num2 = 0; num2 < StartOfRound.Instance.unlockablesList.unlockables.Count; num2++)
			{
				if (StartOfRound.Instance.unlockablesList.unlockables[num2].inStorage)
				{
					flag = true;
					stringBuilder6.Append("\n" + StartOfRound.Instance.unlockablesList.unlockables[num2].unlockableName);
				}
			}
			modifiedDisplayText = (flag ? modifiedDisplayText.Replace("[storedUnlockablesList]", stringBuilder6.ToString()) : modifiedDisplayText.Replace("[storedUnlockablesList]", "[No items stored. While moving an object with B, press X to store it.]"));
		}
		if (modifiedDisplayText.Contains("[scanForItems]"))
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 91);
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
			for (int num6 = 0; num6 < array.Length; num6++)
			{
				if (array[num6].itemProperties.isScrap && !array[num6].isInShipRoom && !array[num6].isInElevator)
				{
					num5 += array[num6].itemProperties.maxValue - array[num6].itemProperties.minValue;
					num4 += Mathf.Clamp(random.Next(array[num6].itemProperties.minValue, array[num6].itemProperties.maxValue), array[num6].scrapValue - 6 * num6, array[num6].scrapValue + 9 * num6);
					num3++;
				}
			}
			modifiedDisplayText = modifiedDisplayText.Replace("[scanForItems]", $"There are {num3} objects outside the ship, totalling at an approximate value of ${num4}.");
		}
		modifiedDisplayText = ((numberOfItemsInDropship > 0) ? modifiedDisplayText.Replace("[numberOfItemsOnRoute]", $"{numberOfItemsInDropship} purchased items on route.") : modifiedDisplayText.Replace("[numberOfItemsOnRoute]", ""));
		modifiedDisplayText = modifiedDisplayText.Replace("[currentDay]", DateTime.Now.DayOfWeek.ToString());
		modifiedDisplayText = modifiedDisplayText.Replace("[variableAmount]", playerDefinedAmount.ToString());
		modifiedDisplayText = modifiedDisplayText.Replace("[playerCredits]", "$" + groupCredits);
		modifiedDisplayText = modifiedDisplayText.Replace("[totalCost]", "$" + totalCostOfItems);
		modifiedDisplayText = modifiedDisplayText.Replace("[companyBuyingPercent]", $"{Mathf.RoundToInt(StartOfRound.Instance.companyBuyingRate * 100f)}%");
		if ((bool)displayingPersistentImage)
		{
			modifiedDisplayText = "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n" + modifiedDisplayText;
		}
		return modifiedDisplayText;
	}

	public void RunTerminalEvents(TerminalNode node)
	{
		if (!string.IsNullOrWhiteSpace(node.terminalEvent))
		{
			if (node.terminalEvent == "setUpTerminal")
			{
				ES3.Save("HasUsedTerminal", value: true, "LCGeneralSaveData");
			}
			if (node.terminalEvent == "cheat_ResetCredits" && (GameNetworkManager.Instance.localPlayerController.playerUsername == "Zeekerss" || GameNetworkManager.Instance.localPlayerController.playerUsername == "Blueray" || GameNetworkManager.Instance.localPlayerController.playerUsername == "Puffo") && GameNetworkManager.Instance.localPlayerController.IsServer)
			{
				useCreditsCooldown = true;
				groupCredits = 2500;
				SyncGroupCreditsServerRpc(groupCredits, numberOfItemsInDropship);
			}
			if (node.terminalEvent == "switchCamera")
			{
				StartOfRound.Instance.mapScreen.SwitchRadarTargetForward(callRPC: true);
			}
			if (base.IsServer && node.terminalEvent == "ejectPlayers" && !StartOfRound.Instance.isChallengeFile && StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.firingPlayersCutsceneRunning)
			{
				StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
			}
		}
	}

	public void LoadTerminalImage(TerminalNode node)
	{
		if ((bool)node.displayVideo)
		{
			terminalImage.enabled = true;
			terminalImage.texture = videoTexture;
			displayingPersistentImage = null;
			videoPlayer.clip = node.displayVideo;
			videoPlayer.enabled = true;
			if (node.loadImageSlowly)
			{
				if (loadImageCoroutine != null)
				{
					StopCoroutine(loadImageCoroutine);
				}
				loadImageCoroutine = StartCoroutine(loadImageSlowly());
			}
			return;
		}
		videoPlayer.enabled = false;
		if (node.displayTexture != null)
		{
			terminalImage.enabled = true;
			terminalImage.texture = node.displayTexture;
			if (node.persistentImage)
			{
				if (StartOfRound.Instance.inShipPhase || displayingPersistentImage == node.displayTexture)
				{
					displayingPersistentImage = null;
					terminalImage.enabled = false;
					return;
				}
				displayingPersistentImage = node.displayTexture;
			}
			if (node.loadImageSlowly)
			{
				if (loadImageCoroutine != null)
				{
					StopCoroutine(loadImageCoroutine);
				}
				loadImageCoroutine = StartCoroutine(loadImageSlowly());
			}
		}
		else if (!displayingPersistentImage)
		{
			terminalImage.enabled = false;
		}
	}

	private IEnumerator loadImageSlowly()
	{
		float paddingValue = 300f;
		while (paddingValue > 0f)
		{
			paddingValue -= Time.deltaTime * 100f * UnityEngine.Random.Range(0.3f, 1.7f);
			terminalImageMask.padding = new Vector4(0f, paddingValue, 0f, 0f);
			yield return null;
		}
		terminalImageMask.padding = Vector4.zero;
	}

	public void OnSubmit()
	{
		if (!terminalInUse)
		{
			return;
		}
		if (textAdded == 0)
		{
			screenText.ActivateInputField();
			screenText.Select();
			return;
		}
		if (currentNode != null && currentNode.acceptAnything)
		{
			LoadNewNode(currentNode.terminalOptions[0].result);
		}
		else
		{
			TerminalNode terminalNode = ParsePlayerSentence();
			if (terminalNode != null)
			{
				if (terminalNode.buyRerouteToMoon == -2)
				{
					totalCostOfItems = terminalNode.itemCost;
				}
				else if (terminalNode.itemCost != 0)
				{
					totalCostOfItems = terminalNode.itemCost * playerDefinedAmount;
				}
				if (terminalNode.buyItemIndex != -1 || (terminalNode.buyRerouteToMoon != -1 && terminalNode.buyRerouteToMoon != -2) || terminalNode.shipUnlockableID != -1 || terminalNode.buyVehicleIndex != -1)
				{
					LoadNewNodeIfAffordable(terminalNode);
				}
				else if (terminalNode.creatureFileID != -1)
				{
					AttemptLoadCreatureFileNode(terminalNode);
				}
				else if (terminalNode.storyLogFileID != -1)
				{
					AttemptLoadStoryLogFileNode(terminalNode);
				}
				else
				{
					LoadNewNode(terminalNode);
				}
			}
			else
			{
				Debug.Log("load 7");
				modifyingText = true;
				screenText.text = screenText.text.Substring(0, screenText.text.Length - textAdded);
				currentText = screenText.text;
				textAdded = 0;
			}
		}
		screenText.ActivateInputField();
		screenText.Select();
		if (forceScrollbarCoroutine != null)
		{
			StopCoroutine(forceScrollbarCoroutine);
		}
		forceScrollbarCoroutine = StartCoroutine(forceScrollbarUp());
	}

	private void AttemptLoadCreatureFileNode(TerminalNode node)
	{
		if (scannedEnemyIDs.Contains(node.creatureFileID))
		{
			newlyScannedEnemyIDs.Remove(node.creatureFileID);
			LoadNewNode(node);
		}
		else
		{
			LoadNewNode(terminalNodes.specialNodes[6]);
		}
	}

	private void AttemptLoadStoryLogFileNode(TerminalNode node)
	{
		if (unlockedStoryLogs.Contains(node.storyLogFileID))
		{
			newlyUnlockedStoryLogs.Remove(node.storyLogFileID);
			LoadNewNode(node);
		}
		else
		{
			LoadNewNode(terminalNodes.specialNodes[9]);
		}
	}

	private void LoadNewNodeIfAffordable(TerminalNode node)
	{
		StartOfRound startOfRound = UnityEngine.Object.FindObjectOfType<StartOfRound>();
		if (node.buyRerouteToMoon != -1 && node.buyRerouteToMoon != -2)
		{
			if (!startOfRound.inShipPhase || startOfRound.travellingToNewLevel)
			{
				LoadNewNode(terminalNodes.specialNodes[3]);
				return;
			}
			playerDefinedAmount = 1;
		}
		else if (node.shipUnlockableID != -1)
		{
			playerDefinedAmount = 1;
		}
		if (useCreditsCooldown)
		{
			LoadNewNode(terminalNodes.specialNodes[5]);
			return;
		}
		if (node.buyItemIndex != -1)
		{
			if (node.buyItemIndex != -7)
			{
				totalCostOfItems = (int)((float)buyableItemsList[node.buyItemIndex].creditsWorth * ((float)itemSalesPercentages[node.buyItemIndex] / 100f) * (float)playerDefinedAmount);
			}
			else
			{
				totalCostOfItems = node.itemCost * playerDefinedAmount;
			}
		}
		else if (node.buyVehicleIndex != -1)
		{
			int num = buyableItemsList.Length + node.buyVehicleIndex;
			totalCostOfItems = (int)((float)node.itemCost * ((float)itemSalesPercentages[num] / 100f));
		}
		else if (node.buyRerouteToMoon != -1 || node.shipUnlockableID != -1)
		{
			totalCostOfItems = node.itemCost;
		}
		float num2 = 0f;
		if (node.buyItemIndex != -1)
		{
			for (int i = 0; i < playerDefinedAmount; i++)
			{
				num2 = ((node.buyItemIndex != -7) ? (num2 + 1f) : (num2 + 9f));
			}
		}
		if (node.shipUnlockableID != -1)
		{
			if (node.shipUnlockableID >= StartOfRound.Instance.unlockablesList.unlockables.Count)
			{
				LoadNewNode(terminalNodes.specialNodes[16]);
				return;
			}
			UnlockableItem unlockableItem = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID];
			if (unlockableItem.inStorage && (unlockableItem.hasBeenUnlockedByPlayer || unlockableItem.alreadyUnlocked) && (node.returnFromStorage || unlockableItem.maxNumber <= 1))
			{
				startOfRound.ReturnUnlockableFromStorageServerRpc(node.shipUnlockableID);
				LoadNewNode(terminalNodes.specialNodes[17]);
				return;
			}
		}
		if (groupCredits < totalCostOfItems && (node.buyVehicleIndex == -1 || !hasWarrantyTicket))
		{
			LoadNewNode(terminalNodes.specialNodes[2]);
			return;
		}
		if (playerDefinedAmount > 12 || num2 + (float)numberOfItemsInDropship > 12f)
		{
			LoadNewNode(terminalNodes.specialNodes[4]);
			return;
		}
		if (node.buyVehicleIndex != -1 && numberOfItemsInDropship > 0)
		{
			LoadNewNode(terminalNodes.specialNodes[27]);
			return;
		}
		if (vehicleInDropship && node.buyItemIndex != -1)
		{
			LoadNewNode(terminalNodes.specialNodes[25]);
			return;
		}
		if (node.buyVehicleIndex != -1)
		{
			if (vehicleInDropship || (bool)UnityEngine.Object.FindObjectOfType<VehicleController>())
			{
				LoadNewNode(terminalNodes.specialNodes[26]);
				return;
			}
			ItemDropship itemDropship = UnityEngine.Object.FindObjectOfType<ItemDropship>();
			if (itemDropship != null && itemDropship.deliveringVehicle)
			{
				LoadNewNode(terminalNodes.specialNodes[26]);
				return;
			}
		}
		else if (node.buyRerouteToMoon != -1 && node.buyRerouteToMoon != -2)
		{
			if (StartOfRound.Instance.isChallengeFile)
			{
				LoadNewNode(terminalNodes.specialNodes[24]);
				return;
			}
			if (StartOfRound.Instance.levels[node.buyRerouteToMoon] == StartOfRound.Instance.currentLevel)
			{
				LoadNewNode(terminalNodes.specialNodes[8]);
				return;
			}
		}
		else if (node.shipUnlockableID != -1)
		{
			UnlockableItem unlockableItem2 = StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID];
			if ((!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded) || StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
			{
				LoadNewNode(terminalNodes.specialNodes[15]);
				return;
			}
			if (!ShipDecorSelection.Contains(node) && !unlockableItem2.alwaysInStock && (!node.buyUnlockable || unlockableItem2.shopSelectionNode == null))
			{
				LoadNewNode(terminalNodes.specialNodes[16]);
				return;
			}
			if (unlockableItem2.hasBeenUnlockedByPlayer || unlockableItem2.alreadyUnlocked)
			{
				LoadNewNode(terminalNodes.specialNodes[14]);
				return;
			}
		}
		if (!node.isConfirmationNode)
		{
			if (node.shipUnlockableID != -1)
			{
				if (node.buyUnlockable)
				{
					groupCredits = Mathf.Clamp(groupCredits - totalCostOfItems, 0, 10000000);
				}
			}
			else if (!hasWarrantyTicket || node.buyVehicleIndex == -1)
			{
				groupCredits = Mathf.Clamp(groupCredits - totalCostOfItems, 0, 10000000);
			}
			if (node.buyItemIndex != -1)
			{
				for (int j = 0; j < playerDefinedAmount; j++)
				{
					if (node.buyItemIndex == -7)
					{
						orderedItemsFromTerminal.Add(5);
						for (int k = 0; k < 4; k++)
						{
							orderedItemsFromTerminal.Add(1);
						}
						for (int l = 0; l < 4; l++)
						{
							orderedItemsFromTerminal.Add(6);
						}
						numberOfItemsInDropship += 9;
					}
					else
					{
						orderedItemsFromTerminal.Add(node.buyItemIndex);
						numberOfItemsInDropship++;
					}
				}
				if (!base.IsServer)
				{
					SyncBoughtItemsWithServer(orderedItemsFromTerminal.ToArray(), numberOfItemsInDropship);
				}
				else
				{
					SyncGroupCreditsClientRpc(groupCredits, numberOfItemsInDropship);
				}
			}
			else if (node.buyRerouteToMoon != -1 && node.buyRerouteToMoon != -2)
			{
				useCreditsCooldown = true;
				startOfRound.ChangeLevelServerRpc(node.buyRerouteToMoon, groupCredits);
			}
			else if (node.shipUnlockableID != -1 && node.buyUnlockable)
			{
				HUDManager.Instance.DisplayTip("Tip", "Press B to move and place objects in the ship, E to cancel.", isWarning: false, useSave: true, "LC_MoveObjectsTip");
				startOfRound.BuyShipUnlockableServerRpc(node.shipUnlockableID, groupCredits);
			}
			else if (node.buyVehicleIndex != -1)
			{
				orderedVehicleFromTerminal = node.buyVehicleIndex;
				vehicleInDropship = true;
				Debug.Log($"Is server?: {base.IsServer}");
				if (!base.IsServer)
				{
					SyncBoughtVehicleWithServer(orderedVehicleFromTerminal);
				}
				else
				{
					hasWarrantyTicket = !hasWarrantyTicket;
					BuyVehicleClientRpc(groupCredits, hasWarrantyTicket);
				}
			}
		}
		LoadNewNode(node);
	}

	public void ClearBoughtItems()
	{
		orderedItemsFromTerminal.Clear();
		numberOfItemsInDropship = 0;
	}

	private void SyncBoughtVehicleWithServer(int vehicleID)
	{
		if (!base.IsServer)
		{
			useCreditsCooldown = true;
			BuyVehicleServerRpc(vehicleID, groupCredits, hasWarrantyTicket);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BuyVehicleServerRpc(int vehicleID, int newGroupCredits, bool useWarranty = false)
{if(newGroupCredits <= groupCredits)			{
				hasWarrantyTicket = !useWarranty;
				groupCredits = newGroupCredits;
				vehicleInDropship = true;
				orderedVehicleFromTerminal = vehicleID;
				BuyVehicleClientRpc(newGroupCredits, hasWarrantyTicket);
			}
}
	[ClientRpc]
	public void BuyVehicleClientRpc(int newGroupCredits, bool giveWarranty)
			{
				hasWarrantyTicket = giveWarranty;
				useCreditsCooldown = false;
				groupCredits = newGroupCredits;
				vehicleInDropship = true;
			}

	private void SyncBoughtItemsWithServer(int[] boughtItems, int numItemsInShip)
	{
		if (!base.IsServer && boughtItems.Length <= 12)
		{
			useCreditsCooldown = true;
			BuyItemsServerRpc(boughtItems, groupCredits, numItemsInShip);
			orderedItemsFromTerminal.Clear();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BuyItemsServerRpc(int[] boughtItems, int newGroupCredits, int numItemsInShip)
{if(boughtItems.Length <= 12 && newGroupCredits <= groupCredits)		{
			orderedItemsFromTerminal.AddRange(boughtItems.ToList());
			groupCredits = newGroupCredits;
			SyncGroupCreditsClientRpc(newGroupCredits, numItemsInShip);
		}
}
	[ServerRpc]
	public void SyncGroupCreditsServerRpc(int newGroupCredits, int numItemsInShip)
{		{
			if (newGroupCredits < 0)
			{
				newGroupCredits = groupCredits;
			}
			else
			{
				groupCredits = newGroupCredits;
			}
			SyncGroupCreditsClientRpc(newGroupCredits, numItemsInShip);
		}
}
	[ClientRpc]
	public void SyncGroupCreditsClientRpc(int newGroupCredits, int numItemsInShip)
{		{
			if (numItemsInShip != -1)
			{
				numberOfItemsInDropship = numItemsInShip;
			}
			useCreditsCooldown = false;
			groupCredits = newGroupCredits;
		}
}
	private TerminalNode ParsePlayerSentence()
	{
		broadcastedCodeThisFrame = false;
		string s = screenText.text.Substring(screenText.text.Length - textAdded);
		s = RemovePunctuation(s);
		string[] array = s.Split(" ", StringSplitOptions.RemoveEmptyEntries);
		TerminalKeyword terminalKeyword = null;
		if (currentNode != null && currentNode.overrideOptions)
		{
			for (int i = 0; i < array.Length; i++)
			{
				TerminalNode terminalNode = ParseWordOverrideOptions(array[i], currentNode.terminalOptions);
				if (terminalNode != null)
				{
					return terminalNode;
				}
			}
			return null;
		}
		if (array.Length > 1)
		{
			switch (array[0])
			{
			case "switch":
			{
				int num = CheckForPlayerNameCommand(array[0], array[1]);
				if (num != -1)
				{
					StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(num);
					return terminalNodes.specialNodes[20];
				}
				break;
			}
			case "flash":
			{
				int num = CheckForPlayerNameCommand(array[0], array[1]);
				if (num != -1)
				{
					StartOfRound.Instance.mapScreen.FlashRadarBooster(num);
					return terminalNodes.specialNodes[23];
				}
				if (StartOfRound.Instance.mapScreen.radarTargets[StartOfRound.Instance.mapScreen.targetTransformIndex].isNonPlayer)
				{
					StartOfRound.Instance.mapScreen.FlashRadarBooster(StartOfRound.Instance.mapScreen.targetTransformIndex);
					return terminalNodes.specialNodes[23];
				}
				break;
			}
			case "ping":
			{
				int num = CheckForPlayerNameCommand(array[0], array[1]);
				if (num != -1)
				{
					StartOfRound.Instance.mapScreen.PingRadarBooster(num);
					return terminalNodes.specialNodes[21];
				}
				break;
			}
			case "transmit":
			{
				SignalTranslator signalTranslator = UnityEngine.Object.FindObjectOfType<SignalTranslator>();
				if (!(signalTranslator != null) || !(Time.realtimeSinceStartup - signalTranslator.timeLastUsingSignalTranslator > 8f) || array.Length < 2)
				{
					break;
				}
				string text = s.Substring(8);
				if (!string.IsNullOrEmpty(text))
				{
					if (!base.IsServer)
					{
						signalTranslator.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
					}
					HUDManager.Instance.UseSignalTranslatorServerRpc(text.Substring(0, Mathf.Min(text.Length, 10)));
					return terminalNodes.specialNodes[22];
				}
				break;
			}
			}
		}
		terminalKeyword = CheckForExactSentences(s);
		if (terminalKeyword != null)
		{
			if (terminalKeyword.accessTerminalObjects)
			{
				CallFunctionInAccessibleTerminalObject(terminalKeyword.word);
				PlayBroadcastCodeEffect();
				return null;
			}
			if (terminalKeyword.specialKeywordResult != null)
			{
				return terminalKeyword.specialKeywordResult;
			}
		}
		string value = Regex.Match(s, "\\d+").Value;
		if (!string.IsNullOrWhiteSpace(value))
		{
			playerDefinedAmount = Mathf.Clamp(int.Parse(value), 0, 10);
		}
		else
		{
			playerDefinedAmount = 1;
		}
		if (array.Length > 5)
		{
			return null;
		}
		TerminalKeyword terminalKeyword2 = null;
		TerminalKeyword terminalKeyword3 = null;
		new List<TerminalKeyword>();
		bool flag = false;
		hasGottenNoun = false;
		hasGottenVerb = false;
		for (int j = 0; j < array.Length; j++)
		{
			terminalKeyword = ParseWord(array[j]);
			if (terminalKeyword != null)
			{
				Debug.Log("Parsed word: " + array[j]);
				if (terminalKeyword.isVerb)
				{
					if (hasGottenVerb)
					{
						continue;
					}
					hasGottenVerb = true;
					terminalKeyword2 = terminalKeyword;
				}
				else
				{
					if (hasGottenNoun)
					{
						continue;
					}
					hasGottenNoun = true;
					terminalKeyword3 = terminalKeyword;
					if (terminalKeyword.accessTerminalObjects)
					{
						broadcastedCodeThisFrame = true;
						CallFunctionInAccessibleTerminalObject(terminalKeyword.word);
						flag = true;
					}
				}
				if (!flag && hasGottenNoun && hasGottenVerb)
				{
					break;
				}
			}
			else
			{
				Debug.Log("Could not parse word: " + array[j]);
			}
		}
		if (broadcastedCodeThisFrame)
		{
			PlayBroadcastCodeEffect();
			return terminalNodes.specialNodes[19];
		}
		hasGottenNoun = false;
		hasGottenVerb = false;
		if (terminalKeyword3 == null)
		{
			return terminalNodes.specialNodes[10];
		}
		if (terminalKeyword2 == null)
		{
			if (!(terminalKeyword3.defaultVerb != null))
			{
				return terminalNodes.specialNodes[11];
			}
			terminalKeyword2 = terminalKeyword3.defaultVerb;
		}
		for (int k = 0; k < terminalKeyword2.compatibleNouns.Length; k++)
		{
			if (terminalKeyword2.compatibleNouns[k].noun == terminalKeyword3)
			{
				Debug.Log($"noun keyword: {terminalKeyword3.word} ; verb keyword: {terminalKeyword2.word} ; result null? : {terminalKeyword2.compatibleNouns[k].result == null}");
				Debug.Log("result: " + terminalKeyword2.compatibleNouns[k].result.name);
				return terminalKeyword2.compatibleNouns[k].result;
			}
		}
		return terminalNodes.specialNodes[12];
	}

	private int CheckForPlayerNameCommand(string firstWord, string secondWord)
	{
		if (firstWord == "radar")
		{
			return -1;
		}
		if (secondWord.Length <= 2)
		{
			return -1;
		}
		Debug.Log("first word: " + firstWord + "; second word: " + secondWord);
		List<string> list = new List<string>();
		for (int i = 0; i < StartOfRound.Instance.mapScreen.radarTargets.Count; i++)
		{
			list.Add(StartOfRound.Instance.mapScreen.radarTargets[i].name);
			Debug.Log($"name {i}: {list[i]}");
		}
		secondWord = secondWord.ToLower();
		for (int j = 0; j < list.Count; j++)
		{
			string text = list[j].ToLower();
			if (text == secondWord)
			{
				return j;
			}
		}
		Debug.Log($"Target names length: {list.Count}");
		for (int k = 0; k < list.Count; k++)
		{
			Debug.Log("A");
			string text = list[k].ToLower();
			Debug.Log($"Word #{k}: {text}; length: {text.Length}");
			for (int num = secondWord.Length; num > 2; num--)
			{
				Debug.Log($"c: {num}");
				Debug.Log(secondWord.Substring(0, num));
				if (text.StartsWith(secondWord.Substring(0, num)))
				{
					return k;
				}
			}
		}
		return -1;
	}

	private TerminalKeyword CheckForExactSentences(string playerWord)
	{
		for (int i = 0; i < terminalNodes.allKeywords.Length; i++)
		{
			if (terminalNodes.allKeywords[i].word == playerWord)
			{
				return terminalNodes.allKeywords[i];
			}
		}
		return null;
	}

	private TerminalKeyword ParseWord(string playerWord, int specificityRequired = 2)
	{
		if (playerWord.Length < specificityRequired)
		{
			return null;
		}
		TerminalKeyword terminalKeyword = null;
		for (int i = 0; i < terminalNodes.allKeywords.Length; i++)
		{
			if (terminalNodes.allKeywords[i].isVerb && hasGottenVerb)
			{
				continue;
			}
			_ = terminalNodes.allKeywords[i].accessTerminalObjects;
			if (terminalNodes.allKeywords[i].word == playerWord)
			{
				return terminalNodes.allKeywords[i];
			}
			if (!(terminalKeyword == null))
			{
				continue;
			}
			for (int num = playerWord.Length; num > specificityRequired; num--)
			{
				if (terminalNodes.allKeywords[i].word.StartsWith(playerWord.Substring(0, num)))
				{
					terminalKeyword = terminalNodes.allKeywords[i];
				}
			}
		}
		return terminalKeyword;
	}

	private TerminalNode ParseWordOverrideOptions(string playerWord, CompatibleNoun[] options)
	{
		for (int i = 0; i < options.Length; i++)
		{
			for (int num = playerWord.Length; num > 0; num--)
			{
				if (options[i].noun.word.StartsWith(playerWord.Substring(0, num)))
				{
					return options[i].result;
				}
			}
		}
		return null;
	}

	public void TextChanged(string newText)
	{
		if (currentNode == null)
		{
			return;
		}
		if (modifyingText)
		{
			modifyingText = false;
			return;
		}
		textAdded += newText.Length - currentText.Length;
		if (textAdded < 0)
		{
			screenText.text = currentText;
			textAdded = 0;
		}
		else if (textAdded > currentNode.maxCharactersToType)
		{
			screenText.text = currentText;
			textAdded = currentNode.maxCharactersToType;
		}
		else
		{
			currentText = newText;
		}
	}

	private string RemovePunctuation(string s)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in s)
		{
			if (!char.IsPunctuation(c))
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString().ToLower();
	}

	private void CallFunctionInAccessibleTerminalObject(string word)
	{
		TerminalAccessibleObject[] array = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].objectCode == word)
			{
				Debug.Log("Found accessible terminal object with corresponding string, calling function");
				broadcastedCodeThisFrame = true;
				array[i].CallFunctionFromTerminal();
			}
		}
	}

	private void PlayBroadcastCodeEffect()
	{
		codeBroadcastAnimator.SetTrigger("display");
		terminalAudio.PlayOneShot(codeBroadcastSFX, 1f);
	}

	private void Awake()
	{
		playerActions = new PlayerActions();
		playerActions.Movement.Enable();
	}

	private void Start()
	{
		terminalTrigger = base.gameObject.GetComponent<InteractTrigger>();
		roundManager = UnityEngine.Object.FindObjectOfType<RoundManager>();
		if (base.IsServer)
		{
			syncedTerminalValues = true;
			int num = 0;
			if (StartOfRound.Instance.isChallengeFile)
			{
				System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 1);
				if (random.Next(0, 100) < 50)
				{
					groupCredits = random.Next(20, 900);
				}
				else
				{
					groupCredits = random.Next(20, 300);
				}
			}
			else
			{
				num = ES3.Load("Reimburse", GameNetworkManager.Instance.currentSaveFileName, 0);
				groupCredits = ES3.Load("GroupCredits", GameNetworkManager.Instance.currentSaveFileName, TimeOfDay.Instance.quotaVariables.startingCredits) + num;
				hasWarrantyTicket = ES3.Load("VehicleWarrantyTicket", GameNetworkManager.Instance.currentSaveFileName, defaultValue: false);
			}
			startingCreditsAmount = groupCredits;
			Debug.Log($"Group credits: {groupCredits}");
			if (ES3.KeyExists("EnemyScans", GameNetworkManager.Instance.currentSaveFileName))
			{
				scannedEnemyIDs = ES3.Load<int[]>("EnemyScans", GameNetworkManager.Instance.currentSaveFileName).ToList();
			}
			if (ES3.KeyExists("StoryLogs", GameNetworkManager.Instance.currentSaveFileName))
			{
				unlockedStoryLogs = ES3.Load<int[]>("StoryLogs", GameNetworkManager.Instance.currentSaveFileName).ToList();
			}
			else
			{
				unlockedStoryLogs.Add(0);
			}
			if (num > 0)
			{
				StartCoroutine(displayReimbursedTipDelay());
			}
		}
		StartCoroutine(waitUntilFrameEndToSetActive(active: false));
	}

	private IEnumerator waitUntilFrameEndToSetActive(bool active)
	{
		yield return new WaitForEndOfFrame();
		terminalUIScreen.gameObject.SetActive(active);
	}

	private IEnumerator displayReimbursedTipDelay()
	{
		yield return new WaitForSeconds(3.5f);
		QuickMenuManager quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
		yield return new WaitUntil(() => !quickMenu.isMenuOpen);
		HUDManager.Instance.DisplayTip("Welcome back!", "You have been reimbursed for your previously bought tools. If you want them back, you will have to buy them.", isWarning: false, useSave: true, "LCTip_Reimbursed");
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncTerminalValuesServerRpc()
{		{
			if (scannedEnemyIDs.Count > 0)
			{
				SyncTerminalValuesClientRpc(groupCredits, numberOfItemsInDropship, hasWarrantyTicket, scannedEnemyIDs.ToArray(), unlockedStoryLogs.ToArray());
			}
			else
			{
				SyncTerminalValuesClientRpc(groupCredits, numberOfItemsInDropship, hasWarrantyTicket);
			}
		}
}
	[ClientRpc]
	public void SyncTerminalValuesClientRpc(int newGroupCredits = 0, int numItemsInDropship = 0, bool vehicleWarranty = false, int[] scannedEnemies = null, int[] storyLogs = null)
{if(syncedTerminalValues)		{
			return;
		}
		syncedTerminalValues = true;
		startingCreditsAmount = newGroupCredits;
		numberOfItemsInDropship = numItemsInDropship;
		groupCredits = newGroupCredits;
		hasWarrantyTicket = vehicleWarranty;
		if (base.IsServer)
		{
			return;
		}
		if (scannedEnemies != null)
		{
			for (int i = 0; i < scannedEnemies.Length; i++)
			{
				scannedEnemyIDs.Add(scannedEnemies[i]);
				Debug.Log("Syncing scanned enemies list with clients");
			}
		}
		if (storyLogs != null)
		{
			for (int j = 0; j < storyLogs.Length; j++)
			{
				unlockedStoryLogs.Add(storyLogs[j]);
			}
		}
}
	public void BeginUsingTerminal()
	{
		terminalInUse = true;
		try
		{
			StartCoroutine(waitUntilFrameEndToSetActive(active: true));
			GameNetworkManager.Instance.localPlayerController.inTerminalMenu = true;
			Debug.Log($"Set interminalmenu to true: {GameNetworkManager.Instance.localPlayerController.inTerminalMenu}");
			if (selectTextFieldCoroutine != null)
			{
				StopCoroutine(selectTextFieldCoroutine);
			}
			selectTextFieldCoroutine = StartCoroutine(selectTextFieldDelayed());
			HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 0f, 0.13f, 0.13f);
			HUDManager.Instance.PingHUDElement(HUDManager.Instance.PlayerInfo, 0f, 0.13f, 0.13f);
			HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat, 0f, 0.35f, 0.13f);
			HUDManager.Instance.PingHUDElement(HUDManager.Instance.Tooltips, 1f, 0f, 0.6f);
			inputFieldText.enableWordWrapping = true;
			if (!ES3.Load("HasUsedTerminal", "LCGeneralSaveData", defaultValue: false))
			{
				LoadNewNode(terminalNodes.specialNodes[0]);
			}
			else if (!usedTerminalThisSession)
			{
				LoadNewNode(terminalNodes.specialNodes[1]);
			}
			else
			{
				LoadNewNode(terminalNodes.specialNodes[13]);
			}
			if (!usedTerminalThisSession)
			{
				usedTerminalThisSession = true;
				if (!syncedTerminalValues)
				{
					SyncTerminalValuesServerRpc();
				}
			}
			SetTerminalInUseLocalClient(inUse: true);
			if (StartOfRound.Instance.localPlayerUsingController && !GameNetworkManager.Instance.disableSteam)
			{
				SteamUtils.ShowGamepadTextInput(GamepadTextInputMode.Normal, GamepadTextInputLineMode.SingleLine, "Type command", currentNode.maxCharactersToType);
				SteamUtils.OnGamepadTextInputDismissed += OnGamepadTextInputDismissed_t;
				displayingSteamKeyboard = true;
			}
			terminalAudio.PlayOneShot(enterTerminalSFX);
			if (StartOfRound.Instance.localPlayerUsingController)
			{
				HUDManager.Instance.ChangeControlTip(0, "Quit terminal : [Start]", clearAllOther: true);
			}
			else
			{
				HUDManager.Instance.ChangeControlTip(0, "Quit terminal : [TAB]", clearAllOther: true);
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"Caught error while entering computer terminal. Exiting player from terminal. Error: {arg}");
			QuitTerminal();
		}
	}

	public void OnGamepadTextInputDismissed_t(bool submitted)
	{
		if (submitted)
		{
			int maxCharactersToType = currentNode.maxCharactersToType;
			string enteredGamepadText = SteamUtils.GetEnteredGamepadText();
			if (string.IsNullOrEmpty(enteredGamepadText) || enteredGamepadText.Length <= maxCharactersToType)
			{
				screenText.text += textAdded;
				OnSubmit();
			}
		}
	}

	private IEnumerator selectTextFieldDelayed()
	{
		screenText.ActivateInputField();
		yield return new WaitForSeconds(1f);
		screenText.Select();
	}

	public void QuitTerminal(bool syncTerminalInUse = true)
	{
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		terminalTrigger.StopSpecialAnimation();
		terminalInUse = false;
		StartCoroutine(waitUntilFrameEndToSetActive(active: false));
		localPlayerController.inTerminalMenu = false;
		timeSinceTerminalInUse = 0f;
		Debug.Log("Quit terminal; inTerminalMenu true?: {playerScript.inTerminalMenu}");
		if (selectTextFieldCoroutine != null)
		{
			StopCoroutine(selectTextFieldCoroutine);
		}
		screenText.ReleaseSelection();
		screenText.DeactivateInputField();
		if (EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(null);
		}
		scrollBarVertical.value = 0f;
		HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 0f, 0.5f, 0.5f);
		HUDManager.Instance.PingHUDElement(HUDManager.Instance.PlayerInfo, 0f, 1f, 1f);
		HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat, 0f, 1f, 1f);
		HUDManager.Instance.PingHUDElement(HUDManager.Instance.Tooltips, 0f, 1f, 1f);
		if (displayingSteamKeyboard)
		{
			SteamUtils.OnGamepadTextInputDismissed -= OnGamepadTextInputDismissed_t;
		}
		if (localPlayerController.isHoldingObject && localPlayerController.currentlyHeldObjectServer != null)
		{
			localPlayerController.currentlyHeldObjectServer.SetControlTipsForItem();
		}
		else
		{
			HUDManager.Instance.ClearControlTips();
		}
		if (syncTerminalInUse)
		{
			SetTerminalInUseLocalClient(inUse: false);
		}
		terminalAudio.PlayOneShot(leaveTerminalSFX);
	}

	private void OnEnable()
	{
		playerActions.Movement.OpenMenu.performed += PressESC;
	}

	private void OnDisable()
	{
		Debug.Log("Terminal disabled, disabling ESC key listener");
		playerActions.Movement.OpenMenu.performed -= PressESC;
	}

	private void PressESC(InputAction.CallbackContext context)
	{
		if (context.performed && terminalInUse)
		{
			QuitTerminal();
		}
	}

	public void RotateShipDecorSelection()
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 65);
		ShipDecorSelection.Clear();
		List<TerminalNode> list = new List<TerminalNode>();
		for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++)
		{
			if (StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode != null && !StartOfRound.Instance.unlockablesList.unlockables[i].alwaysInStock)
			{
				list.Add(StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode);
			}
		}
		int num = random.Next(4, 6);
		for (int j = 0; j < num; j++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			TerminalNode item = list[random.Next(0, list.Count)];
			ShipDecorSelection.Add(item);
			list.Remove(item);
		}
	}

	private void InitializeItemSalesPercentages()
	{
		itemSalesPercentages = new int[buyableItemsList.Length + buyableVehicles.Length];
		for (int i = 0; i < itemSalesPercentages.Length; i++)
		{
			Debug.Log($"Item sales percentages #{i}: {itemSalesPercentages[i]}");
			itemSalesPercentages[i] = 100;
		}
	}

	public void SetItemSales()
	{
		if (itemSalesPercentages == null || itemSalesPercentages.Length == 0)
		{
			InitializeItemSalesPercentages();
		}
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 90);
		DateTime dateTime = new DateTime(DateTime.Now.Year, 10, 23);
		bool num = DateTime.Today == dateTime;
		int minValue = -10;
		int maxValue = 5;
		if (num)
		{
			minValue = -2;
			maxValue = 9;
		}
		int num2 = Mathf.Max(random.Next(minValue, maxValue), 0);
		if (num2 <= 0)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < buyableItemsList.Length; i++)
		{
			list.Add(i);
			itemSalesPercentages[i] = 100;
		}
		for (int j = 0; j < buyableVehicles.Length; j++)
		{
			list.Add(j);
			itemSalesPercentages[j + buyableItemsList.Length] = 100;
		}
		for (int k = 0; k < num2; k++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			int num3 = random.Next(0, list.Count);
			int maxValue2 = ((num3 < buyableItemsList.Length) ? Mathf.Clamp(buyableItemsList[num3].highestSalePercentage, 0, 90) : 80);
			int i2 = 100 - random.Next(0, maxValue2);
			i2 = RoundToNearestTen(i2);
			itemSalesPercentages[num3] = i2;
			list.RemoveAt(num3);
		}
	}

	private int RoundToNearestTen(int i)
	{
		return (int)Math.Round((double)i / 10.0) * 10;
	}

	public void SetTerminalInUseLocalClient(bool inUse)
	{
		SetTerminalInUseServerRpc(inUse);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetTerminalInUseServerRpc(bool inUse)
{		{
			if (placeableObject.inUse)
			{
				KickOffTerminalClientRpc();
			}
			else
			{
				SetTerminalInUseClientRpc(inUse);
			}
		}
}
	[ClientRpc]
	public void KickOffTerminalClientRpc()
{		{
			if (terminalInUse)
			{
				GameNetworkManager.Instance.localPlayerController.CancelSpecialTriggerAnimations();
			}
			placeableObject.inUse = false;
			terminalLight.enabled = false;
		}
}
	[ClientRpc]
	public void SetTerminalInUseClientRpc(bool inUse)
			{
				placeableObject.inUse = inUse;
				terminalLight.enabled = inUse;
			}

	public void SetTerminalNoLongerInUse()
	{
		placeableObject.inUse = false;
		terminalLight.enabled = false;
	}
}
