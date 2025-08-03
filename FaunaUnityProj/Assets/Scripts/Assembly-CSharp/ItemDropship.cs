using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemDropship : NetworkBehaviour
{
	public bool deliveringOrder;

	public bool shipLanded;

	public bool shipDoorsOpened;

	public Animator shipAnimator;

	public float shipTimer;

	public bool playersFirstOrder = true;

	private StartOfRound playersManager;

	private Terminal terminalScript;

	private List<int> itemsToDeliver = new List<int>();

	public Transform[] itemSpawnPositions;

	private float noiseInterval;

	private int timesPlayedWithoutTurningOff;

	public InteractTrigger triggerScript;

	public LineRenderer[] ropes;

	public Transform[] ropeDestinations;

	public Transform deliverVehiclePoint;

	public bool deliveringVehicle;

	public bool untetheredVehicle;

	private void Start()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
		terminalScript = Object.FindObjectOfType<Terminal>();
	}

	public void UntetherVehicle()
	{
		if (base.IsServer)
		{
			untetheredVehicle = true;
			shipTimer = 0f;
			UntetherVehicleServerRpc();
		}
	}

	[ServerRpc]
	public void UntetherVehicleServerRpc()
{		{
			UntetherVehicleClientRpc();
		}
}
	[ClientRpc]
	public void UntetherVehicleClientRpc()
			{
				shipTimer = 0f;
				untetheredVehicle = true;
			}

	private void FinishDeliveringVehicleOnServer()
	{
		if (base.IsServer)
		{
			shipTimer = 0f;
			deliveringOrder = false;
			deliveringVehicle = false;
			FinishDeliveringVehicleServerRpc();
		}
	}

	[ServerRpc]
	public void FinishDeliveringVehicleServerRpc()
{		{
			FinishDeliveringVehicleClientRpc();
		}
}
	[ClientRpc]
	public void FinishDeliveringVehicleClientRpc()
{if(!base.IsServer)			{
				shipTimer = 0f;
				deliveringOrder = false;
				deliveringVehicle = false;
			}
}
	private void Update()
	{
		if (ropes[0].gameObject.activeInHierarchy)
		{
			Debug.Log("Setting position of ropes");
			for (int i = 0; i < ropes.Length; i++)
			{
				ropes[i].SetPosition(1, ropes[i].transform.InverseTransformPoint(ropeDestinations[i].position));
			}
		}
		if (!base.IsServer)
		{
			return;
		}
		if (!deliveringOrder)
		{
			if (terminalScript.orderedItemsFromTerminal.Count <= 0 && terminalScript.orderedVehicleFromTerminal == -1)
			{
				return;
			}
			if (playersManager.shipHasLanded)
			{
				shipTimer += Time.deltaTime;
			}
			if (playersFirstOrder)
			{
				playersFirstOrder = false;
				shipTimer = 20f;
			}
			if (shipTimer > 40f)
			{
				if (terminalScript.orderedVehicleFromTerminal != -1)
				{
					DeliverVehicleOnServer();
				}
				else
				{
					LandShipOnServer();
				}
			}
			return;
		}
		if (deliveringVehicle)
		{
			if (untetheredVehicle)
			{
				shipTimer += Time.deltaTime;
				if (shipTimer > 6f)
				{
					FinishDeliveringVehicleOnServer();
				}
			}
			else
			{
				shipTimer += Time.deltaTime;
				if (shipTimer > 6f)
				{
					UntetherVehicle();
				}
			}
		}
		if (shipLanded)
		{
			shipTimer += Time.deltaTime;
			if (shipTimer > 30f)
			{
				timesPlayedWithoutTurningOff = 0;
				shipLanded = false;
				ShipLeaveClientRpc();
			}
			if (noiseInterval <= 0f)
			{
				noiseInterval = 1f;
				timesPlayedWithoutTurningOff++;
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 60f, 1.3f, timesPlayedWithoutTurningOff, noiseIsInsideClosedShip: false, 94);
			}
			else
			{
				noiseInterval -= Time.deltaTime;
			}
		}
	}

	public void TryOpeningShip()
	{
		if (!shipDoorsOpened)
		{
			if (base.IsServer)
			{
				OpenShipDoorsOnServer();
			}
			else
			{
				OpenShipServerRpc();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenShipServerRpc()
			{
				OpenShipDoorsOnServer();
			}

	private void OpenShipDoorsOnServer()
	{
		if (shipLanded && !shipDoorsOpened)
		{
			int num = 0;
			for (int i = 0; i < itemsToDeliver.Count; i++)
			{
				GameObject gameObject = Object.Instantiate(terminalScript.buyableItemsList[itemsToDeliver[i]].spawnPrefab, itemSpawnPositions[num].position, Quaternion.identity, playersManager.propsContainer);
				gameObject.GetComponent<GrabbableObject>().fallTime = 0f;
				gameObject.GetComponent<NetworkObject>().Spawn();
				gameObject.GetComponent<NetworkObject>().TrySetParent(gameObject.transform.parent);
				num = ((num < 3) ? (num + 1) : 0);
			}
			itemsToDeliver.Clear();
			OpenShipClientRpc();
		}
	}

	[ClientRpc]
	public void OpenShipClientRpc()
			{
				shipAnimator.SetBool("doorsOpened", value: true);
				shipDoorsOpened = true;
				triggerScript.interactable = false;
			}

	public void ShipLandedAnimationEvent()
	{
		shipLanded = true;
	}

	private void DeliverVehicleOnServer()
	{
		shipTimer = 0f;
		Object.Instantiate(terminalScript.buyableVehicles[terminalScript.orderedVehicleFromTerminal].vehiclePrefab, RoundManager.Instance.VehiclesContainer).GetComponent<NetworkObject>().Spawn();
		if (terminalScript.buyableVehicles[terminalScript.orderedVehicleFromTerminal].secondaryPrefab != null)
		{
			Object.Instantiate(terminalScript.buyableVehicles[terminalScript.orderedVehicleFromTerminal].secondaryPrefab, RoundManager.Instance.VehiclesContainer).GetComponent<NetworkObject>().Spawn();
		}
		terminalScript.orderedVehicleFromTerminal = -1;
		terminalScript.vehicleInDropship = false;
		untetheredVehicle = false;
		deliveringVehicle = true;
		deliveringOrder = true;
		DeliverVehicleClientRpc();
	}

	[ClientRpc]
	public void DeliverVehicleClientRpc()
			{
				terminalScript.orderedVehicleFromTerminal = -1;
				terminalScript.vehicleInDropship = false;
				deliveringVehicle = true;
				shipAnimator.SetTrigger("landingVehicle");
				triggerScript.interactable = false;
			}

	private void LandShipOnServer()
	{
		shipTimer = 0f;
		itemsToDeliver.Clear();
		itemsToDeliver.AddRange(terminalScript.orderedItemsFromTerminal);
		terminalScript.orderedItemsFromTerminal.Clear();
		playersFirstOrder = false;
		deliveringOrder = true;
		LandShipClientRpc();
	}

	[ClientRpc]
	public void LandShipClientRpc()
			{
				Object.FindObjectOfType<Terminal>().numberOfItemsInDropship = 0;
				shipAnimator.SetBool("landing", value: true);
				triggerScript.interactable = true;
			}

	[ClientRpc]
	public void ShipLeaveClientRpc()
			{
				ShipLeave();
			}

	public void ShipLeave()
	{
		shipDoorsOpened = false;
		shipAnimator.SetBool("doorsOpened", value: false);
		shipLanded = false;
		shipAnimator.SetBool("landing", value: false);
		deliveringOrder = false;
		if (itemsToDeliver.Count > 0)
		{
			HUDManager.Instance.DisplayTip("Items missed!", "The vehicle returned with your purchased items. Our delivery fee cannot be refunded.");
		}
		Object.FindObjectOfType<Terminal>().numberOfItemsInDropship = 0;
		itemsToDeliver.Clear();
	}

	public void ShipLandedInAnimation()
	{
		shipLanded = true;
	}
}
