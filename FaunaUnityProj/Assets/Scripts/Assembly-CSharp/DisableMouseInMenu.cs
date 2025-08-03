using UnityEngine;
using UnityEngine.InputSystem;

public class DisableMouseInMenu : MonoBehaviour
{
	public PlayerActions actions;

	private void Awake()
	{
		actions = new PlayerActions();
	}

	private void OnEnable()
	{
		actions.Movement.Move.performed += Move_performed;
		actions.Movement.Look.performed += Look_performed;
		actions.Enable();
	}

	private void OnDisable()
	{
		actions.Movement.Move.performed -= Move_performed;
		actions.Movement.Look.performed -= Look_performed;
		actions.Disable();
	}

	private void Look_performed(InputAction.CallbackContext context)
	{
		Cursor.visible = InputControlPath.MatchesPrefix("<Mouse>", context.control);
	}

	private void Move_performed(InputAction.CallbackContext context)
	{
		Cursor.visible = InputControlPath.MatchesPrefix("<Mouse>", context.control);
	}
}
