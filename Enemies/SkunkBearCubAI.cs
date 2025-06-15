using GameNetcodeStuff;
using LethalLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace LethalFauna.Enemies
{
    class SkunkBearCubAI : EnemyAI
    {
        private Random rnd = new System.Random();

        private Animator animator;

        // Animation Speed Coefficients (lower = faster)
        float walkAnimationCoefficient = 0.8f;  // scalar to walk speed based on movement speed
        float idleAnimationCoefficient = 1.0f;  // scalar to idle speed
        float runAnimationCoefficient = 0.5f;  // scalar to run speed based on movement speed

        // walking and running audio sources (loops through arrays on call)
        public AudioSource[] walksteps;
        int walkstepIndex = 0;
        float walkAudioTrack = 0f;
        float walkAudioPoint1 = 1.82f;  // anim time to play first walk step
        float walkAudioPoint2 = 3.02f;  // anim time to play second walk step

        public AudioSource[] runsteps;

        public AudioSource audioPurr;
        public AudioSource audioHit;
        public AudioSource audioPanic;

        // the big boi
        public SkunkBearAI mommaBear;

        // for other cub interactions?
        private List<SkunkBearAI> cubs;

        enum State
        {
            Sleeping,
            Idle,
            Patrolling,
            Curious,  // checking out the player or some object
            Eating,
            Fleeing,
        }

        public void Start()
        {
            base.Start();

            // initializing technical variables
            if (!animator)
            {
                animator = transform.Find("SkunkBearling6").GetComponent<Animator>();
            }
            mapNodeReference = new GameObject[allAINodes.Length];
            Array.Copy(allAINodes, mapNodeReference, allAINodes.Length);
            patrolPoint = this.transform.position;

            // start in patrolling state for now (sleep not available)
            cubDestination = Vector3.zero;  // should be set to this value each time
            SwitchToBehaviourClientRpc((int)State.Sleeping);
        }

        bool deathSwitch = false;  // allows for only playing a death anim once
        public override void Update()
        {
            base.Update();

            // death
            if (isEnemyDead)
            {
                agent.speed = 0;
                setAnimationSpeedClientRpc(1.7f);
                if (audioPurr.isPlaying) { audioPurr.Stop(); }
                if (audioPanic.isPlaying) { audioPanic.Stop(); }
                if (!deathSwitch || (!animator.GetCurrentAnimatorStateInfo(0).IsName("DeathAnimation") && !animator.GetCurrentAnimatorStateInfo(0).IsName("staydeadcub")))
                {
                    animPlayClientRpc("DeathAnimation");
                    deathSwitch = true;
                }
                return;
            }

            // looking at player in Curious State
            if (currentBehaviourStateIndex == (int)State.Curious && playerTarget)
            {
                // looking at the player
                facePosition(playerTarget.transform.position);
            }

            footstepSounds();
        }

        // accurately gives animation clip time accounting for animator speed
        public float getAnimTime()  
        {
            return animator.GetCurrentAnimatorStateInfo(0).length * (animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f) * this.animator.speed;
        }

        // works by comparing the previous time step to the current time step
        // and playing a footstep sound at key animation points
        public void footstepSounds()
        {
            // walking step sounds
            if(animator.GetCurrentAnimatorStateInfo(0).IsName("WalkAnimation"))
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

        // play from a list of walk steps
        // loops through the list for variance
        public void playWalkStep()
        {
            if(walkstepIndex >= walksteps.Length) { walkstepIndex = 0; }

            walksteps[walkstepIndex].Play();

            walkstepIndex++;
        }

        public override void DoAIInterval()
        {
            if(isEnemyDead) { return; }
            base.DoAIInterval();

            // spook level always decays to 0 over time
            interestLevel = Mathf.Max(interestLevel - (interestDecay / (interestLevel + 1)), 0f);

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Sleeping:
                    sleepingState();
                    break;
                case (int)State.Patrolling:
                    patrolState();
                    break;
                case (int)State.Curious:
                    curiousState();
                    break;
                case (int)State.Fleeing:
                    fleeingState();
                    break;
                default:
                    break;
            }
        }

        // sleepingState variables
        float wakeupTime = -1;
        bool awake = false;
        public void sleepingState()
        {
            Debug.Log("Skunk Cub: sleeping... current time = " + Time.time + " wakeup time = " + wakeupTime);
            if (wakeupTime == -1)
            {
                wakeupTime = (float)(Time.time + rnd.NextDouble() * 300 + 60f);  // sleeps for up to 6 minutes, 1 minute minimum
            }

            if(Time.time > wakeupTime) { awake = true; }
            if(interestLevel > 1) 
            {
                interestLevel = 0;
                awake = true;  
            }  // woken up by sound
            
            // still asleep
            if(!awake)
            {
                Debug.Log("asleep...");
                animator.speed = 1;
                if(!animator.GetCurrentAnimatorStateInfo(0).IsName("SleepAnimation"))
                {
                    Debug.Log("sleep anim set");
                    DoAnimationClientRpc(-1);
                    animPlayClientRpc("SleepAnimation");
                }
            }
            else  // awake
            {
                Debug.Log("awake!");
                DoAnimationClientRpc(1);  // awaken animation and then idle transition

                // when finally in idle animation, transition out of sleep state
                if(animator.GetCurrentAnimatorStateInfo(0).IsName("IdleAnimation"))
                {
                    SwitchToBehaviourClientRpc((int)State.Patrolling);
                }
            }
        }

        // fleeingState variables
        float panic = 0f; // when panic hits 0 stop fleeing

        public void fleeingState()
        {
            //Debug.Log("Fleeing State");
            interestLevel = -1;
            agent.speed = 10f;
            agent.angularSpeed = 160;

            if(!audioPanic.isPlaying)
            {
                audioPanic.Play();
            }

            // cancel any searches
            if (currentSearch != null && currentSearch.inProgress) { StopSearch(currentSearch); }

            // update the center of where the cub patrols (AI node scan radius)
            updatePatrolPoint();
            // Bear cubs only check AI nodes within a certain radius of the patrolPoint
            // we will call a function to update these points every interval
            updatePatrolNodes();

            // stand, run and walk animations and speed
            if (agent.velocity.magnitude > (agent.speed / 4))
            {
                //Debug.Log("Run anim");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(3); }
                setAnimationSpeedClientRpc(agent.velocity.magnitude / runAnimationCoefficient);
            }
            else if (agent.velocity.magnitude > (agent.speed / 6))
            {
                //Debug.Log("Walk anim");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
            }
            else if (agent.velocity.magnitude <= (agent.speed / 8))
            {
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                setAnimationSpeedClientRpc(idleAnimationCoefficient);
            }



            // select a new destination
            // the destination is in the opposite direction of the nearest player
            var closestPly = getNearestPlayer();


            Vector3 awayDirection = (transform.position - closestPly.transform.position).normalized;
            Vector3 rawDestination = transform.position + awayDirection * 12f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(rawDestination, out hit, 15f, NavMesh.AllAreas))
            {
                SetDestinationToPosition(hit.position);
            }
            else
            {
                // set destination clause
                if (cubDestination == Vector3.zero)
                {
                    // select a new destination
                    cubDestination = allAINodes[rnd.Next(0, allAINodes.Length)].transform.position;
                    SetDestinationToPosition(cubDestination);
                }
                else
                {
                    // set timer to reset destination if close enough to destination
                    // same applies for a bad path
                    if (Vector3.Distance(transform.position, cubDestination) < 1.8f ||
                        agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    {
                        // idle duration set
                        if (Time.time - patrolIdleStart > 6)
                        {
                            patrolIdleDuration = (float)(rnd.NextDouble() * 5f);
                            patrolIdleStart = Time.time;
                        }

                        // idle duration end
                        if (Time.time - patrolIdleStart > patrolIdleDuration)
                        {
                            cubDestination = Vector3.zero;
                        }
                    }
                }

                Debug.Log("Could not find NavMesh point in flee direction. Defaulting to patrol destination.");
                SetDestinationToPosition(cubDestination);

            }

            // update panic value
            if (closestPly)
            {
                // cub calms down when further than 6 meters away from the closest player
                panic +=  (6 - Vector3.Distance(transform.position, closestPly.transform.position));
                panic = Math.Min(100, panic);  // panic cap
            }

            // transition out of state if panic < 0
            if(panic < 0)
            {
                cubDestination = Vector3.zero;  // should be set to this value each time
                audioPanic.Stop();
                SwitchToBehaviourClientRpc((int)State.Patrolling);
            }
        }

        public PlayerControllerB getNearestPlayer()
        {
            float lowestDist = 99999f;
            PlayerControllerB closestPlayer = null;
            foreach(PlayerControllerB ply in RoundManager.Instance.playersManager.allPlayerScripts)
            {
                var dist = Vector3.Distance(transform.position, ply.transform.position);
                if (dist < lowestDist )
                {
                    lowestDist = dist;
                    closestPlayer = ply;
                }
            }

            return closestPlayer;
        }


        // patrolState variables
        Vector3 patrolPoint = Vector3.zero;  // patrols around this point, updated by parent
        float patrolRadius = 42f;  // cubs have a pretty generous patrol radius
        private GameObject[] mapNodeReference;  // references all AI nodes on the map
        Vector3 cubDestination = Vector3.zero;  // for manually set destinations in patrol state
        float patrolIdleDuration = 1.0f;  // random duration of 0 to 5 seconds
        float patrolIdleStart = 0.0f;  // time marker for idle animation

        public void patrolState()
        {
            //Debug.Log("Patrol State");
            agent.speed = 6f;
            agent.angularSpeed = 160;

            // cancel any searches
            if (currentSearch != null && currentSearch.inProgress) { StopSearch(currentSearch); }

            // update the center of where the cub patrols (AI node scan radius)
            updatePatrolPoint();
            // Bear cubs only check AI nodes within a certain radius of the patrolPoint
            // we will call a function to update these points every interval
            updatePatrolNodes();

            // stand and walk animations and speed
            if (agent.velocity.magnitude > (agent.speed / 4))
            {
                //Debug.Log("Walk Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
            }
            else if (agent.velocity.magnitude <= (agent.speed / 8))
            {
                //Debug.Log("Idle Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                setAnimationSpeedClientRpc(idleAnimationCoefficient);
            }

            // set destination clause
            if(cubDestination == Vector3.zero)
            {
                // select a new destination (with random navmesh and ai node sampling)
                Vector3 basePoint = allAINodes[rnd.Next(0, allAINodes.Length)].transform.position;
                Vector2 randomOffset2D = UnityEngine.Random.insideUnitCircle * 6f;  // 6f is sample radius
                Vector3 randomPoint = basePoint + new Vector3(randomOffset2D.x, 0, randomOffset2D.y);

                // Optionally sample the NavMesh to ensure it's valid
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 8f, NavMesh.AllAreas))
                {
                    cubDestination = hit.position;
                    SetDestinationToPosition(cubDestination);
                }
                else
                {
                    // fallback to node position
                    cubDestination = basePoint;
                    SetDestinationToPosition(basePoint);
                }
            }
            else
            {
                // set timer to reset destination if close enough to destination
                // same applies for a bad path
                if(Vector3.Distance(transform.position, cubDestination) < 1.8f || 
                    agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    // idle duration set
                    if (Time.time - patrolIdleStart > 6)
                    {
                        patrolIdleDuration = (float)(rnd.NextDouble() * 5f);
                        patrolIdleStart = Time.time;
                    }

                    // idle duration end
                    if(Time.time - patrolIdleStart > patrolIdleDuration)
                    {
                        cubDestination = Vector3.zero;
                    }
                }
            }

            // Curious State transition
            // can't get curious with players it doesn't like
            if(getNearPlayer() && interestLevel > 0.65f)
            {
                spooked = false;
                SwitchToBehaviourClientRpc((int)State.Curious);
            }
        }

        // curiousState variable
        PlayerControllerB playerTarget = null;
        float curiousRadius = 7.5f;  // cub loses interest past this radius
        float curiousStopRadius = 2.5f;  // cub stops at this radius
        float interestMultiplier = 4f;  // scalar
        float interestDecay = 0.25f;
        float interestLevel = 0f;  // builds up, decays very slowly at high values
        bool spooked = false;  // if spooked run away!
        float spookSoftNoiseThreshold = 0.23f;
        float spookLoudNoiseThreshold = 0.6f;

        // continued, has to do with straggling around the player for a short while
        // once the cub has gotten close enough for some time
        float proximityCooloff = 0.0f;  // at 10 cooloff, straggle around the player
        bool proximityCooling = false;  // if "cooling" continue straggling, stops at 0 cooloff
        float curiousHeatrate = 2.1f;  // how fast we cycle to straggling
        float curiousCoolrate = 0.045f; // how fast we cycle to easing towards the player

        // the cub noticed a player!
        // The cub will get closer and closer, slowing down as it approaches the player
        // it will stop and stare at the player, growling at them periodically (soft growl)
        // The cub will transition into fleeing if the player is too loud (loudness threshold increases with distance?)
        public void curiousState()
        {
            //Debug.Log("Cub: Curious State");
            playerTarget = getNearPlayer();
            if(!playerTarget)
            {
                cubDestination = Vector3.zero;  // should be set to this value each time
                SwitchToBehaviourClientRpc((int)State.Patrolling);
                return;
            }

            // purr noise
            if (!audioPurr.isPlaying)
            {
                audioPurr.Play();
            }

            // stand and walk animations and speed
            if (agent.velocity.magnitude > (agent.speed / 4))
            {
                //Debug.Log("Walk Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(2); }
                setAnimationSpeedClientRpc(agent.velocity.magnitude / walkAnimationCoefficient);
            }
            else if (agent.velocity.magnitude <= (agent.speed / 8))
            {
                //Debug.Log("Idle Anim Set");
                if (RoundManager.Instance.IsServer) { DoAnimationClientRpc(1); }
                setAnimationSpeedClientRpc(idleAnimationCoefficient);
            }

            // MOVEMENT LOGIC
            if (proximityCooling == false)
            {
                SetDestinationToPosition(playerTarget.transform.position);
                //Debug.Log("Easing... " + proximityCooloff);
                cubDestination = Vector3.zero;
                // curious heat
                proximityCooloff += curiousHeatrate / Vector3.Distance(transform.position, playerTarget.transform.position);

                // movement easing and stop at player
                if (Vector3.Distance(playerTarget.transform.position, transform.position) < curiousStopRadius)
                {
                    //Debug.Log("Cub: Stopped");
                    agent.speed = 0f;
                }
                else
                {
                    //Debug.Log("Cub: Curious easing");
                    agent.speed = (Vector3.Distance(playerTarget.transform.position, transform.position) - curiousStopRadius) / 3;  // distance based easing
                }
            }
            else
            {   // straggle around player
                // curious cooling
                proximityCooloff -= curiousCoolrate * Vector3.Distance(transform.position, playerTarget.transform.position);
                agent.speed = 6f;

                //Debug.Log("Straggling... " + proximityCooloff);

                if (cubDestination == Vector3.zero)
                {
                    Vector3 randomVector = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                    ) * 12f;

                    Vector3 rawDestination = playerTarget.transform.position + randomVector;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(rawDestination, out hit, 15f, NavMesh.AllAreas))
                    {
                        SetDestinationToPosition(hit.position);
                    }
                }

                if(Vector3.Distance(transform.position, cubDestination) < 0.5f)
                {
                    cubDestination = Vector3.zero;
                }
            }

            // proximity cooling flag
            if(proximityCooloff > 10) { proximityCooling = true;  }
            
            if(proximityCooloff < 0) { proximityCooling = false; }

            // get spooked if the player is too loud (spookLevel gets too high)
            if (spooked)
            {
                interestLevel = 0;
                // Flee!
               // Debug.Log("Cub: Too spooky, Run Away!");
                cubDestination = Vector3.zero;  // should be set to this value each time
                spooked = false;
                panic = 100f;
                SwitchToBehaviourClientRpc((int)State.Fleeing);
            }
            else if (interestLevel > 0.5f)
            {
                // stay interested
                // maybe approach a bit closer?
                //Debug.Log("Cub: Interest maintained");
            }
            else
            {
                // Disinterested
                //Debug.Log("Cub: Disinterested, back to patrolling.");
                cubDestination = Vector3.zero;  // should be set to this value each time
                SwitchToBehaviourClientRpc((int)State.Patrolling);
                spooked = false;
            }
        }

        public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0)
        {
            base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
            // noise ID 6 is a player step!
            if (stunNormalizedTimer > 0f || noiseID == 7 || noiseID == 546 || noiseID == 6) return;

            float distance = Vector3.Distance(transform.position, noisePosition);
            if (distance > 25f) return;

            // maintain curiosity gently if sound is soft-moderate
            if (noiseLoudness >= spookSoftNoiseThreshold)
            {
                interestLevel = Mathf.Max(interestLevel + 0.15f * interestMultiplier, 6f); // Soft cap for curiosity
            }

            // if too loud, spike to max
            if (noiseLoudness > spookLoudNoiseThreshold)
            {
                spooked = true;
            }

            //Debug.Log($"Cub: Heard sound at {distance:F1}m, loud={noiseLoudness:F2}, interest={interestLevel:F2}");
        }

        public void facePosition(Vector3 pos)
        {
            Vector3 directionToTarget = pos - transform.position;
            directionToTarget.y = 0f; // Ignore vertical difference

            // If directionToTarget is not zero, rotate to face target
            if (directionToTarget != Vector3.zero)
            {
                // Calculate the rotation to face the target only in the Y-axis (yaw)
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

                // Apply the rotation to the object's transform, preserving current pitch and roll
                transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            }
        }

        // Get a nearby player given the curiousRadius
        // curiousRadius distance threshold (7.5f)
        // scales with interest
        public PlayerControllerB getNearPlayer()
        {
            if(playerTarget != null && Vector3.Distance(playerTarget.transform.position, transform.position) < curiousRadius * interestLevel) { return playerTarget; }

            var m = RoundManager.Instance;
            foreach(PlayerControllerB player in m.playersManager.allPlayerScripts)
            {
                if(Vector3.Distance(player.transform.position, transform.position) < curiousRadius * interestLevel)
                {
                    return player;
                }

            }

            playerTarget = null;

            return null;
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
            Debug.Log("Cub: Ouch, Run Away!");
            awake = true;
            cubDestination = Vector3.zero;  // should be set to this value each time
            spooked = false;
            panic = 100f;
            SwitchToBehaviourClientRpc((int)State.Fleeing);
            audioHit.Play();

            if(enemyHP <= 0)
            {
                isEnemyDead = true;
                animPlayClientRpc("DeathAnimation");
            }
        }


        public void updatePatrolPoint()
        {
            if (mommaBear && !mommaBear.isEnemyDead)
            {
                patrolPoint = mommaBear.transform.position;
            }
            else
            {
                // assign point to self (wandering mode)
                patrolPoint = transform.position;
            }

        }

        // update the allowed patrol nodes with map nodes that fit the
        // patrol radius. Additionally add random nodes if the quantity
        // of nodes is lacking.
        public void updatePatrolNodes()
        {
            List<GameObject> selectedNodes = new List<GameObject>();
            foreach(GameObject node in mapNodeReference)
            {
                if (Vector3.Distance(patrolPoint, node.transform.position) <= patrolRadius)
                {
                    selectedNodes.Add(node);
                }
            }

            allAINodes = selectedNodes.ToArray();
        }

        [ClientRpc]
        public void setAnimationSpeedClientRpc(float speed)
        {
            this.animator.speed = speed;
        }

        [ClientRpc]
        // includes animation transitions
        public void DoAnimationClientRpc(int index)
        {
            if (this.animator) { this.animator.SetInteger("State", index); }
        }


        // directly play an animation
        [ClientRpc]
        public void animPlayClientRpc(String name)
        {
            animator.Play(name);
        }
    }
}