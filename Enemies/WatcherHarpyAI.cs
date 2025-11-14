using System.Linq;
using UnityEngine;

namespace LethalFauna.Enemies
{
    class WatcherHarpyAI : EnemyAI
    {
        public GameObject rotationObj;

        GameObject targetTree;
        GameObject localRotObj;

        bool changeTree;

        enum State
        {
            Traveling,
            Circling,
            Perched,
            Attacking
        }

        public override void Start()
        {
            base.Start();

            // Start in travel state
            currentBehaviourStateIndex = (int)State.Traveling;
        }

        public override void Update()
        {
            base.Update();

            // If in the circling state, then rotate the rotation object
            if (currentBehaviourStateIndex == (int)State.Circling)
            {
                localRotObj.transform.localEulerAngles += new Vector3(0, 0.1f, 0) * Time.deltaTime;
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Traveling:
                    TravelState();
                    break;

                case (int)State.Circling:
                    CircleState();
                    break;

                default:
                    break;
            }
        }

        public void CircleState()
        {

        }

        public void TravelState()
        {
            // If the target tree needs to change then do so
            if (changeTree)
            {
                // If a target tree was chosen before, destroy the rotation object first
                if (localRotObj != null)
                    Destroy(localRotObj);

                // Find a tree to target
                GameObject[] usableTrees = GameObject.FindGameObjectsWithTag("Wood").Where(x => x.name.StartsWith("tree")).ToArray();
                if (usableTrees.Length == 0)
                {
                    // Spawn a tree
                }
                else
                {
                    targetTree = usableTrees[Random.Range(0, usableTrees.Length)];
                    localRotObj = Instantiate(rotationObj, targetTree.transform);
                }

                changeTree = false;
            }

            // Set destination to the target tree's rotation point
            Vector3 rotPoint = localRotObj.transform.GetChild(0).position;
            destination = rotPoint;
            moveTowardsDestination = true;

            // Reached destination, begin circling
            if (Vector3.Distance(transform.position, rotPoint) < 0.1f)
            {
                moveTowardsDestination = false;
                currentBehaviourStateIndex = (int)State.Circling;
            }
        }
    }
}
