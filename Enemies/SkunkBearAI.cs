using GameNetcodeStuff;
using LethalLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace LethalFauna.Enemies
{
    class SkunkBearAI : EnemyAI
    {
        private Random rnd = new System.Random();
        private Animator animator;

        public GameObject skunkBearSpray;

        // Animation Speed Coefficients (lower = faster)
        float walkAnimationCoefficient = 1.6f;  // scalar to walk speed based on movement speed
        float idleAnimationCoefficient = 1.0f;  // scalar to idle speed
        float runAnimationCoefficient = 0.5f;  // scalar to run speed based on movement speed

        // used to spawn in and manage cubs
        public GameObject bearCubPrefab;
        private List<SkunkBearCubAI> cubs;
        public Transform mouthPoint;

        // Audio
        public AudioSource bearStandAudio;
        public AudioSource bearBiteAudio;
        public AudioSource angryBear;
        public AudioSource sprayRoar1;
        public AudioSource sprayRoar2;
        public AudioSource spraySpawnSound;

        // walking and running audio sources (loops through arrays on call)
        public AudioSource[] walksteps;
        int walkstepIndex = 0;
        float walkAudioTrack = 0f;
        float walkAudioPoint1 = 0.1f;  // anim time to play first walk step
        float walkAudioPoint2 = 1.05f;  // anim time to play second walk step

        enum State
        {
            Sleeping,
            Idle,
            Attacking,
            Patrolling,
            Spraying,
            Standing,
            Hungry
        }

        public void Start()
        {
            base.Start();
            if (!animator) { animator = GetComponent<Animator>(); }

            // initialize 0-2 cub bear AIS
            cubs = new List<SkunkBearCubAI>();
            int totalCubs = rnd.Next(1, 3);  // 1-2 cubs. No cubs is currently unsupported
            for(int i = 0; i < totalCubs; i++)
            {
                var cub = Instantiate<GameObject>(bearCubPrefab, this.transform.position, this.transform.rotation);  // create cub
                cub.GetComponent<NetworkObject>().Spawn();  // net sync
                cub.GetComponent<SkunkBearCubAI>().mommaBear = this;
                cubs.Add(cub.GetComponent<SkunkBearCubAI>());
            }


            // start in patrolling state (sleeping state not available)
            SwitchToBehaviourClientRpc((int)State.Patrolling);
        }

        bool deathSwitch = false;  // allows for only playing a death anim once
        public override void Update()
        {
            base.Update();

            // death
            if (isEnemyDead)
            {
                agent.speed = 0;
                setAnimationSpeedClientRpc(1);
                if (bearStandAudio.isPlaying) { bearStandAudio.Stop(); }
                if (bearBiteAudio.isPlaying) { bearBiteAudio.Stop(); }
                if (angryBear.isPlaying) { angryBear.Stop(); }
                if (sprayRoar1.isPlaying) { sprayRoar1.Stop(); }
                if (sprayRoar2.isPlaying) { sprayRoar2.Stop(); }

                if (!deathSwitch || (!animator.GetCurrentAnimatorStateInfo(0).IsName("DeathAnimation") && !animator.GetCurrentAnimatorStateInfo(0).IsName("StayDead")))
                {
                    DoAnimationClientRpc(7);
                    animPlayClientRpc("DeathAnimation");
                    deathSwitch = true;
                }
                return;
            }

            // Face the player closest to a cub
            // if the bear is spraying, or standing
            if ((currentBehaviourStateIndex == (int)State.Spraying || currentBehaviourStateIndex == (int)State.Standing) && !isTweeningAction)
            {
                SkunkBearCubAI bestCub = null;
                PlayerControllerB closestPly = null;
                float closestDist = 9999f;
                foreach (var cub in cubs)
                {
                    var ply = getClosestPlayerToGO(cub.gameObject);
                    float dist = Vector3.Distance(ply.transform.position, cub.transform.position);

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestCub = cub;
                        closestPly = ply;
                    }
                }

            Vector3 lookDir = (closestPly.transform.position - transform.position).normalized;
            lookDir.y = 0;  // flatten to avoid head tilt
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            footstepSounds();
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            if (this.isEnemyDead)
            {
                return;
            }
            this.enemyHP -= force;

            // Flee!
            Debug.Log("Bear: Ouch, you will regret that!");
            provokePoints = 35;

            if (enemyHP <= 0)
            {
                isEnemyDead = true;
                animPlayClientRpc("DeathAnimation");
            }
            else
            {
                SwitchToBehaviourState((int)State.Attacking);
                escalationSwitch = 3;
                playAngryBearClientRpc();
            }
        }


        // works by comparing the previous time step to the current time step
        // and playing a footstep sound at key animation points
        public void footstepSounds()
        {
            // walking step sounds
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("WalkAnimation"))
            {
                //Debug.Log("WalkAudioTrack: " + walkAudioTrack);
                //Debug.Log("walkAudioPoint: " + getAnimTime());
                if (walkAudioTrack < walkAudioPoint1 && getAnimTime() >= walkAudioPoint1)
                {
                    //Debug.Log("Play WalkStep");
                    playWalkStep();
                }


                if (walkAudioTrack < walkAudioPoint2 && getAnimTime() >= walkAudioPoint2)
                {
                    //Debug.Log("Play WalkStep");
                    playWalkStep();
                }

                // update walkAudioTrack
                walkAudioTrack = getAnimTime();
            }

            // running step sounds

        }

        // accurately gives animation clip time accounting for animator speed
        public float getAnimTime()
        {
            return animator.GetCurrentAnimatorStateInfo(0).length * (animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f) * this.animator.speed;
        }

        // play from a list of walk steps
        // loops through the list for variance
        public void playWalkStep()
        {
            if (walkstepIndex >= walksteps.Length) { walkstepIndex = 0; }

            walksteps[walkstepIndex].Play();

            walkstepIndex++;
        }

        // the bear escalates through increasingly aggressive behavior
        // it only resets if provokePoints goes under 1
        // this switch prevents concurrent state transitions
        public int escalationSwitch = 0;
        public override void DoAIInterval()
        {
            base.DoAIInterval();

            if(isEnemyDead) { return; }

            // update to threat level parameter
            threatTick();

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Sleeping:
                    sleepingState();
                    return;
                case (int)State.Attacking: // requires escalation switch = 3 line
                    attackingState();
                    break;
                case (int)State.Patrolling:
                    patrolState();
                    break;
                case (int)State.Standing: // requires escalation switch = 1 line
                    standState();
                    break;
                case (int)State.Spraying:  // requires escalation switch = 2 v
                    sprayState();
                    break;
                case (int)State.Hungry:
                    hungryState();
                    return;

                default:
                    break;
            }

            // state transitions based on provocation
            // in situations where the player is very close or the bear is enraged, the bear
            // will skip standing, spraying, and just attack
            if ((provokePoints > 25 || getClosestDistanceToCubsKnown() < 2.5) && escalationSwitch == 2)
            {
                Debug.Log("switch to attacking state");
                SwitchToBehaviourState((int)State.Attacking);
                StopSearch(currentSearch);
                escalationSwitch = 3;
                playAngryBearClientRpc();
            }

            // in situations where the player is pretty close or the bear is angry, the bear
            // will skip the stand warning and go right to sprays
            if ((provokePoints > 17 || getClosestDistanceToCubsKnown() < 5) && escalationSwitch == 1)
            {
                SwitchToBehaviourState((int)State.Spraying);
                StopSearch(currentSearch);
                escalationSwitch = 2;
            }

            // standing state transition (provoked)
            // the player has to not be super close to cubs for the stand warning to occur
            // the bear must have not stood within the last 8 seconds (cooldown)
            if ((provokePoints > 4.5 && getClosestDistanceToCubsKnown() > 5 && Time.time - lastTimeSinceStand > standCooldown) && escalationSwitch == 0)
            {
                StopSearch(currentSearch);
                SwitchToBehaviourState((int)State.Standing);
                playStandingBearClientRpc();
                lastTimeSinceStand = Time.time;
                escalationSwitch = 1;
            }

            if(provokePoints < 1) { escalationSwitch = 0; }
        }

        [ClientRpc]
        public void playAngryBearClientRpc() { angryBear.Play(); }
        [ClientRpc]
        public void playStandingBearClientRpc() { bearStandAudio.Play(); }

        // sleeping state is the initial state, currently scrapped until animation is fixed
        float wakeupTime = -1;
        bool awake = false;
        public void sleepingState()
        {
            //Debug.Log("Skunk Bear: sleeping... current time = " + Time.time + " wakeup time = " + wakeupTime);
            if (wakeupTime == -1)
            {
                wakeupTime = (float)(Time.time + rnd.NextDouble() * 300 + 60f);  // sleeps for up to 6 minutes, 1 minute minimum
            }

            if (Time.time > wakeupTime) { awake = true; }

            // still asleep
            if (!awake)
            {
                //Debug.Log("asleep...");
                animator.speed = 1;
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("SleepAnimation"))
                {
                    //Debug.Log("sleep anim set");
                    DoAnimationClientRpc(-1);
                    animPlayClientRpc("SleepAnimation");
                }
            }
            else  // awake
            {
                //Debug.Log("awake!");
                DoAnimationClientRpc(1);  // awaken animation and then idle transition

                // when finally in idle animation, transition out of sleep state
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("IdleAnimation"))
                {
                    SwitchToBehaviourClientRpc((int)State.Patrolling);
                }
            }
        }

        float biteRange = 5.8f;
        public void attackingState()
        {
            agent.speed = 5.8f;  // pretty quick, bears are fast in real life so... lol

            if (!isTweeningAction)
            {
                // stand and walk animations and speed
                if (agent.velocity.magnitude > (agent.speed / 4))
                {
                    //Debug.Log("Walk Anim Set");
                    if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                    setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
                }
                else if (agent.velocity.magnitude <= (agent.speed / 12))
                {
                    //Debug.Log("Idle Anim Set");
                    if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                    setAnimationSpeedClientRpc(idleAnimationCoefficient);
                }
            }
            else
            {  // bite anim speed
                setAnimationSpeedClientRpc(1);
            }

            // charge at the player directly
            PlayerControllerB closestPly = null;
            float closestDist = 9999f;

            foreach (var cub in cubs)
            {
                var ply = getClosestPlayerToGO(cub.gameObject);
                float dist = Vector3.Distance(ply.transform.position, cub.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPly = ply;
                }
            }

            if (provokePoints <= 18 || closestDist > 30f) 
            {
                provokePoints = 18;
                escalationSwitch = 2;
                SwitchToBehaviourState((int)State.Spraying);
            }

            SetDestinationToPosition(closestPly.transform.position);

            // biting
            var distToSelf = Vector3.Distance(closestPly.transform.position, transform.position);
            Debug.Log("isTweeningAction = " + isTweeningAction + " closestDist = " + distToSelf + " timeSince = " + (Time.time - lastTimeSinceAction));
            if (!isTweeningAction && distToSelf < biteRange && Time.time - lastTimeSinceAction > 2.5f)
            {
                Debug.Log("Bite attempt");
                lastTimeSinceAction = Time.time;
                if (sprayTweenRoutine != null)
                    StopCoroutine(sprayTweenRoutine);
                sprayTweenRoutine = StartCoroutine(RotateAndBite(closestPly.transform.position));
            }
        }

        private IEnumerator RotateAndBite(Vector3 targetPosition)
        {
            isTweeningAction = true;

            Quaternion startRot = transform.rotation;
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(direction);

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRot;

            // Play bite animation
            DoAnimationClientRpc(5);  // Assuming 5 is the bite state
            animPlayClientRpc("BiteAnimation"); // forces animation
            bearBiteAudio.Play();
            setAnimationSpeedClientRpc(1.0f);

            // Damage after a brief moment
            yield return new WaitForSeconds(0.4f);

            var target = getClosestPlayerToGO(this.gameObject);
            if (target != null && Vector3.Distance(target.transform.position, mouthPoint.position) < biteRange)
            {
                target.DamagePlayer(60, true, true, CauseOfDeath.Mauling, 0, false); // Adjust damage if needed
            }

            yield return new WaitForSeconds(0.3f); // Finish animation duration

            isTweeningAction = false;
        }

        // the patrol state for an adult bear is much simpler than a cub
        // it uses a traditional AI search algorithm
        public void patrolState()
        {
            //Debug.Log("Patrol State");
            agent.speed = 4.2f;
            agent.angularSpeed = 160;

            if(currentSearch == null || !currentSearch.inProgress) { StartSearch(transform.position); }

            // stand and walk animations and speed
            if (agent.velocity.magnitude > (agent.speed / 4))
            {
                //Debug.Log("Walk Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
            }
            else if (agent.velocity.magnitude <= (agent.speed / 12))
            {
                //Debug.Log("Idle Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                setAnimationSpeedClientRpc(idleAnimationCoefficient);
            }

            if (Time.time - lastEat > eatCooldown)
            {
                // check for any potential food sources nearby
                List<GameObject> food = FindObjectsOfType<GameObject>().Where(x => x.name == "RedLocustHive(Clone)" || x.GetComponent<RagdollGrabbableObject>() != null).ToList();
                float best = 20f;
                for (int i = 0; i < food.Count; i++)
                {
                    if (Vector3.Distance(transform.position, food[i].transform.position) < best)
                    {
                        best = Vector3.Distance(transform.position, food[i].transform.position);
                        thingToEat = food[i];
                    }
                }
                // found a food source, stop patrolling and head to food source
                if (thingToEat != null)
                {
                    StopSearch(currentSearch);
                    lastEat = Time.time;
                    SwitchToBehaviourState((int)State.Hungry);
                }
            }
        }

        // a semi-aggressive state where the bear targets the player and sprays at
        // them. note: while in this state the bear shouldn't get angrier by getting closer to the
        // player, unless they are very close.
        private Coroutine sprayTweenRoutine;
        private bool isTweeningAction = false;
        private float lastTimeSinceAction = 0f;
        private float sprayCooldown = 12f;
        public void sprayState()
        {
            //Debug.Log("Spray State");
            PositionBetweenCubAndThreat();

            if (!isTweeningAction)
            {
                agent.speed = 5.2f;
                // stand and walk animations and speed
                if (agent.velocity.magnitude > (agent.speed / 4))
                {
                    //Debug.Log("Walk Anim Set");
                    if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                    setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
                }
                else if (agent.velocity.magnitude <= (agent.speed / 12))
                {
                    //Debug.Log("Idle Anim Set");
                    if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                    setAnimationSpeedClientRpc(idleAnimationCoefficient);
                }

                if (provokePoints <= 1) { SwitchToBehaviourState((int)State.Patrolling); }
            }
            else
            {
                agent.speed = 0;
                DoAnimationClientRpc(4);
            }
        }

        [ClientRpc]
        public void playSprayRoarClientRpc(int code)
        { 
            if(code == 0)
            {
                sprayRoar1.Play();
            }
            else
            {
                sprayRoar2.Play();
            }
        }

        // coroutine for during around and spraying the player
        int sprayRoarOsc = 0;  // oscillate between two sounds
        private IEnumerator RotateAndSpray(Vector3 targetPosition)
        {
            isTweeningAction = true;

            if(sprayRoarOsc == 0)
            {
                sprayRoarOsc = 1;

            }
            else
            {
                sprayRoarOsc = 0;
            }
            playSprayRoarClientRpc(sprayRoarOsc);

            Quaternion startRot = transform.rotation;
            Vector3 direction = transform.position - targetPosition;
            direction.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(direction);

            float duration = 0.7f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRot;

            // Spray animation start
            animPlayClientRpc("SprayAnimation");
            DoAnimationClientRpc(4); // Replace 4 with your spray animation state
            setAnimationSpeedClientRpc(1.0f);

            // Optional delay or state transition after spray
            yield return new WaitForSeconds(0.6f);

            spawnSprayClientRpc(transform.position, targetPosition);

            yield return new WaitForSeconds(0.9f);

            // Reset so it can spray again if needed later
            isTweeningAction = false;
        }

        [ClientRpc]
        public void spawnSprayClientRpc(Vector3 position, Vector3 targetPosition)
        {
            try
            {
                GameObject sprayObj = Instantiate(skunkBearSpray, position, Quaternion.identity);
                spraySpawnSound.Play();

                var cloudScript = sprayObj.transform.Find("BearSmoke").GetComponent<SkunkBearCloud>();
                if (cloudScript != null)
                {
                    Debug.Log("passing movement dest as " + targetPosition + " and startDest as " + transform.position);
                    cloudScript.movementDest = targetPosition;
                    cloudScript.startDest = transform.position;
                }
                else
                {
                    Debug.Log("Skunk Bear Error: Cloud is null.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        // also includes a spray trigger, I know its jank
        public void PositionBetweenCubAndThreat()
        {
            if (cubs.Count == 0) return;

            SkunkBearCubAI bestCub = null;
            PlayerControllerB closestPly = null;
            float closestDist = 9999f;

            foreach (var cub in cubs)
            {
                var ply = getClosestPlayerToGO(cub.gameObject);
                float dist = Vector3.Distance(ply.transform.position, cub.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestCub = cub;
                    closestPly = ply;
                }
            }

            if (!isTweeningAction)
            {
                PlayerControllerB target = closestPly;
                if (target != null && Vector3.Distance(transform.position, closestPly.transform.position) < 15f && Time.time - lastTimeSinceAction > sprayCooldown)
                {
                    lastTimeSinceAction = Time.time;
                    if (sprayTweenRoutine != null)
                        StopCoroutine(sprayTweenRoutine);

                    sprayTweenRoutine = StartCoroutine(RotateAndSpray(target.transform.position));
                }
            }

            if (bestCub == null || closestPly == null) return;

            // Compute position halfway between cub and player, with some bear-safe offset
            Vector3 playerPos = closestPly.transform.position;
            Vector3 cubPos = bestCub.transform.position;
            Vector3 directionToPlayer = (playerPos - cubPos).normalized;

            // Place the bear a bit closer to the cub, not directly in the middle
            Vector3 newPosition = cubPos + directionToPlayer * (closestDist * 0.3f);

            // Set the bear's destination
            SetDestinationToPosition(newPosition);
        }

        float lastTimeSinceStand = 0f;
        float standCooldown = 8f;  // the bear has to wait 8 seconds before standing again
        // warning signal to player
        public void standState()
        {
            //Debug.Log("Stand State");

            // bear isn't moving in this state
            agent.speed = 0;

            // stand animation
            DoAnimationClientRpc(3);

            // finish animation faster depending on how angry the bear is
            setAnimationSpeedClientRpc(Math.Max(1, provokePoints / 12) / 1.4f);

            // transition out at finished animation
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("FinishedStanding") || animator.GetInteger("State") != 3)
            {
                SwitchToBehaviourState((int)State.Patrolling);
                StartSearch(transform.position);
                bearStandAudio.Stop();
            }
        }

        // found something to eat
        // the bear will not feel threatened while in this state unless its interrupted
        GameObject thingToEat = null;
        float eatCooldown = 45f;
        float lastEat = 0f;
        public void hungryState()
        {
            // stand and walk animations and speed
            if (agent.velocity.magnitude > (agent.speed / 4))
            {
                //Debug.Log("Walk Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
            }
            else if (agent.velocity.magnitude <= (agent.speed / 12))
            {
                //Debug.Log("Idle Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                setAnimationSpeedClientRpc(idleAnimationCoefficient);
            }

            // check if food has been picked up
            bool isBody = thingToEat.GetComponent<RagdollGrabbableObject>() != null;
            bool foodPickedUp = (isBody && thingToEat.GetComponent<RagdollGrabbableObject>().isHeld) || (!isBody && thingToEat.GetComponent<GrabbableObject>().isHeld);

            // close enough to eating target, begin eating
            if (Vector3.Distance(transform.position, thingToEat.transform.position) < 3.5f)
            {
                moveTowardsDestination = false;
                agent.speed = 0; // do not move while eating
                DoAnimationClientRpc(6); // play eating animation
                setAnimationSpeedClientRpc(idleAnimationCoefficient);
                if (foodPickedUp)
                {
                    SwitchToBehaviourState((int)State.Attacking);
                    escalationSwitch = 3;
                    playAngryBearClientRpc();
                }
            }
            else
                SetDestinationToPosition(thingToEat.transform.position); // constantly update position of food

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("FinishedEating")) // if we finish eating animation then go back to patrol state
                SwitchToBehaviourClientRpc((int)State.Patrolling);
            else if (thingToEat != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > .8f) // if we get 80% through eating animation then destroy the food
            {
                if (thingToEat.GetComponent<RagdollGrabbableObject>() != null)
                    Destroy(thingToEat);
                else
                    thingToEat.GetComponent<NetworkObject>().Despawn(true);
            }
        }

        // updates the threat threshold to the player every 0.2 seconds
        // defined as how "threatened" the bear currently is
        // at 4.5 points, the bear stands in response to the player, a warning signal.
        // at 17 points, the bear will begin emitting toxic fumes, and may swipe at the player (3 times max / minute)
        // at 25 points, the bear becomes aggressive (chase mode)
        float provokePoints = 0;
        float passiveThreatDecay = 0.4f;
        public void threatTick()
        {
            // passive decay of threat level
            if(provokePoints > 0) { provokePoints -= passiveThreatDecay; }

            // assess threat to cubs
            // suggested improvement: add radius and/or sight requirement for threat level to change

            // determine if provoke points should be less due to a crouching player
            float moreProvokedValCub = 18f;
            float lessProvokedValCub = 12f;
            float moreProvokedValBear = 18f;
            float lessProvokedValBear = 12f;
            float cubDistance = getClosestDistanceToCubsKnown();
            PlayerControllerB plyNearBear = getClosestPlayerToGO(this.gameObject);
            if (cubDistance < 0)
            {
                moreProvokedValCub = 9f;
                lessProvokedValCub = 6f;
                cubDistance *= -1;
            }
            if (plyNearBear.isCrouching)
            {
                moreProvokedValBear = 9f;
                lessProvokedValBear = 6f;
            }

            provokePoints += lessProvokedValCub / cubDistance;
            for (int i = 0; i < cubs.Count; i++)
            {
                var cub = cubs[i];
                if (cub.currentBehaviourStateIndex == 5)  // fleeing state
                {
                    // uses closest distance to cubs known unless player is within certain radius
                    // of the bear. If they are within this radius the bear assumes the player
                    // is causing the panic.
                    if (Vector3.Distance(plyNearBear.transform.position, transform.position) < 10f)
                    {
                        provokePoints += moreProvokedValBear / (Vector3.Distance(plyNearBear.transform.position, transform.position) + 1.5f);
                    }
                    else
                    {
                        provokePoints += moreProvokedValCub / (cubDistance + 1.5f);
                    }
                }
                else
                {
                    provokePoints += lessProvokedValCub / (cubDistance + 1.5f);
                }
            }

            // assess threat to self
            // threat to self is not counted if warning sprays are being fired at the player
            if (currentBehaviourStateIndex != (int)State.Spraying)
            {
                if (cubs.Count == 0) {
                    provokePoints += moreProvokedValBear / (Vector3.Distance(plyNearBear.transform.position, transform.position) + 1.5f);
                }
                else
                {
                    provokePoints += lessProvokedValBear / (Vector3.Distance(plyNearBear.transform.position, transform.position) + 1.5f);
                }
            }

            // provoke point cap
            if(provokePoints > 35) { provokePoints = 35; }
        }

        // this function is pretty complex
        // so it will get the closest distance any player is to any cub
        // but it also accounts for if the mother bear actually sees the player being close to them
        public float getClosestDistanceToCubsKnown()
        {
            float bestDist = 9999f;
            Vector3 bearPos = transform.position;
            Vector3 bearForward = transform.forward;
            PlayerControllerB player = null;

            foreach (var cub in cubs)
            {
                Vector3 dirToCub = cub.transform.position - bearPos;
                float distToCub = dirToCub.magnitude;
                Vector3 dirNormalized = dirToCub.normalized;

                // Check if within ~100 degree FOV (adjust the dot threshold as needed)
                if (Vector3.Dot(bearForward, dirNormalized) > 0.17f)
                {
                    // Optional: check line of sight (raycast to cub)
                    if (!Physics.Raycast(bearPos + Vector3.up * 0.5f, dirNormalized, distToCub, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue; // Something is blocking the line of sight
                    }

                    // Check for visible players around the current cub
                    for (int i = 0; i < RoundManager.Instance.playersManager.allPlayerScripts.Length; i++)
                    {
                        Vector3 dirToPly = RoundManager.Instance.playersManager.allPlayerScripts[i].transform.position - bearPos;
                        float distToPly = dirToPly.magnitude;
                        Vector3 dirNormalized2 = dirToPly.normalized;

                        if (Vector3.Dot(bearForward, dirNormalized2) > 0.17f)
                        {
                            if (!Physics.Raycast(bearPos + Vector3.up * 0.5f, dirNormalized2, distToPly, ~0, QueryTriggerInteraction.Ignore))
                            {
                                continue; // Something is blocking the line of sight
                            }

                            // Track the closest visible cub to a player
                            float distCubToPly = Vector3.Distance(RoundManager.Instance.playersManager.allPlayerScripts[i].transform.position, cub.transform.position);
                            if (distCubToPly < bestDist)
                            {
                                bestDist = distCubToPly;
                                player = RoundManager.Instance.playersManager.allPlayerScripts[i];
                            }
                        }
                    }
                }
            }

            return player != null && player.isCrouching ? -1 * bestDist : bestDist; // Return the distance as negative to indicate the player closest to any cub is crouching
        }

        public PlayerControllerB getClosestPlayerToGO(GameObject GO)
        {
            var m = RoundManager.Instance;
            var closestDist = 10000f;
            PlayerControllerB closestPly = null;
            for(int i = 0; i < m.playersManager.allPlayerScripts.Length; i++)
            {
                var ply = m.playersManager.allPlayerScripts[i];
                var dist = Vector3.Distance(ply.transform.position, GO.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPly = ply;
                }
            }
            return closestPly;
        }

        [ClientRpc]
        public void setAnimationSpeedClientRpc(float speed)
        {
            this.animator.speed = speed;
        }

        [ClientRpc]
        // start animation with transitions
        public void DoAnimationClientRpc(int index)
        {
            //LogIfDebugBuild($"Animation: {index}");
            if (RoundManager.Instance.IsServer)
            {
                if (this.animator) { this.animator.SetInteger("State", index); }
            }
        }


        // directly play an animation
        [ClientRpc]
        public void animPlayClientRpc(String name)
        {
            animator.Play(name);
        }
    }
}