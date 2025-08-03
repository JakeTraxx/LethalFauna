using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MeteorShowers : NetworkBehaviour
{
	public List<Meteor> meteors = new List<Meteor>();

	public System.Random meteorRandom;

	public AnimationCurve numberOfMeteorsCurve;

	public AnimationCurve meteorSizeVarianceCurve;

	public AnimationCurve landingTimeCurve;

	public float baseMeteorSize;

	public float skyAngularTilt = 0.5f;

	public float baseWarningTime = 0.15f;

	public float heightInSky = 800f;

	public float landingSpeed = 1.5f;

	public GameObject meteorPrefab;

	public GameObject distantSpritePrefab;

	public GameObject meteorLandingExplosion;

	private Ray skyRay;

	private Vector3 dest;

	public bool meteorsEnabled;

	public AudioClip insideFactoryMeteorClip;

	public void SetStartMeteorShower()
	{
		Debug.Log($"Enable meteor!; {base.IsServer}");
		if (base.IsServer)
		{
			if (TimeOfDay.Instance.normalizedTimeOfDay > 0.8f)
			{
				base.gameObject.SetActive(value: false);
				Debug.Log($"failed start meteor shower; Normalized time of day: {TimeOfDay.Instance.normalizedTimeOfDay}");
			}
			else
			{
				BeginDay(TimeOfDay.Instance.normalizedTimeOfDay);
			}
		}
	}

	public void ResetMeteorWeather()
	{
		meteorsEnabled = false;
		for (int i = 0; i < meteors.Count; i++)
		{
			if (meteors[i].meteorObject != null)
			{
				UnityEngine.Object.Destroy(meteors[i].meteorObject);
			}
		}
		meteors.Clear();
	}

	[ServerRpc]
	public void CreateMeteorServerRpc(float landingTime, Vector3 landingPosition, Vector3 skyDirection, float size)
{		{
			CreateMeteorClientRpc(landingTime, landingPosition, skyDirection, size);
		}
}
	[ClientRpc]
	public void CreateMeteorClientRpc(float landingTime, Vector3 landingPosition, Vector3 skyDirection, float size)
{if(!base.IsServer && TimeOfDay.Instance.timeHasStarted)			{
				Meteor meteor = new Meteor();
				meteor.normalizedLandingTime = landingTime;
				meteor.scale = size;
				meteor.skyDirection = skyDirection;
				meteor.landingPosition = landingPosition;
				meteors.Add(meteor);
			}
}
	public void BeginDay(float timeOfDay)
	{
		if (!base.IsServer)
		{
			return;
		}
		meteorRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 12);
		meteors.Clear();
		float time = (float)meteorRandom.Next(0, 100) / 100f;
		int num = Mathf.RoundToInt(Mathf.Clamp(numberOfMeteorsCurve.Evaluate(time) * 200f, 160f, 250f) / Mathf.Clamp(timeOfDay * 2.5f, 1f, 10f));
		Debug.Log($"Meteors number: {num}; {Mathf.Clamp(numberOfMeteorsCurve.Evaluate(time) * 200f, 180f, 250f)}");
		float num2 = Mathf.Clamp(baseMeteorSize / ((float)num * 0.1f), 12f, baseMeteorSize);
		int num3 = 0;
		if (num2 < 18f)
		{
			num3 = meteorRandom.Next(1, 5);
		}
		Vector3 b = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, getOutsideEntrance: true);
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < RoundManager.Instance.outsideAINodes.Length; i++)
		{
			if (!(Vector3.Distance(RoundManager.Instance.outsideAINodes[i].transform.position, StartOfRound.Instance.shipLandingPosition.position) < num2 * 2f) && !(Vector3.Distance(RoundManager.Instance.outsideAINodes[i].transform.position, b) < num2 + 24f))
			{
				list.Add(RoundManager.Instance.outsideAINodes[i]);
			}
		}
		int minValue = 0;
		for (int j = 0; j < 50; j++)
		{
			if (landingTimeCurve.Evaluate((float)j / 50f) > timeOfDay + 0.07f)
			{
				minValue = j * 2;
				break;
			}
		}
		for (int k = 0; k < num; k++)
		{
			Meteor meteor = new Meteor();
			time = meteorSizeVarianceCurve.Evaluate((float)meteorRandom.Next(0, 100) / 100f);
			meteor.scale = Mathf.Clamp(num2 * time, num2 * 0.25f, num2 * 1.8f);
			meteor.landingPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(list[meteorRandom.Next(0, list.Count)].transform.position, 15f, default(NavMeshHit), meteorRandom, -273);
			if (Physics.Raycast(meteor.landingPosition + Vector3.up * 100f, -Vector3.up, out var hitInfo, 150f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				meteor.landingPosition = hitInfo.point;
			}
			meteor.skyDirection = Vector3.up * 10f + new Vector3((float)meteorRandom.Next(-10, 10) * skyAngularTilt, 0f, (float)meteorRandom.Next(-10, 10) * skyAngularTilt);
			float time2 = (float)meteorRandom.Next(minValue, 100) / 100f;
			meteor.normalizedLandingTime = Mathf.Clamp(landingTimeCurve.Evaluate(time2), 0.06f, 0.93f);
			meteors.Add(meteor);
		}
		for (int l = 0; l < num3; l++)
		{
			Meteor meteor2 = meteors[meteorRandom.Next(0, meteors.Count)];
			meteor2.scale = Mathf.Clamp(meteor2.scale + (float)meteorRandom.Next(8, 40), 25f, baseMeteorSize);
		}
		HUDManager.Instance.MeteorShowerWarningHUD();
		TimeOfDay.Instance.SetBeginMeteorShowerClientRpc();
		meteorsEnabled = true;
	}

	private void Update()
	{
		if (TimeOfDay.Instance.timeHasStarted && StartOfRound.Instance.shipDoorsEnabled && meteorsEnabled)
		{
			for (int i = 0; i < meteors.Count; i++)
			{
				MeteorUpdate(meteors[i]);
			}
		}
	}

	public void MeteorUpdate(Meteor meteor)
	{
		float num = meteor.normalizedLandingTime - TimeOfDay.Instance.normalizedTimeOfDay;
		switch (meteor.phase)
		{
		case 0:
			if (num < baseWarningTime)
			{
				meteor.phase = 1;
				skyRay = new Ray(meteor.landingPosition, meteor.skyDirection);
				ParticleSystem[] componentsInChildren3 = (meteor.meteorObject = UnityEngine.Object.Instantiate(meteorPrefab, skyRay.GetPoint(800f), Quaternion.LookRotation(meteor.landingPosition - skyRay.GetPoint(800f)), RoundManager.Instance.mapPropsContainer.transform)).GetComponentsInChildren<ParticleSystem>();
				meteor.fireParticle = componentsInChildren3[0];
				meteor.fireParticle2 = componentsInChildren3[1];
				meteor.distantSprite = UnityEngine.Object.Instantiate(distantSpritePrefab, meteor.meteorObject.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
				if (base.IsServer)
				{
					CreateMeteorClientRpc(meteor.normalizedLandingTime, meteor.landingPosition, meteor.skyDirection, meteor.scale);
				}
			}
			break;
		case 1:
		{
			if (meteor.previousPhase != 1)
			{
				AudioSource[] componentsInChildren4 = meteor.meteorObject.GetComponentsInChildren<AudioSource>();
				for (int l = 0; l < componentsInChildren4.Length; l++)
				{
					if (!componentsInChildren4[l].CompareTag("Aluminum"))
					{
						componentsInChildren4[l].Play();
					}
				}
				meteor.previousPhase = 1;
			}
			skyRay = new Ray(meteor.landingPosition, meteor.skyDirection);
			float num2 = (baseWarningTime - num) / baseWarningTime;
			dest = Vector3.Lerp(skyRay.GetPoint(heightInSky), skyRay.GetPoint(150f), num2);
			meteor.meteorObject.transform.position = Vector3.Lerp(meteor.meteorObject.transform.position, dest, 5f * Time.deltaTime);
			if (meteor.distantSprite != null)
			{
				meteor.distantSprite.transform.LookAt(StartOfRound.Instance.audioListener.transform.position, Vector3.up);
				meteor.distantSprite.transform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one * meteor.scale, num2 * 2f);
				Ray ray = new Ray(StartOfRound.Instance.audioListener.transform.position, meteor.meteorObject.transform.position - StartOfRound.Instance.audioListener.transform.position);
				meteor.distantSprite.transform.position = ray.GetPoint(380f);
				meteor.meteorObject.transform.localScale = Vector3.one * meteor.scale * 0.25f;
				if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, meteor.meteorObject.transform.position) < Vector3.Distance(meteor.distantSprite.transform.position, StartOfRound.Instance.audioListener.transform.position) && num2 > 0.75f)
				{
					UnityEngine.Object.Destroy(meteor.distantSprite.gameObject);
					AudioSource[] componentsInChildren5 = meteor.meteorObject.GetComponentsInChildren<AudioSource>();
					for (int m = 0; m < componentsInChildren5.Length; m++)
					{
						componentsInChildren5[m].Play();
					}
					if (meteor.scale > 25f)
					{
						componentsInChildren5 = meteor.meteorObject.GetComponentsInChildren<AudioSource>();
						for (int n = 0; n < componentsInChildren5.Length; n++)
						{
							componentsInChildren5[n].pitch *= 0.5f;
							componentsInChildren5[n].maxDistance += 120f;
						}
					}
				}
			}
			else
			{
				meteor.meteorObject.transform.localScale = Vector3.Lerp(meteor.meteorObject.transform.localScale, Vector3.Lerp(Vector3.one * meteor.scale * 0.25f, Vector3.one * meteor.scale, num2), 5f * Time.deltaTime);
			}
			meteor.fireParticle.transform.localScale = new Vector3(meteor.meteorObject.transform.localScale.x, meteor.meteorObject.transform.localScale.y, meteor.meteorObject.transform.localScale.z * 0.72f);
			meteor.fireParticle2.transform.localScale = meteor.fireParticle.transform.localScale;
			if (num <= 0f)
			{
				meteor.phase = 2;
			}
			break;
		}
		case 2:
		case 3:
			if (meteor.previousPhase != meteor.phase)
			{
				meteor.previousPhase = meteor.phase;
				AudioSource[] componentsInChildren = meteor.meteorObject.GetComponentsInChildren<AudioSource>();
				if (!componentsInChildren[0].isPlaying)
				{
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].Play();
					}
					if (meteor.scale > 25f)
					{
						componentsInChildren = meteor.meteorObject.GetComponentsInChildren<AudioSource>();
						for (int j = 0; j < componentsInChildren.Length; j++)
						{
							componentsInChildren[j].pitch *= 0.5f;
							componentsInChildren[j].maxDistance += 120f;
						}
					}
				}
				if (meteor.distantSprite != null)
				{
					UnityEngine.Object.Destroy(meteor.distantSprite.gameObject);
				}
				if (meteor.phase == 2)
				{
					meteor.positionAtStartOfLandingPhase = meteor.meteorObject.transform.position;
				}
			}
			meteor.landingTimer = Mathf.Min(meteor.landingTimer + Time.deltaTime * landingSpeed, 1.3f);
			meteor.meteorObject.transform.localScale = Vector3.one * meteor.scale;
			meteor.fireParticle.transform.localScale = new Vector3(meteor.meteorObject.transform.localScale.x, meteor.meteorObject.transform.localScale.y, meteor.meteorObject.transform.localScale.z * 0.72f);
			meteor.fireParticle2.transform.localScale = meteor.fireParticle.transform.localScale;
			meteor.meteorObject.transform.position = Vector3.LerpUnclamped(meteor.positionAtStartOfLandingPhase, meteor.landingPosition, meteor.landingTimer / 1f);
			meteor.meteorObject.transform.position = new Vector3(meteor.meteorObject.transform.position.x, Mathf.Max(meteor.meteorObject.transform.position.y, -80f), meteor.meteorObject.transform.position.z);
			if (meteor.landingTimer >= 0.4f && meteor.phase != 3)
			{
				meteor.phase = 3;
				if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory)
				{
					AudioSource[] componentsInChildren2 = meteor.meteorObject.GetComponentsInChildren<AudioSource>();
					for (int k = 0; k < componentsInChildren2.Length; k++)
					{
						if (componentsInChildren2[k].CompareTag("Aluminum"))
						{
							componentsInChildren2[k].pitch = UnityEngine.Random.Range(0.84f, 1.16f);
							componentsInChildren2[k].Play();
							break;
						}
					}
				}
			}
			if (meteor.landingTimer >= 1f && !meteor.landed)
			{
				LandMeteor(meteor);
			}
			break;
		}
	}

	public void LandMeteor(Meteor meteor)
	{
		if (!meteor.landed)
		{
			meteor.landed = true;
			StartCoroutine(meteorLandingAnimation(meteor));
		}
	}

	private IEnumerator meteorLandingAnimation(Meteor meteor)
	{
		yield return new WaitForSeconds(0.05f);
		GameObject gameObject = UnityEngine.Object.Instantiate(meteorLandingExplosion, meteor.landingPosition, Quaternion.Euler(-90f, 0f, 0f), RoundManager.Instance.mapPropsContainer.transform);
		gameObject.transform.localScale = Vector3.one * meteor.scale;
		AudioSource[] componentsInChildren = gameObject.GetComponentsInChildren<AudioSource>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
			{
				componentsInChildren[i].spatialBlend = 0f;
				componentsInChildren[i].clip = insideFactoryMeteorClip;
				componentsInChildren[i].pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				componentsInChildren[i].volume = UnityEngine.Random.Range(0.2f, 0.8f);
				componentsInChildren[i].Play();
				break;
			}
			componentsInChildren[i].pitch = UnityEngine.Random.Range(0.9f, 1.1f);
			componentsInChildren[i].Play();
			WalkieTalkie.TransmitOneShotAudio(componentsInChildren[i], componentsInChildren[i].clip);
		}
		ParticleSystem[] componentsInChildren2 = gameObject.GetComponentsInChildren<ParticleSystem>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].transform.localScale = gameObject.transform.localScale * 0.12f;
		}
		bool flag = meteor.scale > baseMeteorSize / 3f;
		float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, meteor.landingPosition);
		Landmine.SpawnExplosion(meteor.landingPosition, spawnExplosionEffect: false, meteor.scale, Mathf.Min(meteor.scale + meteor.scale * 0.4f, meteor.scale + 12f), 40, 50f);
		RoundManager.Instance.DestroyTreeAtPosition(meteor.landingPosition, meteor.scale);
		if (num < Mathf.Max(meteor.scale * 1.5f, 35f) || (flag && num < Mathf.Max(meteor.scale * 2f, 90f)))
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
			GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += Vector3.Normalize(GameNetworkManager.Instance.localPlayerController.transform.position - (meteor.landingPosition - Vector3.up * 25f)) * 110f / num;
		}
		else if (num < Mathf.Max(meteor.scale * 2f, 60f) || (flag && num < Mathf.Max(meteor.scale * 2.5f, 150f)))
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += Vector3.Normalize(GameNetworkManager.Instance.localPlayerController.transform.position - (meteor.landingPosition - Vector3.up * 20f)) * 85f / num;
		}
		yield return new WaitForSeconds(0.6f);
		if (meteor != null && meteor.meteorObject != null)
		{
			UnityEngine.Object.Destroy(meteor.meteorObject);
		}
		meteors.Remove(meteor);
	}
}
