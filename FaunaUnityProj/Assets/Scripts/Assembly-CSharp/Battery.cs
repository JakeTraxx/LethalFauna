using System;

[Serializable]
public class Battery
{
	public bool empty;

	public float charge = 1f;

	public Battery(bool isEmpty, float chargeNumber)
	{
		empty = isEmpty;
		charge = chargeNumber;
	}
}
