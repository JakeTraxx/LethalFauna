using UnityEngine;

namespace LethalFauna.Enemies
{
    class WatcherHarpyAI : EnemyAI
    {
        public Transform harpyHead;

        enum State
        {
            GoingToBranch,
            LookForPrey,
            AttackPrey
        }

        public override void Start()
        {
            base.Start();

            currentBehaviourStateIndex = (int)State.LookForPrey;
            transform.position += new Vector3(0, 5, 0);
        }

        public override void Update()
        {
            base.Update();
            
            if (targetPlayer != null && !targetPlayer.isPlayerDead)
            {
                if (PreyUnderMe())
                {
                    //SwitchToBehaviourClientRpc((int)State.AttackPrey);
                    targetPlayer.DamagePlayer(100);
                }
                else
                    harpyHead.LookAt(targetPlayer.gameplayCamera.transform.position);
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            switch (currentBehaviourStateIndex)
            {
                case (int)State.LookForPrey:
                    TargetClosestPlayer();
                    break;

                default:
                    break;
            }
        }

        private bool PreyUnderMe()
        {
            float[] xBounds = { transform.position.x - 5, transform.position.x + 5 };
            float[] zBounds = { transform.position.z - 5, transform.position.z + 5 };
            if (targetPlayer.transform.position.x > xBounds[0] && targetPlayer.transform.position.x < xBounds[1])
                if (targetPlayer.transform.position.z > zBounds[0] && targetPlayer.transform.position.z < zBounds[1])
                    if (targetPlayer.transform.position.y < transform.position.y)
                        return true;
            return false;
        }
    }
}
