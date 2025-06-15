
using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace LethalFauna.Enemies
{
    class SkunkBearCloud : MonoBehaviour
    {
        List<PlayerControllerB> players = new List<PlayerControllerB>();

        float lastTimeSinceTick = 0;
        float tickRate = 0.5f;
        int damage = 7;

        float expiration = 0;
        public Vector3 movementDest;
        public Vector3 startDest;
        private float movementDuration = 0.4f;
        private float timeAwake = 0;

        public void Start()
        {
            expiration = Time.time + 12f;  // spray lasts for 12 seconds
        }

        public void Update()
        {
            if(players.Count > 0 && Time.time - lastTimeSinceTick > tickRate)
            {
                lastTimeSinceTick = Time.time;
                foreach(var player in players)
                {
                    if (player && !player.isPlayerDead)
                    {
                        player.DamagePlayer(damage, true, true, CauseOfDeath.Suffocation, 0, false);
                    }
                }
            }

            // cloud movement (needed for the cloud to actually be a threat to the player)
            if (movementDest != null)
            {
                timeAwake += Time.deltaTime;
                if (timeAwake / movementDuration > 1.0f)
                {
                    this.transform.position = movementDest;
                }
                else
                {
                    this.transform.position = Vector3.Lerp(startDest, movementDest, timeAwake / movementDuration);
                }
            }

            players.RemoveAll(p => p == null || p.isPlayerDead);

            if(Time.time > expiration) { Destroy(gameObject);  }
        }

        public void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerControllerB>();
            if (player && !players.Contains(player))
            {
                players.Add(player);
                Debug.Log("Player entered the spray zone!");
            }
        }

        public void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<PlayerControllerB>();
            if (player)
            {
                players.Remove(player);
                Debug.Log("Player exited the spray zone!");
            }
        }
    }
}
