using System;
using UnityEngine.InputSystem;

[Serializable]
public class RemappableKey
{
	public string ControlName;

	public InputActionReference currentInput;

	public int rebindingIndex = -1;

	public bool gamepadOnly;
}
