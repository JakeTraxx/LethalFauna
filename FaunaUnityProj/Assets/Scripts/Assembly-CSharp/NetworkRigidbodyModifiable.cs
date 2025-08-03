using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class NetworkRigidbodyModifiable : NetworkBehaviour
{
	private Rigidbody m_Rigidbody;

	private NetworkTransform m_NetworkTransform;

	private bool m_OriginalKinematic;

	public bool kinematicOnOwner;

	public bool nonKinematicWhenDropping;

	private RigidbodyInterpolation m_OriginalInterpolation;

	private bool m_IsAuthority;

	private bool HasAuthority => m_NetworkTransform.CanCommitToTransform;

	private void Awake()
	{
		m_Rigidbody = GetComponent<Rigidbody>();
		m_NetworkTransform = GetComponent<NetworkTransform>();
	}

	private void FixedUpdate()
	{
		if (base.NetworkManager.IsListening && HasAuthority != m_IsAuthority)
		{
			m_IsAuthority = HasAuthority;
			UpdateRigidbodyKinematicMode();
		}
	}

	public void UpdateRigidbodyKinematicMode()
	{
		if (!m_IsAuthority)
		{
			m_OriginalKinematic = m_Rigidbody.isKinematic;
			m_Rigidbody.isKinematic = true;
			m_OriginalInterpolation = m_Rigidbody.interpolation;
			m_Rigidbody.interpolation = RigidbodyInterpolation.None;
			return;
		}
		if (kinematicOnOwner)
		{
			m_Rigidbody.isKinematic = true;
		}
		else if (nonKinematicWhenDropping)
		{
			m_Rigidbody.isKinematic = false;
			nonKinematicWhenDropping = false;
		}
		else
		{
			m_Rigidbody.isKinematic = m_OriginalKinematic;
		}
		m_Rigidbody.interpolation = m_OriginalInterpolation;
	}

	public override void OnNetworkSpawn()
	{
		m_IsAuthority = HasAuthority;
		m_OriginalKinematic = m_Rigidbody.isKinematic;
		m_OriginalInterpolation = m_Rigidbody.interpolation;
		UpdateRigidbodyKinematicMode();
	}

	public override void OnNetworkDespawn()
	{
		UpdateRigidbodyKinematicMode();
	}
}
