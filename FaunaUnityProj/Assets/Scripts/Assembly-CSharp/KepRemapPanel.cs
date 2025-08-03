using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KepRemapPanel : MonoBehaviour
{
	public List<RemappableKey> remappableKeys = new List<RemappableKey>();

	public List<GameObject> keySlots = new List<GameObject>();

	public GameObject keyRemapSlotPrefab;

	public RectTransform keyRemapContainer;

	public float maxVertical;

	public float horizontalOffset;

	public float verticalOffset;

	public int currentVertical;

	public int currentHorizontal;

	public GameObject sectionTextPrefab;

	public void ResetKeybindsUI()
	{
		UnloadKeybindsUI();
		LoadKeybindsUI();
	}

	private void OnDisable()
	{
		UnloadKeybindsUI();
	}

	public void UnloadKeybindsUI()
	{
		for (int i = 0; i < keySlots.Count; i++)
		{
			Object.Destroy(keySlots[i]);
		}
		keySlots.Clear();
	}

	public void LoadKeybindsUI()
	{
		currentVertical = 0;
		currentHorizontal = 0;
		Vector2 anchoredPosition = new Vector2(horizontalOffset * (float)currentHorizontal, verticalOffset * (float)currentVertical);
		bool flag = false;
		int num = 0;
		for (int i = 0; i < remappableKeys.Count; i++)
		{
			if (remappableKeys[i].currentInput == null)
			{
				continue;
			}
			GameObject gameObject = Object.Instantiate(keyRemapSlotPrefab, keyRemapContainer);
			keySlots.Add(gameObject);
			gameObject.GetComponentInChildren<TextMeshProUGUI>().text = remappableKeys[i].ControlName;
			gameObject.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
			SettingsOption componentInChildren = gameObject.GetComponentInChildren<SettingsOption>();
			componentInChildren.rebindableAction = remappableKeys[i].currentInput;
			componentInChildren.rebindableActionBindingIndex = remappableKeys[i].rebindingIndex;
			componentInChildren.gamepadOnlyRebinding = remappableKeys[i].gamepadOnly;
			Debug.Log($"{remappableKeys[i].ControlName}: rebind controls length: {remappableKeys[i].currentInput.action.controls.Count}");
			Debug.Log($"{remappableKeys[i].ControlName}: rebind control binding index is {remappableKeys[i].rebindingIndex}");
			for (int j = 0; j < remappableKeys[i].currentInput.action.controls.Count; j++)
			{
				Debug.Log($"control #{j}: ${InputControlPath.ToHumanReadableString(remappableKeys[i].currentInput.action.bindings[remappableKeys[i].currentInput.action.GetBindingIndexForControl(remappableKeys[i].currentInput.action.controls[j])].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice)}");
			}
			int rebindingIndex = remappableKeys[i].rebindingIndex;
			int num2 = Mathf.Max(rebindingIndex, 0);
			componentInChildren.currentlyUsedKeyText.text = InputControlPath.ToHumanReadableString(componentInChildren.rebindableAction.action.bindings[num2].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
			Debug.Log($"bindingIndex of {componentInChildren.currentlyUsedKeyText.text} : {rebindingIndex}; display bindingIndex: {num2}");
			if (!flag && i + 1 < remappableKeys.Count && remappableKeys[i + 1].gamepadOnly)
			{
				num = (int)(maxVertical + 2f);
				currentVertical = 0;
				currentHorizontal = 0;
				GameObject gameObject2 = Object.Instantiate(sectionTextPrefab, keyRemapContainer);
				gameObject2.GetComponent<RectTransform>().anchoredPosition = new Vector2(-40f, (0f - verticalOffset) * (float)num);
				gameObject2.GetComponentInChildren<TextMeshProUGUI>().text = "REBIND CONTROLLERS";
				keySlots.Add(gameObject2);
				flag = true;
			}
			else
			{
				currentVertical++;
				if ((float)currentVertical > maxVertical)
				{
					currentVertical = 0;
					currentHorizontal++;
				}
			}
			anchoredPosition = new Vector2(horizontalOffset * (float)currentHorizontal, (0f - verticalOffset) * (float)(currentVertical + num));
		}
	}

	private void OnEnable()
	{
		LoadKeybindsUI();
	}
}
