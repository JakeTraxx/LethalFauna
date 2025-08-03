using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerActions : IInputActionCollection2, IInputActionCollection, IEnumerable<InputAction>, IEnumerable, IDisposable
{
	public struct MovementActions
	{
		private PlayerActions m_Wrapper;

		public InputAction Look => m_Wrapper.m_Movement_Look;

		public InputAction Move => m_Wrapper.m_Movement_Move;

		public InputAction Jump => m_Wrapper.m_Movement_Jump;

		public InputAction Sprint => m_Wrapper.m_Movement_Sprint;

		public InputAction OpenMenu => m_Wrapper.m_Movement_OpenMenu;

		public InputAction Interact => m_Wrapper.m_Movement_Interact;

		public InputAction Crouch => m_Wrapper.m_Movement_Crouch;

		public InputAction Use => m_Wrapper.m_Movement_Use;

		public InputAction ActivateItem => m_Wrapper.m_Movement_ActivateItem;

		public InputAction Discard => m_Wrapper.m_Movement_Discard;

		public InputAction SwitchItem => m_Wrapper.m_Movement_SwitchItem;

		public InputAction QEItemInteract => m_Wrapper.m_Movement_QEItemInteract;

		public InputAction EnableChat => m_Wrapper.m_Movement_EnableChat;

		public InputAction SubmitChat => m_Wrapper.m_Movement_SubmitChat;

		public InputAction ReloadBatteries => m_Wrapper.m_Movement_ReloadBatteries;

		public InputAction SetFreeCamera => m_Wrapper.m_Movement_SetFreeCamera;

		public InputAction InspectItem => m_Wrapper.m_Movement_InspectItem;

		public InputAction SpeedCheat => m_Wrapper.m_Movement_SpeedCheat;

		public InputAction PingScan => m_Wrapper.m_Movement_PingScan;

		public InputAction VoiceButton => m_Wrapper.m_Movement_VoiceButton;

		public InputAction Emote1 => m_Wrapper.m_Movement_Emote1;

		public InputAction Emote2 => m_Wrapper.m_Movement_Emote2;

		public InputAction BuildMode => m_Wrapper.m_Movement_BuildMode;

		public InputAction ConfirmBuildMode => m_Wrapper.m_Movement_ConfirmBuildMode;

		public InputAction Delete => m_Wrapper.m_Movement_Delete;

		public bool enabled => Get().enabled;

		public MovementActions(PlayerActions wrapper)
		{
			m_Wrapper = wrapper;
		}

		public InputActionMap Get()
		{
			return m_Wrapper.m_Movement;
		}

		public void Enable()
		{
			Get().Enable();
		}

		public void Disable()
		{
			Get().Disable();
		}

		public static implicit operator InputActionMap(MovementActions set)
		{
			return set.Get();
		}

		public void AddCallbacks(IMovementActions instance)
		{
			if (instance != null && !m_Wrapper.m_MovementActionsCallbackInterfaces.Contains(instance))
			{
				m_Wrapper.m_MovementActionsCallbackInterfaces.Add(instance);
				Look.started += instance.OnLook;
				Look.performed += instance.OnLook;
				Look.canceled += instance.OnLook;
				Move.started += instance.OnMove;
				Move.performed += instance.OnMove;
				Move.canceled += instance.OnMove;
				Jump.started += instance.OnJump;
				Jump.performed += instance.OnJump;
				Jump.canceled += instance.OnJump;
				Sprint.started += instance.OnSprint;
				Sprint.performed += instance.OnSprint;
				Sprint.canceled += instance.OnSprint;
				OpenMenu.started += instance.OnOpenMenu;
				OpenMenu.performed += instance.OnOpenMenu;
				OpenMenu.canceled += instance.OnOpenMenu;
				Interact.started += instance.OnInteract;
				Interact.performed += instance.OnInteract;
				Interact.canceled += instance.OnInteract;
				Crouch.started += instance.OnCrouch;
				Crouch.performed += instance.OnCrouch;
				Crouch.canceled += instance.OnCrouch;
				Use.started += instance.OnUse;
				Use.performed += instance.OnUse;
				Use.canceled += instance.OnUse;
				ActivateItem.started += instance.OnActivateItem;
				ActivateItem.performed += instance.OnActivateItem;
				ActivateItem.canceled += instance.OnActivateItem;
				Discard.started += instance.OnDiscard;
				Discard.performed += instance.OnDiscard;
				Discard.canceled += instance.OnDiscard;
				SwitchItem.started += instance.OnSwitchItem;
				SwitchItem.performed += instance.OnSwitchItem;
				SwitchItem.canceled += instance.OnSwitchItem;
				QEItemInteract.started += instance.OnQEItemInteract;
				QEItemInteract.performed += instance.OnQEItemInteract;
				QEItemInteract.canceled += instance.OnQEItemInteract;
				EnableChat.started += instance.OnEnableChat;
				EnableChat.performed += instance.OnEnableChat;
				EnableChat.canceled += instance.OnEnableChat;
				SubmitChat.started += instance.OnSubmitChat;
				SubmitChat.performed += instance.OnSubmitChat;
				SubmitChat.canceled += instance.OnSubmitChat;
				ReloadBatteries.started += instance.OnReloadBatteries;
				ReloadBatteries.performed += instance.OnReloadBatteries;
				ReloadBatteries.canceled += instance.OnReloadBatteries;
				SetFreeCamera.started += instance.OnSetFreeCamera;
				SetFreeCamera.performed += instance.OnSetFreeCamera;
				SetFreeCamera.canceled += instance.OnSetFreeCamera;
				InspectItem.started += instance.OnInspectItem;
				InspectItem.performed += instance.OnInspectItem;
				InspectItem.canceled += instance.OnInspectItem;
				SpeedCheat.started += instance.OnSpeedCheat;
				SpeedCheat.performed += instance.OnSpeedCheat;
				SpeedCheat.canceled += instance.OnSpeedCheat;
				PingScan.started += instance.OnPingScan;
				PingScan.performed += instance.OnPingScan;
				PingScan.canceled += instance.OnPingScan;
				VoiceButton.started += instance.OnVoiceButton;
				VoiceButton.performed += instance.OnVoiceButton;
				VoiceButton.canceled += instance.OnVoiceButton;
				Emote1.started += instance.OnEmote1;
				Emote1.performed += instance.OnEmote1;
				Emote1.canceled += instance.OnEmote1;
				Emote2.started += instance.OnEmote2;
				Emote2.performed += instance.OnEmote2;
				Emote2.canceled += instance.OnEmote2;
				BuildMode.started += instance.OnBuildMode;
				BuildMode.performed += instance.OnBuildMode;
				BuildMode.canceled += instance.OnBuildMode;
				ConfirmBuildMode.started += instance.OnConfirmBuildMode;
				ConfirmBuildMode.performed += instance.OnConfirmBuildMode;
				ConfirmBuildMode.canceled += instance.OnConfirmBuildMode;
				Delete.started += instance.OnDelete;
				Delete.performed += instance.OnDelete;
				Delete.canceled += instance.OnDelete;
			}
		}

		private void UnregisterCallbacks(IMovementActions instance)
		{
			Look.started -= instance.OnLook;
			Look.performed -= instance.OnLook;
			Look.canceled -= instance.OnLook;
			Move.started -= instance.OnMove;
			Move.performed -= instance.OnMove;
			Move.canceled -= instance.OnMove;
			Jump.started -= instance.OnJump;
			Jump.performed -= instance.OnJump;
			Jump.canceled -= instance.OnJump;
			Sprint.started -= instance.OnSprint;
			Sprint.performed -= instance.OnSprint;
			Sprint.canceled -= instance.OnSprint;
			OpenMenu.started -= instance.OnOpenMenu;
			OpenMenu.performed -= instance.OnOpenMenu;
			OpenMenu.canceled -= instance.OnOpenMenu;
			Interact.started -= instance.OnInteract;
			Interact.performed -= instance.OnInteract;
			Interact.canceled -= instance.OnInteract;
			Crouch.started -= instance.OnCrouch;
			Crouch.performed -= instance.OnCrouch;
			Crouch.canceled -= instance.OnCrouch;
			Use.started -= instance.OnUse;
			Use.performed -= instance.OnUse;
			Use.canceled -= instance.OnUse;
			ActivateItem.started -= instance.OnActivateItem;
			ActivateItem.performed -= instance.OnActivateItem;
			ActivateItem.canceled -= instance.OnActivateItem;
			Discard.started -= instance.OnDiscard;
			Discard.performed -= instance.OnDiscard;
			Discard.canceled -= instance.OnDiscard;
			SwitchItem.started -= instance.OnSwitchItem;
			SwitchItem.performed -= instance.OnSwitchItem;
			SwitchItem.canceled -= instance.OnSwitchItem;
			QEItemInteract.started -= instance.OnQEItemInteract;
			QEItemInteract.performed -= instance.OnQEItemInteract;
			QEItemInteract.canceled -= instance.OnQEItemInteract;
			EnableChat.started -= instance.OnEnableChat;
			EnableChat.performed -= instance.OnEnableChat;
			EnableChat.canceled -= instance.OnEnableChat;
			SubmitChat.started -= instance.OnSubmitChat;
			SubmitChat.performed -= instance.OnSubmitChat;
			SubmitChat.canceled -= instance.OnSubmitChat;
			ReloadBatteries.started -= instance.OnReloadBatteries;
			ReloadBatteries.performed -= instance.OnReloadBatteries;
			ReloadBatteries.canceled -= instance.OnReloadBatteries;
			SetFreeCamera.started -= instance.OnSetFreeCamera;
			SetFreeCamera.performed -= instance.OnSetFreeCamera;
			SetFreeCamera.canceled -= instance.OnSetFreeCamera;
			InspectItem.started -= instance.OnInspectItem;
			InspectItem.performed -= instance.OnInspectItem;
			InspectItem.canceled -= instance.OnInspectItem;
			SpeedCheat.started -= instance.OnSpeedCheat;
			SpeedCheat.performed -= instance.OnSpeedCheat;
			SpeedCheat.canceled -= instance.OnSpeedCheat;
			PingScan.started -= instance.OnPingScan;
			PingScan.performed -= instance.OnPingScan;
			PingScan.canceled -= instance.OnPingScan;
			VoiceButton.started -= instance.OnVoiceButton;
			VoiceButton.performed -= instance.OnVoiceButton;
			VoiceButton.canceled -= instance.OnVoiceButton;
			Emote1.started -= instance.OnEmote1;
			Emote1.performed -= instance.OnEmote1;
			Emote1.canceled -= instance.OnEmote1;
			Emote2.started -= instance.OnEmote2;
			Emote2.performed -= instance.OnEmote2;
			Emote2.canceled -= instance.OnEmote2;
			BuildMode.started -= instance.OnBuildMode;
			BuildMode.performed -= instance.OnBuildMode;
			BuildMode.canceled -= instance.OnBuildMode;
			ConfirmBuildMode.started -= instance.OnConfirmBuildMode;
			ConfirmBuildMode.performed -= instance.OnConfirmBuildMode;
			ConfirmBuildMode.canceled -= instance.OnConfirmBuildMode;
			Delete.started -= instance.OnDelete;
			Delete.performed -= instance.OnDelete;
			Delete.canceled -= instance.OnDelete;
		}

		public void RemoveCallbacks(IMovementActions instance)
		{
			if (m_Wrapper.m_MovementActionsCallbackInterfaces.Remove(instance))
			{
				UnregisterCallbacks(instance);
			}
		}

		public void SetCallbacks(IMovementActions instance)
		{
			foreach (IMovementActions movementActionsCallbackInterface in m_Wrapper.m_MovementActionsCallbackInterfaces)
			{
				UnregisterCallbacks(movementActionsCallbackInterface);
			}
			m_Wrapper.m_MovementActionsCallbackInterfaces.Clear();
			AddCallbacks(instance);
		}
	}

	public interface IMovementActions
	{
		void OnLook(InputAction.CallbackContext context);

		void OnMove(InputAction.CallbackContext context);

		void OnJump(InputAction.CallbackContext context);

		void OnSprint(InputAction.CallbackContext context);

		void OnOpenMenu(InputAction.CallbackContext context);

		void OnInteract(InputAction.CallbackContext context);

		void OnCrouch(InputAction.CallbackContext context);

		void OnUse(InputAction.CallbackContext context);

		void OnActivateItem(InputAction.CallbackContext context);

		void OnDiscard(InputAction.CallbackContext context);

		void OnSwitchItem(InputAction.CallbackContext context);

		void OnQEItemInteract(InputAction.CallbackContext context);

		void OnEnableChat(InputAction.CallbackContext context);

		void OnSubmitChat(InputAction.CallbackContext context);

		void OnReloadBatteries(InputAction.CallbackContext context);

		void OnSetFreeCamera(InputAction.CallbackContext context);

		void OnInspectItem(InputAction.CallbackContext context);

		void OnSpeedCheat(InputAction.CallbackContext context);

		void OnPingScan(InputAction.CallbackContext context);

		void OnVoiceButton(InputAction.CallbackContext context);

		void OnEmote1(InputAction.CallbackContext context);

		void OnEmote2(InputAction.CallbackContext context);

		void OnBuildMode(InputAction.CallbackContext context);

		void OnConfirmBuildMode(InputAction.CallbackContext context);

		void OnDelete(InputAction.CallbackContext context);
	}

	private readonly InputActionMap m_Movement;

	private List<IMovementActions> m_MovementActionsCallbackInterfaces = new List<IMovementActions>();

	private readonly InputAction m_Movement_Look;

	private readonly InputAction m_Movement_Move;

	private readonly InputAction m_Movement_Jump;

	private readonly InputAction m_Movement_Sprint;

	private readonly InputAction m_Movement_OpenMenu;

	private readonly InputAction m_Movement_Interact;

	private readonly InputAction m_Movement_Crouch;

	private readonly InputAction m_Movement_Use;

	private readonly InputAction m_Movement_ActivateItem;

	private readonly InputAction m_Movement_Discard;

	private readonly InputAction m_Movement_SwitchItem;

	private readonly InputAction m_Movement_QEItemInteract;

	private readonly InputAction m_Movement_EnableChat;

	private readonly InputAction m_Movement_SubmitChat;

	private readonly InputAction m_Movement_ReloadBatteries;

	private readonly InputAction m_Movement_SetFreeCamera;

	private readonly InputAction m_Movement_InspectItem;

	private readonly InputAction m_Movement_SpeedCheat;

	private readonly InputAction m_Movement_PingScan;

	private readonly InputAction m_Movement_VoiceButton;

	private readonly InputAction m_Movement_Emote1;

	private readonly InputAction m_Movement_Emote2;

	private readonly InputAction m_Movement_BuildMode;

	private readonly InputAction m_Movement_ConfirmBuildMode;

	private readonly InputAction m_Movement_Delete;

	public InputActionAsset asset { get; }

	public InputBinding? bindingMask
	{
		get
		{
			return asset.bindingMask;
		}
		set
		{
			asset.bindingMask = value;
		}
	}

	public ReadOnlyArray<InputDevice>? devices
	{
		get
		{
			return asset.devices;
		}
		set
		{
			asset.devices = value;
		}
	}

	public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

	public IEnumerable<InputBinding> bindings => asset.bindings;

	public MovementActions Movement => new MovementActions(this);

	public PlayerActions()
	{
		asset = InputActionAsset.FromJson("{\n    \"name\": \"PlayerActions\",\n    \"maps\": [\n        {\n            \"name\": \"Movement\",\n            \"id\": \"1560e87b-23aa-4005-bf8b-264f6a3c3736\",\n            \"actions\": [\n                {\n                    \"name\": \"Look\",\n                    \"type\": \"Value\",\n                    \"id\": \"c63a6ade-6c5a-4659-9aa5-e336e7b9970f\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"AxisDeadzone(max=1)\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Move\",\n                    \"type\": \"Value\",\n                    \"id\": \"1af759ec-380d-4f9b-9108-c4e024e17c3e\",\n                    \"expectedControlType\": \"Vector2\",\n                    \"processors\": \"AxisDeadzone(min=0.3,max=1)\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Jump\",\n                    \"type\": \"Button\",\n                    \"id\": \"29820219-83ac-41cb-9f43-9ba2bcb7882c\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Sprint\",\n                    \"type\": \"Value\",\n                    \"id\": \"38a90280-ca06-4012-853a-06cd9bf6cda3\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"OpenMenu\",\n                    \"type\": \"Button\",\n                    \"id\": \"61f99167-dec0-46cb-a700-a21e900ddbe6\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Interact\",\n                    \"type\": \"Value\",\n                    \"id\": \"7dc7e4c4-a4eb-449d-a885-cf7ad4b8faaa\",\n                    \"expectedControlType\": \"\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Crouch\",\n                    \"type\": \"Button\",\n                    \"id\": \"a5e81f24-9799-4b3e-b009-386c60e18cc1\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Use\",\n                    \"type\": \"Button\",\n                    \"id\": \"afa10779-50c6-45ee-828e-2c782fd48921\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"ActivateItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"990dbbff-3266-4890-8b7d-da5d76679e09\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Discard\",\n                    \"type\": \"Button\",\n                    \"id\": \"a4608dd4-03c1-4f59-94e2-84a333a9981b\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SwitchItem\",\n                    \"type\": \"Value\",\n                    \"id\": \"c4f37a56-2df5-447b-8d60-98946d41bfe8\",\n                    \"expectedControlType\": \"Axis\",\n                    \"processors\": \"AxisDeadzone\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"QEItemInteract\",\n                    \"type\": \"Value\",\n                    \"id\": \"e1790f23-249a-40a3-b51b-892fd6eb78d4\",\n                    \"expectedControlType\": \"Axis\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"EnableChat\",\n                    \"type\": \"Button\",\n                    \"id\": \"58e1c009-b16f-4d4d-a0ee-1d2922c4a10f\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SubmitChat\",\n                    \"type\": \"Button\",\n                    \"id\": \"52ce15c0-45ed-4b05-98ed-04db00b51b35\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ReloadBatteries\",\n                    \"type\": \"Button\",\n                    \"id\": \"2f6bf1bd-1a9d-42de-bab3-345b343c4010\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SetFreeCamera\",\n                    \"type\": \"Button\",\n                    \"id\": \"f783bb29-6cc7-46ae-b08c-b5ec213df236\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"InspectItem\",\n                    \"type\": \"Button\",\n                    \"id\": \"d22b85a2-31df-40ce-b54f-ef936324a412\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"SpeedCheat\",\n                    \"type\": \"Button\",\n                    \"id\": \"1f917a99-4119-46b5-9cd3-a306bd7f7d4a\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"PingScan\",\n                    \"type\": \"Button\",\n                    \"id\": \"10a87310-b590-4c4a-bb17-c6e801480dee\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"VoiceButton\",\n                    \"type\": \"Value\",\n                    \"id\": \"c0b6b3e8-4fe6-46b7-896a-0e8e1f39bcff\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": true\n                },\n                {\n                    \"name\": \"Emote1\",\n                    \"type\": \"Button\",\n                    \"id\": \"c6fba331-7cf2-4fd9-a214-f95c9182cb92\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Emote2\",\n                    \"type\": \"Button\",\n                    \"id\": \"02446a15-cc51-421a-8cde-feca90b28c42\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"BuildMode\",\n                    \"type\": \"Button\",\n                    \"id\": \"31bb1483-6a93-4220-9542-6483a33469bd\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"ConfirmBuildMode\",\n                    \"type\": \"Button\",\n                    \"id\": \"995cf773-e209-4596-b873-0ef4652542f1\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                },\n                {\n                    \"name\": \"Delete\",\n                    \"type\": \"Button\",\n                    \"id\": \"c3fbd8b5-4e95-4a60-ae3a-907c2af57784\",\n                    \"expectedControlType\": \"Button\",\n                    \"processors\": \"\",\n                    \"interactions\": \"\",\n                    \"initialStateCheck\": false\n                }\n            ],\n            \"bindings\": [\n                {\n                    \"name\": \"\",\n                    \"id\": \"7aea34a9-040b-4a60-b98a-ef8cb75e8ccf\",\n                    \"path\": \"<Mouse>/delta\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Look\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"171607bb-692f-4770-9f45-05b1456f6ce0\",\n                    \"path\": \"<Gamepad>/rightStick\",\n                    \"interactions\": \"\",\n                    \"processors\": \"ScaleVector2(x=70,y=70)\",\n                    \"groups\": \"\",\n                    \"action\": \"Look\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"Keyboard\",\n                    \"id\": \"efe1ca7a-482c-4dfb-b80b-a34166b2cc7d\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"ae3df94a-dcc6-4177-b026-0938f8413a45\",\n                    \"path\": \"<Keyboard>/w\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"bc252037-120e-4c64-9671-40d365f856b3\",\n                    \"path\": \"<Keyboard>/s\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"ad96b1ce-c0f3-4913-be03-acf077c11064\",\n                    \"path\": \"<Keyboard>/a\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"756f3db2-a6e6-42d7-9580-70d42154cd11\",\n                    \"path\": \"<Keyboard>/d\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"KeyboardArrows\",\n                    \"id\": \"0014756c-e66d-4001-812b-4ad824368820\",\n                    \"path\": \"2DVector\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"up\",\n                    \"id\": \"7a85dbed-b531-4a68-95ef-3011cf4b09c3\",\n                    \"path\": \"<Keyboard>/upArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"down\",\n                    \"id\": \"0fbf076d-87d6-45c2-b11a-3782889fca60\",\n                    \"path\": \"<Keyboard>/downArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"left\",\n                    \"id\": \"942d42b0-058f-4053-b6ba-3e157c0675f3\",\n                    \"path\": \"<Keyboard>/leftArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"right\",\n                    \"id\": \"a519a21b-a924-4ce6-9b55-e28ef0d9d488\",\n                    \"path\": \"<Keyboard>/rightArrow\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a0d6c3d4-6041-4492-bbb3-c0b84914ffc9\",\n                    \"path\": \"<Gamepad>/leftStick\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Move\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"7b616c75-7032-463c-b9d3-72884bcded84\",\n                    \"path\": \"<Gamepad>/buttonSouth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Jump\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"29d0e539-abdc-46ae-8dff-27436261f379\",\n                    \"path\": \"<Keyboard>/space\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Jump\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c243b846-2159-41a2-87d5-5a36f89e70da\",\n                    \"path\": \"<Keyboard>/shift\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Sprint\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ea358e03-6f7e-4054-860f-ded39d00cc30\",\n                    \"path\": \"<Gamepad>/leftTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Sprint\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"00fb2c2d-6f7a-498b-91a9-42c3ead2d5f3\",\n                    \"path\": \"<Keyboard>/escape\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"OpenMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b73f442d-bbd4-45ad-87d5-34cba090fa9b\",\n                    \"path\": \"<Gamepad>/start\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"OpenMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3d52b957-c79a-4d3f-b84c-a0f21cf4c089\",\n                    \"path\": \"<Keyboard>/tab\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"OpenMenu\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"155d449f-a2c5-43ab-9244-955fec5e0497\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a7e5a03c-3ab7-45be-970a-a9c2963502fc\",\n                    \"path\": \"<Gamepad>/buttonWest\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Interact\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e3c54809-7fb3-4bc6-9aed-40a36b018c06\",\n                    \"path\": \"<Gamepad>/rightStickPress\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Crouch\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"3bfe566d-e645-4254-8fd2-2c98358dbd17\",\n                    \"path\": \"<Keyboard>/ctrl\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Crouch\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9e5d8c19-ecee-48ed-ac7f-039528c22031\",\n                    \"path\": \"<Mouse>/leftButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Use\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"73118618-851a-4995-8aae-3848f6f81ab5\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Use\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"19548cb7-6c57-4972-a4e3-28a4e7e71595\",\n                    \"path\": \"<Mouse>/leftButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"ActivateItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"15a2f703-29f3-46b5-a1d0-508a159aad30\",\n                    \"path\": \"<Gamepad>/rightTrigger\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"ActivateItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"c3c0f7fa-24e9-49bc-9e71-0405fdd4cf02\",\n                    \"path\": \"<Keyboard>/g\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Discard\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"99c75319-4397-4e28-9316-fedfd1c63c1c\",\n                    \"path\": \"<Gamepad>/buttonEast\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Discard\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b4d55417-49f4-42ba-ac02-9af841a003e8\",\n                    \"path\": \"<Mouse>/scroll/y\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SwitchItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"ec3eb10a-1604-426f-8251-42be798f3c05\",\n                    \"path\": \"<Gamepad>/dpad/x\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SwitchItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"1D Axis\",\n                    \"id\": \"a7b0da01-86be-4854-b134-176d96ab1571\",\n                    \"path\": \"1DAxis\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"QEItemInteract\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"negative\",\n                    \"id\": \"c62b6b31-53c1-43f9-92c5-07f7217c585e\",\n                    \"path\": \"<Keyboard>/q\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"QEItemInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"positive\",\n                    \"id\": \"8f47af30-c4b4-4448-bd39-d9aba47a3188\",\n                    \"path\": \"<Keyboard>/e\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"QEItemInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"1D Axis\",\n                    \"id\": \"a307f844-0fa2-4111-b60a-afd91de8e536\",\n                    \"path\": \"1DAxis\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"QEItemInteract\",\n                    \"isComposite\": true,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"negative\",\n                    \"id\": \"7b008b4a-fdb1-4603-becb-6b90ee9e345c\",\n                    \"path\": \"<Gamepad>/dpad/down\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"QEItemInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"positive\",\n                    \"id\": \"86e38089-e357-45c5-a39d-7d66cdcbf803\",\n                    \"path\": \"<Gamepad>/dpad/up\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"QEItemInteract\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": true\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e8ff5cf5-ee6d-4e54-a800-27c16bf307fc\",\n                    \"path\": \"<Keyboard>/slash\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"EnableChat\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e6fe9b9c-ff67-4d42-83b9-979bfb1623df\",\n                    \"path\": \"<Keyboard>/enter\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SubmitChat\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"91068d88-dacd-4f4e-8d78-b360b12c8f99\",\n                    \"path\": \"<Keyboard>/r\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"ReloadBatteries\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"48a42f6b-7cb1-4f49-bc47-03628a21a652\",\n                    \"path\": \"<Keyboard>/c\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SetFreeCamera\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9a7b1df9-3829-42d5-967d-bf7cb202d823\",\n                    \"path\": \"<Gamepad>/select\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SetFreeCamera\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e2f85d45-4161-43dd-9792-9655d4ffb7fe\",\n                    \"path\": \"<Keyboard>/z\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"InspectItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e81e7228-432d-4b23-b2bc-c4adcce2f830\",\n                    \"path\": \"<Gamepad>/leftShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"InspectItem\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"351e9484-e505-4183-ba21-c666bd64484e\",\n                    \"path\": \"<Keyboard>/h\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"SpeedCheat\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"f41287a6-fe38-4620-a1ec-b4d871a72d17\",\n                    \"path\": \"<Mouse>/rightButton\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"PingScan\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"82b7c0d0-3d7c-4b80-acf2-17e66cf5b3ac\",\n                    \"path\": \"<Gamepad>/rightShoulder\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"PingScan\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"654df1e4-2b33-44e0-afdb-b57dcce462a5\",\n                    \"path\": \"<Keyboard>/t\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"VoiceButton\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"8c5fe1cb-4346-4fe7-a829-2f6af2459ed2\",\n                    \"path\": \"<Keyboard>/1\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Emote1\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"84dee523-67bb-4220-bf80-3d770065c31b\",\n                    \"path\": \"<Keyboard>/2\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Emote2\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"b64c453d-9f9e-462f-9227-51307bcab6ed\",\n                    \"path\": \"<Keyboard>/b\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"BuildMode\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"a7abfd96-3670-4488-80ec-1f1d13b99cfc\",\n                    \"path\": \"<Gamepad>/buttonNorth\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"BuildMode\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"9373b4dc-553a-4526-89c9-f36926194964\",\n                    \"path\": \"<Keyboard>/v\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"ConfirmBuildMode\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                },\n                {\n                    \"name\": \"\",\n                    \"id\": \"e2f369bc-1ae3-4dcb-837e-e6a2e7c37e5e\",\n                    \"path\": \"<Keyboard>/x\",\n                    \"interactions\": \"\",\n                    \"processors\": \"\",\n                    \"groups\": \"\",\n                    \"action\": \"Delete\",\n                    \"isComposite\": false,\n                    \"isPartOfComposite\": false\n                }\n            ]\n        }\n    ],\n    \"controlSchemes\": []\n}");
		m_Movement = asset.FindActionMap("Movement", throwIfNotFound: true);
		m_Movement_Look = m_Movement.FindAction("Look", throwIfNotFound: true);
		m_Movement_Move = m_Movement.FindAction("Move", throwIfNotFound: true);
		m_Movement_Jump = m_Movement.FindAction("Jump", throwIfNotFound: true);
		m_Movement_Sprint = m_Movement.FindAction("Sprint", throwIfNotFound: true);
		m_Movement_OpenMenu = m_Movement.FindAction("OpenMenu", throwIfNotFound: true);
		m_Movement_Interact = m_Movement.FindAction("Interact", throwIfNotFound: true);
		m_Movement_Crouch = m_Movement.FindAction("Crouch", throwIfNotFound: true);
		m_Movement_Use = m_Movement.FindAction("Use", throwIfNotFound: true);
		m_Movement_ActivateItem = m_Movement.FindAction("ActivateItem", throwIfNotFound: true);
		m_Movement_Discard = m_Movement.FindAction("Discard", throwIfNotFound: true);
		m_Movement_SwitchItem = m_Movement.FindAction("SwitchItem", throwIfNotFound: true);
		m_Movement_QEItemInteract = m_Movement.FindAction("QEItemInteract", throwIfNotFound: true);
		m_Movement_EnableChat = m_Movement.FindAction("EnableChat", throwIfNotFound: true);
		m_Movement_SubmitChat = m_Movement.FindAction("SubmitChat", throwIfNotFound: true);
		m_Movement_ReloadBatteries = m_Movement.FindAction("ReloadBatteries", throwIfNotFound: true);
		m_Movement_SetFreeCamera = m_Movement.FindAction("SetFreeCamera", throwIfNotFound: true);
		m_Movement_InspectItem = m_Movement.FindAction("InspectItem", throwIfNotFound: true);
		m_Movement_SpeedCheat = m_Movement.FindAction("SpeedCheat", throwIfNotFound: true);
		m_Movement_PingScan = m_Movement.FindAction("PingScan", throwIfNotFound: true);
		m_Movement_VoiceButton = m_Movement.FindAction("VoiceButton", throwIfNotFound: true);
		m_Movement_Emote1 = m_Movement.FindAction("Emote1", throwIfNotFound: true);
		m_Movement_Emote2 = m_Movement.FindAction("Emote2", throwIfNotFound: true);
		m_Movement_BuildMode = m_Movement.FindAction("BuildMode", throwIfNotFound: true);
		m_Movement_ConfirmBuildMode = m_Movement.FindAction("ConfirmBuildMode", throwIfNotFound: true);
		m_Movement_Delete = m_Movement.FindAction("Delete", throwIfNotFound: true);
	}

	public void Dispose()
	{
		UnityEngine.Object.Destroy(asset);
	}

	public bool Contains(InputAction action)
	{
		return asset.Contains(action);
	}

	public IEnumerator<InputAction> GetEnumerator()
	{
		return asset.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Enable()
	{
		asset.Enable();
	}

	public void Disable()
	{
		asset.Disable();
	}

	public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
	{
		return asset.FindAction(actionNameOrId, throwIfNotFound);
	}

	public int FindBinding(InputBinding bindingMask, out InputAction action)
	{
		return asset.FindBinding(bindingMask, out action);
	}
}
