using Il2CppRUMBLE.Environment.Howard;
using Il2CppRUMBLE.Managers;
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
        private float turnSpeed = 7.2f;
        private float attackDistance = 20;
        private float stopMovingCloserDistance = 3;
        private float dontGroupDistance = 4f;
        public int mode = 0; //0 = idle, 1 = move, 2 = attack, 3 = death, 4 = sideStep
        private bool attacking = false;
        private List<Attack> attacks = new List<Attack>();
        private bool attackRunning = false;
        private bool initDone = false;
        private float lastAngleToPlayer = 0;
        private bool canSeePlayer = true;
        private bool sideStepping = false;

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

        void FixedUpdate()
        {
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
                    default:
                        break;
                }
            }
        }

        private IEnumerator RunSetup()
        {
            try
            {
                thisHowardComponent.SetHowardLogicActive(true);
            }
            catch { yield break; }
            yield return new WaitForSeconds(4f);
            try
            {
                thisHoward.transform.position = new Vector3(thisHoward.transform.position.x, 0, thisHoward.transform.position.z);
                initDone = true;
            }
            catch { yield break; }
            yield break;
        }

        private void SetupAttacks()
        {
            // Summon
            if (Main.availableStacks[0] != null)
            {
                attacks.Add(new Attack("d", 2, 10, CastDisc()));
            }
            else
            {
                Main.Log($"Disk: {Main.availableStacks[0] != null}");
            }
            // Straight
            if ((Main.availableStacks[0] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("dS", 7, 20, CastDiscStraight()));
            }
            else
            {
                Main.Log($"Disk: {Main.availableStacks[0] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[2] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("bS", 2, 10, CastBallStraight()));
            }
            else
            {
                Main.Log($"Ball: {Main.availableStacks[2] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[4] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("wS", 0, 4, CastWallStraight()));
            }
            else
            {
                Main.Log($"Wall: {Main.availableStacks[4] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[3] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("cS", 0, 5, CastCubeStraight()));
            }
            else
            {
                Main.Log($"Cube: {Main.availableStacks[3] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[1] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("pS", 0, 6, CastPillarStraight()));
            }
            else
            {
                Main.Log($"Pillar: {Main.availableStacks[1] != null} Straight: {Main.availableStacks[5] != null}");
            }
            // Straight Uppercut
            if ((Main.availableStacks[4] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[7] != null))
            {
                attacks.Add(new Attack("wSU", 4, 8, CastWallStraightUppercut()));
            }
            else
            {
                Main.Log($"Wall: {Main.availableStacks[4] != null} Straight: {Main.availableStacks[5] != null} Uppercut: {Main.availableStacks[6] != null}");
            }
            if ((Main.availableStacks[3] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[6] != null))
            {
                attacks.Add(new Attack("cSU", 0, 15, CastCubeStraightUppercut()));
            }
            else
            {
                Main.Log($"Cube: {Main.availableStacks[3] != null} Straight: {Main.availableStacks[5] != null} Uppercut: {Main.availableStacks[6] != null}");
            }
            if ((Main.availableStacks[1] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[6] != null))
            {
                attacks.Add(new Attack("pSU", 0, 8, CastPillarStraightUppercut()));
            }
            else
            {
                Main.Log($"Pillar: {Main.availableStacks[1] != null} Straight: {Main.availableStacks[5] != null} Uppercut: {Main.availableStacks[6] != null}");
            }
            // Uppercut Straight
            if ((Main.availableStacks[2] != null) && (Main.availableStacks[6] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("bUS", 5, 14, CastBallUppercutStraight()));
            }
            else
            {
                Main.Log($"Ball: {Main.availableStacks[2] != null} Uppercut: {Main.availableStacks[6] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[4] != null) && (Main.availableStacks[7] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("wUS", 3, 7, CastWallUppercutStraight()));
            }
            else
            {
                Main.Log($"Wall: {Main.availableStacks[4] != null} Uppercut: {Main.availableStacks[6] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[3] != null) && (Main.availableStacks[6] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("cUS", 0, 10, CastCubeUppercutStraight()));
            }
            else
            {
                Main.Log($"Cube: {Main.availableStacks[3] != null} Uppercut: {Main.availableStacks[6] != null} Straight: {Main.availableStacks[5] != null}");
            }
            if ((Main.availableStacks[1] != null) && (Main.availableStacks[6] != null) && (Main.availableStacks[5] != null))
            {
                attacks.Add(new Attack("pUS", 0, 7, CastPillarUppercutStraight()));
            }
            else
            {
                Main.Log($"Pillar: {Main.availableStacks[1] != null} Uppercut: {Main.availableStacks[6] != null} Straight: {Main.availableStacks[5] != null}");
            }
            //Other
            if ((Main.availableStacks[1] != null) && (Main.availableStacks[5] != null) && (Main.availableStacks[6] != null))
            {
                attacks.Add(new Attack("ppSSU", 8, 10, CastPillarPillarStraightStraightUppercut()));
            }
            else
            {
                Main.Log($"ppSUK: Pillar: {Main.availableStacks[1] != null} Straight: {Main.availableStacks[5] != null} Uppercut: {Main.availableStacks[6] != null}");
            }
            if ((Main.availableStacks[5] != null) && (Main.availableStacks[6] != null) && (Main.availableStacks[7] != null))
            {
                if (Main.availableStacks[4] != null)
                {
                    attacks.Add(new Attack("wSUK", 10, 14, CastWallStraightUppercutKick()));
                    attacks.Add(new Attack("wUKS", 9, 13, CastWallUppercutKickStraight()));
                    attacks.Add(new Attack("wUSK", 8, 13, CastWallUppercutStraightKick()));
                    attacks.Add(new Attack("wKUS", 8, 12, CastWallKickUppercutStraight()));
                    attacks.Add(new Attack("wKSU", 8, 12, CastWallKickStraightUppercut()));
                }
                else
                {
                    Main.Log($"Wall: {Main.availableStacks[4] != null}");
                }
                if (Main.availableStacks[3] != null)
                {
                    attacks.Add(new Attack("cSUK", 15, 18, CastCubeStraightUppercutKick()));
                    attacks.Add(new Attack("cUKS", 14, 16, CastCubeUppercutKickStraight()));
                    attacks.Add(new Attack("cUSK", 13, 17, CastCubeUppercutStraightKick()));
                    attacks.Add(new Attack("cKUS", 11, 13, CastCubeKickUppercutStraight()));
                    attacks.Add(new Attack("cKSU", 12, 15, CastCubeKickStraightUppercut()));
                }
                else
                {
                    Main.Log($"Cube: {Main.availableStacks[3] != null}");
                }
                if (Main.availableStacks[1] != null)
                {
                    attacks.Add(new Attack("pSUK", 15, 20, CastPillarStraightUppercutKick()));
                    attacks.Add(new Attack("pUKS", 15, 20, CastPillarUppercutKickStraight()));
                    attacks.Add(new Attack("pUSK", 15, 20, CastPillarUppercutStraightKick()));
                    attacks.Add(new Attack("pKUS", 12, 16, CastPillarKickUppercutStraight()));
                    attacks.Add(new Attack("pKSU", 13, 18, CastPillarKickStraightUppercut()));
                }
                else
                {
                    Main.Log($"Pillar: {Main.availableStacks[1] != null}");
                }
            }
            else
            {
                Main.Log($"3-Hit Check | Straight: {Main.availableStacks[5] != null} Uppercut: {Main.availableStacks[6] != null} Kick: {Main.availableStacks[7] != null}");
            }
        }

        private IEnumerator UpdateMode()
        {
            while ((thisHoward != null) && (mode != 3))
            {
                if ((initDone) && (mode != 4))
                {
                    RaycastCheck();
                    if (mode != 4)
                    {
                        Vector2 howardXZ = Main.V3ToV2XZ(thisDummy.transform.position);
                        Vector2 playerXZ = Main.V3ToV2XZ(playerHead.transform.position);
                        float distanceToPlayer = Vector2.Distance(howardXZ, playerXZ);
                        if (distanceToPlayer > Main.howardVisualDistance)
                        {
                            mode = 0;
                        }
                        else if (distanceToPlayer <= stopMovingCloserDistance)
                        {
                            if (canSeePlayer && lastAngleToPlayer == 0)
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
                            moveSpeed = 3.6f;
                            if (canSeePlayer && lastAngleToPlayer == 0)
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
                            moveSpeed = 5f;
                            mode = 1;
                        }
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }
            yield break;
        }

        private void RaycastCheck()
        {
            if ((mode == 2) || (lastAngleToPlayer > 0)) { return; }
            Vector3 rayStart = thisDummy.transform.position + thisDummy.transform.forward;
            Vector3 dummy = new Vector3(rayStart.x, rayStart.y + 1.5f, rayStart.z);
            Vector3 direction = (playerHead.transform.position - thisDummy.transform.position).normalized;
            RaycastHit hit;
            if (Physics.Raycast(dummy, direction, out hit, Vector3.Distance(dummy, playerHead.transform.position)))
            {
                try
                {
                    if (!sideStepping && (hit.transform.gameObject.name != "Hitboxes") && (!hit.transform.gameObject.name.ToLower().Contains("bone")))
                    {
                        canSeePlayer = false;
                        mode = 4;
                        sideStepping = true;
                        MelonCoroutines.Start(MoveTillSeePlayer());
                    }
                    else
                    {
                        canSeePlayer = true;
                    }
                }
                catch { }
            }
        }

        private IEnumerator MoveTillSeePlayer()
        {
            bool dontSeePlayer = true;
            int moveLeftVsRight = (Main.random.Next(0, 2) == 0 ? -1 : 1);
            while (dontSeePlayer && (thisHoward != null) && (mode != 3))
            {
                thisHoward.transform.position += thisDummy.transform.right * moveLeftVsRight * moveSpeed * Time.deltaTime / 2;
                Vector3 rayStart = thisDummy.transform.position + thisDummy.transform.forward;
                Vector3 dummy = new Vector3(rayStart.x, rayStart.y + 1.5f, rayStart.z);
                Vector3 direction = (playerHead.transform.position - thisDummy.transform.position).normalized;
                RaycastHit hit;
                dontSeePlayer = Physics.Raycast(dummy, direction, out hit, Vector3.Distance(dummy, playerHead.transform.position));
                if (dontSeePlayer)
                {
                    try
                    {
                        if ((hit.transform.gameObject.name == "Hitboxes") || (hit.transform.gameObject.name.ToLower().Contains("bone")))
                        {
                            dontSeePlayer = false;
                        }
                    }
                    catch { }
                }
                yield return new WaitForFixedUpdate();
            }
            canSeePlayer = !dontSeePlayer;
            if (mode != 3) { mode = 0; }
            UpdateRotation();
            sideStepping = false;
            yield break;
        }

        private IEnumerator StartAttack()
        {
            attacking = true;
            Vector2 howardXZ = Main.V3ToV2XZ(thisHoward.transform.position);
            Vector2 playerXZ = Main.V3ToV2XZ(playerHead.transform.position);
            float distance = Vector2.Distance(playerXZ, howardXZ);
            List<Attack> usableAttacks = new List<Attack>();
            foreach (Attack attack in attacks)
            {
                if (attack.IsWithinRange(distance))
                {
                    usableAttacks.Add(attack);
                }
            }
            if (usableAttacks.Count > 0)
            {
                int chosenAttack = Main.random.Next(usableAttacks.Count);
                var attackCoroutine = MelonCoroutines.Start(usableAttacks[chosenAttack].doMoves);
                while (attackRunning && (mode != 3))
                {
                    yield return new WaitForFixedUpdate();
                }
                if (mode == 3)
                {
                    MelonCoroutines.Stop(attackCoroutine);
                    attackRunning = false;
                }
                switch (usableAttacks[chosenAttack].name)
                {
                    case "d":
                        usableAttacks[chosenAttack].doMoves = CastDisc();
                        break;
                    case "dS":
                        usableAttacks[chosenAttack].doMoves = CastDiscStraight();
                        break;
                    case "pS":
                        usableAttacks[chosenAttack].doMoves = CastPillarStraight();
                        break;
                    case "pSU":
                        usableAttacks[chosenAttack].doMoves = CastPillarStraightUppercut();
                        break;
                    case "pUS":
                        usableAttacks[chosenAttack].doMoves = CastPillarUppercutStraight();
                        break;
                    case "ppSSU":
                        usableAttacks[chosenAttack].doMoves = CastPillarPillarStraightStraightUppercut();
                        break;
                    case "bS":
                        usableAttacks[chosenAttack].doMoves = CastBallStraight();
                        break;
                    case "bUS":
                        usableAttacks[chosenAttack].doMoves = CastBallUppercutStraight();
                        break;
                    case "cS":
                        usableAttacks[chosenAttack].doMoves = CastCubeStraight();
                        break;
                    case "cSU":
                        usableAttacks[chosenAttack].doMoves = CastCubeStraightUppercut();
                        break;
                    case "cUS":
                        usableAttacks[chosenAttack].doMoves = CastCubeUppercutStraight();
                        break;
                    case "wS":
                        usableAttacks[chosenAttack].doMoves = CastWallStraight();
                        break;
                    case "wSU":
                        usableAttacks[chosenAttack].doMoves = CastWallStraightUppercut();
                        break;
                    case "wUS":
                        usableAttacks[chosenAttack].doMoves = CastWallUppercutStraight();
                        break;
                    case "wSUK":
                        usableAttacks[chosenAttack].doMoves = CastWallStraightUppercutKick();
                        break;
                    case "cSUK":
                        usableAttacks[chosenAttack].doMoves = CastCubeStraightUppercutKick();
                        break;
                    case "pSUK":
                        usableAttacks[chosenAttack].doMoves = CastPillarStraightUppercutKick();
                        break;
                    case "wUKS":
                        usableAttacks[chosenAttack].doMoves = CastWallUppercutKickStraight();
                        break;
                    case "cUKS":
                        usableAttacks[chosenAttack].doMoves = CastCubeUppercutKickStraight();
                        break;
                    case "pUKS":
                        usableAttacks[chosenAttack].doMoves = CastPillarUppercutKickStraight();
                        break;
                    case "wUSK":
                        usableAttacks[chosenAttack].doMoves = CastWallUppercutStraightKick();
                        break;
                    case "cUSK":
                        usableAttacks[chosenAttack].doMoves = CastCubeUppercutStraightKick();
                        break;
                    case "pUSK":
                        usableAttacks[chosenAttack].doMoves = CastPillarUppercutStraightKick();
                        break;
                    case "wKUS":
                        usableAttacks[chosenAttack].doMoves = CastWallKickUppercutStraight();
                        break;
                    case "cKUS":
                        usableAttacks[chosenAttack].doMoves = CastCubeKickUppercutStraight();
                        break;
                    case "pKUS":
                        usableAttacks[chosenAttack].doMoves = CastPillarKickUppercutStraight();
                        break;
                    case "wKSU":
                        usableAttacks[chosenAttack].doMoves = CastWallKickStraightUppercut();
                        break;
                    case "cKSU":
                        usableAttacks[chosenAttack].doMoves = CastCubeKickStraightUppercut();
                        break;
                    case "pKSU":
                        usableAttacks[chosenAttack].doMoves = CastPillarKickStraightUppercut();
                        break;
                }
            }
            attacking = false;
            yield break;
        }

        #region Combos
        // Summon
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
            yield return new WaitForSeconds(0.4f);
            attackRunning = false;
            yield break;
        }

        // Straight
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
            yield return new WaitForSeconds(0.4f);
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

        private IEnumerator CastPillarStraight()
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
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastCubeStraight()
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
            yield return new WaitForSeconds(0.3f);
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

        // Straight Uppercut
        private IEnumerator CastPillarStraightUppercut()
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

        // Uppercut Straight
        private IEnumerator CastBallUppercutStraight()
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
            yield return new WaitForSeconds(0.75f);
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastPillarUppercutStraight()
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
            yield return new WaitForSeconds(0.3f);
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
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

        private IEnumerator CastCubeUppercutStraight()
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
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.5f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastWallUppercutStraight()
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
            yield return new WaitForSeconds(0.4f);
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.5f);
            attackRunning = false;
            yield break;
        }

        // Straight Uppercut Kick
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

        private IEnumerator CastCubeStraightUppercutKick()
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
            yield return new WaitForSeconds(0.3f);
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

        private IEnumerator CastPillarStraightUppercutKick()
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

        // Uppercut Kick Straight
        private IEnumerator CastWallUppercutKickStraight()
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
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
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
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastCubeUppercutKickStraight()
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
            yield return new WaitForSeconds(0.3f);
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
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
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastPillarUppercutKickStraight()
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
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
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
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        // Uppercut Straight Kick

        private IEnumerator CastWallUppercutStraightKick()
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
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
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
            yield return new WaitForSeconds(0.8f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastCubeUppercutStraightKick()
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
            yield return new WaitForSeconds(0.5f);
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
                //Kick
                thisHowardAnimator.SetAnimationTrigger("Kick");
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastPillarUppercutStraightKick()
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
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
                //Kick
                thisHowardAnimator.SetAnimationTrigger("Kick");
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        // Kick Uppercut Straight

        private IEnumerator CastWallKickUppercutStraight()
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
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastCubeKickUppercutStraight()
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
            yield return new WaitForSeconds(0.3f);
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastPillarKickUppercutStraight()
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
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        // Kick Straight Uppercut

        private IEnumerator CastWallKickStraightUppercut()
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
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastCubeKickStraightUppercut()
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
            yield return new WaitForSeconds(0.3f);
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        private IEnumerator CastPillarKickStraightUppercut()
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
            //Kick
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Kick");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.2f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[7]);
            }
            catch { attackRunning = false; yield break; }
            //Straight
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.1f);
            try
            {
                thisHowardComponent.Execute(Main.availableStacks[5]);
            }
            catch { attackRunning = false; yield break; }
            //Uppercut
            try
            {
                thisHowardAnimator.SetAnimationTrigger("Straight");
                thisHowardComponent.Execute(Main.availableStacks[6]);
            }
            catch { attackRunning = false; yield break; }
            yield return new WaitForSeconds(0.6f);
            attackRunning = false;
            yield break;
        }

        // Other
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
        #endregion

        private void UpdateRotation()
        {
            if (thisHoward == null) { return; }
            //Grab Vector2 Angle (X/Z) to look at player from current rotation
            Vector3 directionNormalized = (playerHead.transform.position - thisDummy.transform.position).normalized;
            Vector2 directionToObject = new Vector2(directionNormalized.x, directionNormalized.z);
            float targetAngle = Main.AngleToObject(thisDummy.transform, playerHead.transform.position);
            targetAngle = Math.Abs(thisDummy.transform.rotation.eulerAngles.y - (Mathf.Atan2((playerHead.transform.position - thisDummy.transform.position).x, (playerHead.transform.position - thisDummy.transform.position).z) * 180 / Mathf.PI));
            if (targetAngle >= 360) { targetAngle -= 360; }
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
            lastAngleToPlayer = targetAngle - Math.Abs(turnAmount);
            //update rotation
            thisHoward.transform.rotation = Quaternion.Euler(0, thisHoward.transform.localRotation.eulerAngles.y + turnAmount, 0);
        }

        private void UpdatePosition()
        {
            try
            {
                Vector3.Distance(playerHead.transform.position, thisHoward.transform.position);
            }
            catch
            {
                Main.Log("thisHoward.transform.position ERROR");
                return;
            }
            try
            {
                Vector3.Distance(playerHead.transform.position, thisHoward.transform.position);
            }
            catch
            {
                Main.Log("thisHoward.transform.position ERROR");
                return;
            }
            float distanceToPlayer = Vector3.Distance(thisHoward.transform.position, playerHead.transform.position);
            if (distanceToPlayer > stopMovingCloserDistance)
            {
                Vector2 moveAwayDistance = Vector3.zero;
                int closeCount = 0;
                foreach (GameObject howard in Main.activeHowards)
                {
                    try
                    {
                        Vector3.Distance(howard.transform.position, howard.transform.position);
                    }
                    catch
                    {
                        Main.Log("foreach Main.activeHowards ERROR");
                        continue;
                    }
                    if ((thisHoward != howard) && (Vector3.Distance(howard.transform.position, thisHoward.transform.position) < dontGroupDistance))
                    {
                        Vector2 howardV2 = Main.V3ToV2XZ(howard.transform.position);
                        Vector2 thisHowardV2 = Main.V3ToV2XZ(thisHoward.transform.position);
                        moveAwayDistance += (howardV2 - thisHowardV2) * -1f;
                        closeCount++;
                    }
                }
                moveAwayDistance = moveAwayDistance.normalized;
                Vector3 moveAwayDistanceV3 = Main.V2ToV3XZ(moveAwayDistance);
                Vector3 newPosition = thisHoward.transform.position + ((closeCount > 0 ? moveAwayDistanceV3 : thisDummy.transform.forward) * moveSpeed * Time.deltaTime);
                int closeCount2 = 0;
                foreach (GameObject howard in Main.activeHowards)
                {
                    if ((thisHoward != howard) && (Vector3.Distance(howard.transform.position, newPosition) < dontGroupDistance))
                    {
                        closeCount2++;
                    }
                }
                if (closeCount2 <= closeCount)
                {
                    thisHoward.transform.position += (closeCount > 0 ? moveAwayDistanceV3 : thisDummy.transform.forward) * moveSpeed * Time.deltaTime;
                }
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
            float sinkSpeed = 0.1f;
            for(int i = 0; i < 25; i++)
            {
                try
                {
                    thisHoward.transform.position = new Vector3(thisHoward.transform.position.x, thisHoward.transform.position.y - sinkSpeed, thisHoward.transform.position.z);
                }
                catch { yield break; }
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
            if (Main.waveModeActive)
            {
                Main.nextWaveCount += respawnCount;
                Main.activeHowards.Remove(thisHoward);
                if (Main.activeHowards.Count == 0)
                {
                    for (int i = 0; i < Main.nextWaveCount; i++)
                    {
                        Main.SpawnNewHoward();
                        yield return new WaitForFixedUpdate();
                    }
                }
                GameObject.DestroyImmediate(thisHoward);
                yield break;
            }
            Main.activeHowards.Remove(thisHoward);
            for (int i = 0; i < respawnCount; i++)
            {
                if (Main.leftFlatland) { break; }
                Main.SpawnNewHoward();
                yield return new WaitForFixedUpdate();
            }
            GameObject.DestroyImmediate(thisHoward);
            yield break;
        }
    }
}
