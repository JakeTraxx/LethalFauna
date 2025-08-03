using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeltBagInventoryUI : MonoBehaviour
{
	public GameObject panel;

	public BeltBagItem currentBeltBag;

	public GameObject[] inventorySlots;

	public Image[] inventorySlotIcons;

	public int grabbingItemSlot = -1;

	public Image cursorGrabbingObjectImage;

	public bool setCursorImageV1;

	public bool displaying;

	public void ClickExitButton()
	{
		if (GameNetworkManager.Instance.localPlayerController.inSpecialMenu)
		{
			GameNetworkManager.Instance.localPlayerController.SetInSpecialMenu(setInMenu: false);
		}
		currentBeltBag = null;
		displaying = false;
	}

	public void FillSlots(BeltBagItem beltBag)
	{
		currentBeltBag = beltBag;
		displaying = true;
		for (int i = 0; i < inventorySlots.Length; i++)
		{
			if (beltBag.objectsInBag == null || beltBag.objectsInBag.Count <= i || beltBag.objectsInBag[i] == null)
			{
				inventorySlotIcons[i].enabled = false;
				continue;
			}
			inventorySlotIcons[i].enabled = true;
			inventorySlotIcons[i].sprite = beltBag.objectsInBag[i].itemProperties.itemIcon;
		}
	}

	public void OnEnable()
	{
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("ActivateItem").canceled += OnClickRelease;
	}

	public void OnDisable()
	{
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("ActivateItem").canceled -= OnClickRelease;
		displaying = false;
	}

	public void Update()
	{
		if (grabbingItemSlot != -1)
		{
			cursorGrabbingObjectImage.enabled = true;
			Vector3 position = Mouse.current.position.ReadValue();
			position.z = 2.1f;
			cursorGrabbingObjectImage.transform.position = HUDManager.Instance.UICamera.ScreenToWorldPoint(position);
		}
		else
		{
			cursorGrabbingObjectImage.enabled = false;
		}
	}

	public void OnClickRelease(InputAction.CallbackContext context)
	{
		if (grabbingItemSlot == -1)
		{
			return;
		}
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = Mouse.current.position.ReadValue();
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].gameObject == panel)
			{
				inventorySlotIcons[grabbingItemSlot].enabled = true;
				HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.handOpenCursorTex, new Vector2(16f, 16f));
				grabbingItemSlot = -1;
				return;
			}
		}
		RemoveItemFromUI(grabbingItemSlot);
	}

	public void RemoveItemFromUI(int slot)
	{
		if (currentBeltBag != null && slot != -1)
		{
			if (currentBeltBag.objectsInBag.Count > slot && currentBeltBag.objectsInBag[slot] != null && !currentBeltBag.tryingAddToBag)
			{
				inventorySlotIcons[slot].enabled = false;
				currentBeltBag.RemoveObjectFromBag(slot);
				FillSlots(currentBeltBag);
			}
			else
			{
				inventorySlotIcons[slot].enabled = true;
			}
			HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.handOpenCursorTex);
			grabbingItemSlot = -1;
		}
	}

	public void ClickInventorySlotController(int slotNumber)
	{
		if (StartOfRound.Instance.localPlayerUsingController && !(currentBeltBag == null) && currentBeltBag.objectsInBag.Count > slotNumber && currentBeltBag.objectsInBag[slotNumber] != null)
		{
			RemoveItemFromUI(slotNumber);
		}
	}

	public void ClickInventorySlot(int slotNumber)
	{
		if (!StartOfRound.Instance.localPlayerUsingController && !(currentBeltBag == null) && currentBeltBag.objectsInBag.Count > slotNumber && currentBeltBag.objectsInBag[slotNumber] != null)
		{
			grabbingItemSlot = slotNumber;
			currentBeltBag.GrabItemInBag();
			HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.handClosedCursorTex, new Vector2(16f, 16f));
			cursorGrabbingObjectImage.sprite = inventorySlotIcons[slotNumber].sprite;
			inventorySlotIcons[slotNumber].enabled = false;
		}
	}
}
