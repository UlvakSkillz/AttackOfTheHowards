using Il2CppRUMBLE.Environment.Howard;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players.Subsystems;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace AttackOfTheHowards
{

    [RegisterTypeInIl2Cpp]
    public class HowardCombat : MonoBehaviour
    {
        private GameObject thisHoward;
        private GameObject playerHead;
        private Howard thisHowardComponent;
        private HowardAnimator thisHowardAnimator;
        private GameObject thisDummy;
        private float moveSpeed = 5f;
        private float turnSpeed = 3.6f;
        private float attackDistance = 20;
        private float stopMovingCloserDistance = 3;
        public int mode = 0; //0 = idle, 1 = move, 2 = attack, 3 = death
        private bool attacking = false;
        private List<Attack> attacks = new List<Attack>();
        private bool attackRunning = false;
        private float lastKnownAngleToPlayer;

        private class Attack
        {
            public string name;
            public IEnumerator doMoves;
            public float minDistanceToUse;
            public float maxDistanceToUse;

            public Attack(string name, float minDistance, float maxDistance, IEnumerator moves)
            {
                this.name = name;
                doMoves = moves;
                minDistanceToUse = minDistance;
                maxDistanceToUse = maxDistance;
            }

            public bool IsWithinRange(float distance)
            {
                return ((minDistanceToUse <= distance) && (distance <= maxDistanceToUse));
            }
        }

        void Start()
        {
            thisHoward = this.gameObject;
            playerHead = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(1).GetChild(0).GetChild(0).gameObject;
            thisHowardComponent = thisHoward.GetComponent<Howard>();
            thisDummy = thisHoward.transform.GetChild(2).gameObject;
            thisHowardAnimator = thisDummy.transform.GetChild(0).GetComponent<HowardAnimator>();
            MelonCoroutines.Start(UpdateMode());
            SetupAttacks();
            MelonCoroutines.Start(RunSetup());
        }

        private IEnumerator RunSetup()
        {
            try
            {
                thisHowardComponent.SetHowardLogicActive(true);
            }
            catch { yield break; }
            yield return new WaitForSeconds(3.5f);
            try
            {
                Transform dummy = thisHowardAnimator.gameObject.transform.parent;
                dummy.localPosition = Vector3.zero;
                thisHoward.transform.position = new Vector3(thisHoward.transform.position.x, 0, thisHoward.transform.position.z);
            }
            catch { yield break; }
            yield break;
        }

        private void SetupAttacks()
        {
            if (Main.availableStacks[0] != null)
            {
                attacks.Add(new Attack("d", 5, 10, CastDisc()));
            }
            if ((Main.availableStacks[0] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("dS", 7, 20, CastDiscStraight()));
            }
            if ((Main.availableStacks[1] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[6] != null))
            {
                attacks.Add(new Attack("ppSSU", 8, 10, CastPillarPillarStraightStraightUppercut()));
            }
            if ((Main.availableStacks[2] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("bS", 2, 10, CastBallStraight()));
            }
            if ((Main.availableStacks[3] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[6] != null))
            {
                attacks.Add(new Attack("cSU", 0, 15, CastCubeStraightUppercut()));
            }
            if ((Main.availableStacks[4] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("wS", 0, 4, CastWallStraight()));
            }
            if ((Main.availableStacks[4] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[7] != null))
            {
                attacks.Add(new Attack("wSU", 4, 8, CastWallStraightUppercut()));
            }
            if ((Main.availableStacks[4] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[6] != null) && (Main.availableStacks[7] != null))
            {
                attacks.Add(new Attack("wSUK", 10, 15, CastWallStraightUppercutKick()));
            }
        }

        private IEnumerator UpdateMode()
        {
            while ((thisHoward != null) && (mode != 3))
            {
                float distanceToPlayer = Vector3.Distance(thisHoward.transform.position, playerHead.transform.position);
                if (distanceToPlayer > Main.howardVisualDistance)
                {
                    mode = 0;
                }
                else if (distanceToPlayer <= stopMovingCloserDistance)
                {
                    if (lastKnownAngleToPlayer < 5f)
                    {
                        mode = 2;
                    }
                    else
                    {
                        mode = 0;
                    }
                }
                else if (distanceToPlayer <= attackDistance)
                {
                    if (lastKnownAngleToPlayer < 5f)
                    {
                        mode = Main.random.Next(1, 3);
                    }
                    else
                    {
                        mode = 0;
                    }
                }
                else
                {
                    mode = 1;
                }
                yield return new WaitForSeconds(0.25f);
            }
            yield break;
        }

        void FixedUpdate()
        {
            if ((thisHowardComponent.activeLogicRoutine == null) && (thisHowardComponent.activeRegenRoutine == null))
            {
                thisHowardComponent.StartCurrentLogicLevel();
                if ((PlayerManager.instance.localPlayer.Controller.gameObject.transform.position.y < 0.5f) && (PlayerManager.instance.localPlayer.Controller.GetComponent<PlayerMovement>().activeMovementType == PlayerMovement.MovementType.AirealKnockback))
                {
                    PlayerManager.instance.localPlayer.Controller.GetComponent<PlayerMovement>().activeMovementType = PlayerMovement.MovementType.Normal;
                }
            }
            if (!attacking)
            {
                UpdateRotation();
                switch (mode)
                {
                    case 0:
                        break;
                    case 1:
                        UpdatePosition();
                        break;
                    case 2:
                        MelonCoroutines.Start(StartAttack());
                        break;
                    case 3:
                        break;
                }
            }
        }

        private IEnumerator StartAttack()
        {
            attacking = true;
            Vector2 howardXZ = new Vector2(thisHoward.transform.position.x, thisHoward.transform.position.z);
            Vector2 playerXZ = new Vector2(playerHead.transform.position.x, playerHead.transform.position.z);
            float distance = Vector2.Distance(playerXZ, howardXZ);
            List<Attack> usableAttacks = new List<Attack>();
            int i = 0;
            foreach (Attack attack in attacks)
            {
                if (attack.IsWithinRange(distance))
                {
                    usableAttacks.Add(attack);
                }
                i++;
            }
            if (usableAttacks.Count > 0)
            {
                int chosenAttack = Main.random.Next(usableAttacks.Count);
                MelonCoroutines.Start(usableAttacks[chosenAttack].doMoves);
                while (attackRunning)
                {
                    yield return new WaitForFixedUpdate();
                }
                switch (usableAttacks[chosenAttack].name)
                {
                    case "d":
                        usableAttacks[chosenAttack].doMoves = CastDisc();
                        break;
                    case "dS":
                        usableAttacks[chosenAttack].doMoves = CastDiscStraight();
                        break;
                    case "ppSSU":
                        usableAttacks[chosenAttack].doMoves = CastPillarPillarStraightStraightUppercut();
                        break;
                    case "bS":
                        usableAttacks[chosenAttack].doMoves = CastBallStraight();
                        break;
                    case "cSU":
                        usableAttacks[chosenAttack].doMoves = CastCubeStraightUppercut();
                        break;
                    case "wS":
                        usableAttacks[chosenAttack].doMoves = CastWallStraight();
                        break;
                    case "wSU":
                        usableAttacks[chosenAttack].doMoves = CastWallStraightUppercut();
                        break;
                    case "wSUK":
                        usableAttacks[chosenAttack].doMoves = CastWallStraightUppercutKick();
                        break;
                }
            }
            attacking = false;
            yield break;
        }

        private IEnumerator CastDisc()
        {
            //Disc
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[0]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.3f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastDiscStraight()
        {
            //Disc
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[0]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.3f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastPillarPillarStraightStraightUppercut()
        {
            //Pillar
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[1]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.4f);
            //Pillar
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[1]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.3f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            attackRunning = true;
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastBallStraight()
        {
            //Ball
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[2]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastCubeStraightUppercut()
        {
            //Cube
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[3]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.4f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
                //Uppercut
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.5f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastWallStraight()
        {
            //Wall
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[4]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.5f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastWallStraightUppercut()
        {
            //Wall
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[4]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.5f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
                //Uppercut
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastWallStraightUppercutKick()
        {
            //Wall
            attackRunning = true;
            try
            {
                thisHowardAnimator.SetAnimationTrigger("SpawnStructure");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {

                thisHowardComponent.Execute(Main.availableStacks[4]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.5f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
                //Uppercut
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private void UpdateRotation()
        {
            //Grab Vector2 Angle (X/Z) to look at player from current rotation
            Vector3 thisHowardNormalized = thisDummy.transform.forward.normalized;
            Vector2 forwardVector = new Vector2(thisHowardNormalized.x, thisHowardNormalized.z);
            Vector3 directionNormalized = (playerHead.transform.position - thisDummy.transform.position).normalized;
            Vector2 directionToObject = new Vector2(directionNormalized.x, directionNormalized.z);
            float dotProduct = Vector2.Dot(forwardVector, directionToObject);
            float targetAngle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg; // Convert radians to degrees
            //find how far to turn
            float turnAmount;
            if (targetAngle <= turnSpeed)
            {
                turnAmount = targetAngle;
            }
            else
            {
                turnAmount = turnSpeed;
            }
            //checks if player is on howards left to turn left instead of right
            Vector3 thisHowardRight = thisDummy.transform.right;
            Vector2 rightVector = new Vector2(thisHowardRight.x, thisHowardRight.z);
            float dotProduct2 = Vector2.Dot(rightVector, directionToObject);
            if (dotProduct2 < 0)
            {
                turnAmount *= -1;
            }
            lastKnownAngleToPlayer = targetAngle - Math.Abs(turnAmount);
            //update rotation
            thisHoward.transform.localRotation = Quaternion.Euler(0, thisHoward.transform.localRotation.eulerAngles.y + turnAmount, 0);
        }

        private void UpdatePosition()
        {
            float distanceToPlayer = Vector3.Distance(thisHoward.transform.position, playerHead.transform.position);
            if (distanceToPlayer > stopMovingCloserDistance)
            {
                thisHoward.transform.position += thisDummy.transform.forward * moveSpeed * Time.deltaTime;
            }
            thisDummy.transform.localPosition = Vector3.zero;
        }

        public void RunDeath()
        {
            mode = 3;
            MelonCoroutines.Start(Dying());
        }

        private IEnumerator Dying()
        {
            float sinkSpeed = 0.08f;
            for(int i = 0; i < 25; i++)
            {
                thisHoward.transform.position = new Vector3(thisHoward.transform.position.x, thisHoward.transform.position.y - sinkSpeed, thisHoward.transform.position.z);
                yield return new WaitForFixedUpdate();
            }
            MelonCoroutines.Start(SpawnOnDeath());
            yield break;
        }

        private IEnumerator SpawnOnDeath()
        {
            int respawnCount = (Main.duplicateHoward ? 2 : 1);
            int mapSize = (int)FlatLand.main.FlatLand.Settings[0].SavedValue;
            float edgeLength = (((float)mapSize) / 2) - 1;
            for (int i = 0; i < respawnCount; i++)
            {
                Main.SpawnNewHoward();
                yield return new WaitForFixedUpdate();
            }
            Main.activeHowards.Remove(thisHoward);
            GameObject.DestroyImmediate(thisHoward);
            yield break;
        }
    }
}
