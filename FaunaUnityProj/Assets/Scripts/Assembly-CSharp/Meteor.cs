using System;
using UnityEngine;

[Serializable]
public class Meteor
{
	public Vector3 landingPosition;

	public Vector3 skyDirection;

	public Vector3 positionAtStartOfLandingPhase;

	public float landingTimer;

	public bool landed;

	public float normalizedLandingTime;

	public float scale;

	public int phase;

	public int previousPhase;

	public GameObject meteorObject;

	public ParticleSystem fireParticle;

	public ParticleSystem fireParticle2;

	public GameObject distantSprite;
}
