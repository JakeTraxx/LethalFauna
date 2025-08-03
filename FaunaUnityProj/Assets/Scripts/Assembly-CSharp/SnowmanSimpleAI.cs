using System;
using UnityEngine;

public class SnowmanSimpleAI : MonoBehaviour
{
	private bool snowmanTurns;

	private float snowmanInterval;

	private void Start()
	{
		if (RoundManager.Instance.IsServer)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + (int)base.transform.position.x);
			snowmanInterval = UnityEngine.Random.Range(0f, 7f);
			if (random.Next(0, 100) < 30)
			{
				snowmanTurns = true;
			}
		}
	}

	private void Update()
	{
		if (!snowmanTurns || !RoundManager.Instance.IsServer || !(Time.realtimeSinceStartup - snowmanInterval > 8f))
		{
			return;
		}
		snowmanInterval = Time.realtimeSinceStartup;
		bool flag = true;
		int num = -1;
		float num2 = 1000f;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && !StartOfRound.Instance.allPlayerScripts[i].isInsideFactory)
			{
				float num3 = Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].transform.position, base.transform.position);
				if (num3 < num2)
				{
					num = i;
					num2 = num3;
				}
				if (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position, 90f, 200, 2f))
				{
					flag = false;
					break;
				}
			}
		}
		if (flag)
		{
			if (num != -1 && (StartOfRound.Instance.livingPlayers != 1 || UnityEngine.Random.Range(0, 100) <= 14) && UnityEngine.Random.Range(0, 100) <= 27)
			{
				RoundManager.Instance.tempTransform.position = base.transform.parent.position;
				RoundManager.Instance.tempTransform.LookAt(StartOfRound.Instance.allPlayerScripts[num].transform.position);
				float num4 = Vector3.Angle(RoundManager.Instance.tempTransform.eulerAngles, base.transform.parent.eulerAngles);
				Vector3 eulerAngles = RoundManager.Instance.tempTransform.eulerAngles;
				eulerAngles.x = 0f;
				eulerAngles.z = 0f;
				bool laugh = num4 > 30f && (UnityEngine.Random.Range(0, 100) < 50 || num2 < 8f);
				RoundManager.Instance.TurnSnowmanServerRpc(base.transform.parent.position, eulerAngles, laugh);
			}
		}
		else if (num != -1 && num2 > 50f)
		{
			RoundManager.Instance.tempTransform.position = base.transform.parent.position;
			RoundManager.Instance.tempTransform.LookAt(StartOfRound.Instance.allPlayerScripts[num].transform.position);
			if (!(Vector3.Angle(RoundManager.Instance.tempTransform.eulerAngles, base.transform.parent.eulerAngles) < 10f))
			{
				Vector3 eulerAngles2 = RoundManager.Instance.tempTransform.eulerAngles;
				eulerAngles2.x = 0f;
				eulerAngles2.z = 0f;
				bool laugh2 = false;
				RoundManager.Instance.TurnSnowmanServerRpc(base.transform.parent.position, eulerAngles2, laugh2);
			}
		}
	}
}
