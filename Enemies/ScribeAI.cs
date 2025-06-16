using GameNetcodeStuff;
using System.Linq;
using UnityEngine;

namespace LethalFauna.Enemies
{
    class ScribeAI : EnemyAI
    {
        // Activities the Scribe watches for
        readonly string[] activityTypes = { "Running", "Jumping", "Climbing", "Crouching", "Attacking", "Picking Up Item", "Dropping Item", "Using Item", "Talking" };

        // Timer variables
        float activityTimer;
        float cooldownTimer;
        float writingSpeed;

        // States the Scribe can be in
        enum State
        {
            Idle,
            Watch,
            Attack
        }

        public override void Start()
        {
            base.Start();

            // Ensure starting state is Idle
            currentBehaviourStateIndex = (int)State.Idle;
        }

        public override void Update()
        {
            base.Update();

            // Update timer variables if they have been set
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;
            if (writingSpeed > 0f)
                writingSpeed -= Time.deltaTime * 0.2f;

            // Things to update/check in the Attack state
            if (currentBehaviourStateIndex == (int)State.Attack)
            {
                // Increase fear level gradually
                targetPlayer.IncreaseFearLevelOverTime(1.4f, 1f);
                // If the player leaves the interior then abandon the attack
                if (!targetPlayer.isInsideFactory)
                {
                    LethalFaunaMod.log.LogInfo($"{gameObject.name}: Abandoning attack due to player leaving facility");
                    movingTowardsTargetPlayer = false;
                    agent.speed = 3.5f;
                    cooldownTimer = 10f;
                    searchCoroutine = null;
                    currentBehaviourStateIndex = (int)State.Idle;
                }
            }

            // Things to update/check in the Watch state
            if (currentBehaviourStateIndex == (int)State.Watch)
            {
                // If the writing speed timer is done then start to lower the activity timer
                if (writingSpeed <= 0f)
                {
                    activityTimer -= Time.deltaTime;
                    // After 10 seconds of inactivity go back to searching for a player
                    if (activityTimer <= 0)
                    {
                        LethalFaunaMod.log.LogInfo($"{gameObject.name}: Abandoning watch due to player inactivity");
                        movingTowardsTargetPlayer = false;
                        agent.speed = 3.5f;
                        cooldownTimer = 10f;
                        searchCoroutine = null;
                        currentBehaviourStateIndex = (int)State.Idle;
                    }
                }
                // If the writing speed timer gets too high then attack the targeted player
                else if (writingSpeed >= 5f)
                {
                    LethalFaunaMod.log.LogInfo($"{gameObject.name}: Can't keep up with activity, attacking");
                    movingTowardsTargetPlayer = true;
                    agent.speed = 5.5f;
                    targetPlayer.JumpToFearLevel(0.7f, true);
                    currentBehaviourStateIndex = (int)State.Attack;
                }
                // Reset the inactivity timer to 10 if the writing speed timer is not done
                else
                    activityTimer = 10f;
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Idle:
                    // Start a search for a player if we havent already
                    if (searchCoroutine == null)
                    {
                        LethalFaunaMod.log.LogInfo($"{gameObject.name}: Starting idle player search");
                        StartSearch(transform.position);
                    }
                    // If a player is in LOS, in range, and the cooldown timer is done then watch the player
                    if (TargetClosestPlayer(1.5f, true, 45f) && cooldownTimer <= 0f)
                    {
                        LethalFaunaMod.log.LogInfo($"{gameObject.name}: Player found during search, watching player");
                        StopSearch(currentSearch, true);
                        activityTimer = 10f;
                        currentBehaviourStateIndex = (int)State.Watch;
                    }
                    break;

                case (int)State.Watch:
                    // If we don't see the player in LOS then follow, otherwise stay put
                    PlayerControllerB[] seen = GetAllPlayersInLineOfSight();
                    if (seen == null || !seen.Contains(targetPlayer))
                    {
                        movingTowardsTargetPlayer = true;
                        agent.speed = 3.5f;
                    }
                    else
                    {
                        movingTowardsTargetPlayer = false;
                        agent.speed = 0f;
                    }
                    break;

                default:
                    break;
            }
        }

        public override void OnCollideWithPlayer(Collider collider)
        {
            base.OnCollideWithPlayer(collider);

            // If attacking a player then kill the player upon colliding with them
            if (currentBehaviourStateIndex == (int)State.Attack)
            {
                targetPlayer.DamagePlayer(100, causeOfDeath: CauseOfDeath.Mauling);
                if (targetPlayer.isPlayerDead)
                {
                    LethalFaunaMod.log.LogInfo($"{gameObject.name}: Killed the player");
                    movingTowardsTargetPlayer = false;
                    agent.speed = 3.5f;
                    cooldownTimer = 10f;
                    searchCoroutine = null;
                    currentBehaviourStateIndex = (int)State.Idle;
                }
            }
        }

        // Register activities done by player being watched and add to writing speed timer
        public void NewActivity(PlayerControllerB player, int type)
        {
            if (currentBehaviourStateIndex == (int)State.Watch && targetPlayer == player)
            {
                writingSpeed += 1f;
                LethalFaunaMod.log.LogInfo($"{gameObject.name}: New activity {activityTypes[type]} detected");
            }
        }
    }
}
