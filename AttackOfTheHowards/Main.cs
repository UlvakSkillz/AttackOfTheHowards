using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppRUMBLE.Environment.Howard;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using RumbleModdingAPI;
using RumbleModUI;
using System.Collections;
using UnityEngine;
using Stack = Il2CppRUMBLE.MoveSystem.Stack;

namespace AttackOfTheHowards
{
    public static class BuildInfo
    {
        public const string ModName = "AttackOfTheHowards";
        public const string ModVersion = "1.3.1";
        public const string Author = "UlvakSkillz";
    }


    public class Main : MelonMod
    {
        public static System.Random random = new System.Random();
        public static string currentScene = "Loader";
        private bool flatLandFound = false;
        private bool flatLandPressed = false;
        public static Vector3 flatLandOffsetCenter = new Vector3(2.8007f, 0f, -1.9802f);
        private Mod AttackOfTheHowards = new Mod();
        private static bool enabled = true;
        private int startingNumber = 1;
        private int startingNumberTemp = 1;
        public static bool duplicateHoward = true;
        public static bool duplicateHowardTemp = true;
        public static int maximumHowards = 10;
        public static int maximumHowardsTemp = 10;
        public static float howardVisualDistance = 150f;
        public static float howardVisualDistanceTemp = 150f;
        private static bool healOnKill = false;
        private static bool healOnKillTemp = false;
        private bool bypassBeltLevel = false;
        private bool bypassBeltLevelTemp = false;
        public static bool waveModeActive = false;
        public static bool waveModeActiveTemp = false;
        private static bool showScore = true;
        private static bool showScoreTemp = true;
        private bool initialized = false;
        public static GameObject storedHoward;
        public static List<GameObject> activeHowards = new List<GameObject>();
        public static Stack[] availableStacks;
        public static int nextWaveCount;
        public static bool leftFlatland = true;
        public static int score = 0;
        public static bool running = false;
        private bool healthListenerAdded = false;

        public static void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        public static Vector2 V3ToV2XZ(Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

        public static Vector3 V2ToV3XZ(Vector2 v3)
        {
            return new Vector3(v3.x, 0, v3.y);
        }

        public static float AngleToObject(Transform viewer, Vector3 objectPosition)
        {
            Vector3 thisHowardNormalized = viewer.forward.normalized;
            Vector3 directionNormalized = (objectPosition - viewer.position).normalized;
            Vector2 forwardVector = new Vector2(thisHowardNormalized.x, thisHowardNormalized.z);
            Vector2 directionToObject = new Vector2(directionNormalized.x, directionNormalized.z);
            float dotProduct = Vector2.Dot(forwardVector, directionToObject);
            float targetAngle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg; // Convert radians to degrees
            targetAngle = Math.Abs(viewer.rotation.eulerAngles.y - (Mathf.Atan2((objectPosition - viewer.position).x, (objectPosition - viewer.position).z) * 180 / Mathf.PI));
            if (targetAngle >= 360) { targetAngle -= 360; }
            return targetAngle;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
            leftFlatland = true;
            duplicateHoward = duplicateHowardTemp;
            startingNumber = startingNumberTemp;
            maximumHowards = maximumHowardsTemp;
            howardVisualDistance = howardVisualDistanceTemp;
            healOnKill = healOnKillTemp;
            bypassBeltLevel = bypassBeltLevelTemp;
            waveModeActive = waveModeActiveTemp;
            showScore = showScoreTemp;
            score = 0;
            healthListenerAdded = false;
        }

        public override void OnLateInitializeMelon()
        {
            Calls.onMapInitialized += mapLoaded;
            Calls.onMyModsGathered += checkMods;
            UI.instance.UI_Initialized += UIInit;
        }
        
        private void checkMods()
        {
            flatLandFound = Calls.Mods.findOwnMod("FlatLand", "1.0.0", false);
        }

        private void UIInit()
        {
            AttackOfTheHowards.ModName = BuildInfo.ModName;
            AttackOfTheHowards.ModVersion = BuildInfo.ModVersion;
            AttackOfTheHowards.SetFolder(BuildInfo.ModName);
            AttackOfTheHowards.AddToList("Enabled", true, 0, "Toggles Mod On/Off", new Tags { });
            AttackOfTheHowards.AddToList("Restart Fight", false, 0, "Stops Existing Fight if there is one, then Starts another Fight", new Tags { DoNotSave = true });
            AttackOfTheHowards.AddToList("Duplicate When Howard Dies", true, 0, "Respawns 2 Howards instead of 1 when Set to True", new Tags { });
            AttackOfTheHowards.AddToList("Starting Number of Howards", 1, "Sets the Starting number of Howards to Fight", new Tags { });
            AttackOfTheHowards.AddToList("Max Number of Howards", 10, "Sets the Max number of Howards to Fight", new Tags { });
            AttackOfTheHowards.AddToList("Howard Visual Distance", 150, "Sets the Distance in which Howard will Start Chasing the Player", new Tags { });
            AttackOfTheHowards.AddToList("Heal On Howard Death", false, 0, "Heal whenever a Howard Dies", new Tags { });
            AttackOfTheHowards.AddToList("Bypass Belt Limit", false, 0, "Gives Howard access to All Moves even if the Player doesn't have those Moves", new Tags { });
            AttackOfTheHowards.AddToList("Wave Mode", false, 0, "Respawns a new Wave of Howards after all Howards Dies instead of Right Away", new Tags { });
            AttackOfTheHowards.AddToList("Show Score", true, 0, "Shows the Scoreboard After the Player Dies", new Tags { });
            AttackOfTheHowards.GetFromFile();
            Save();
            AttackOfTheHowards.ModSaved += Save;
            UI.instance.AddMod(AttackOfTheHowards);
        }

        private void Save()
        {
            enabled = (bool)AttackOfTheHowards.Settings[0].SavedValue;
            bool restart = (bool)AttackOfTheHowards.Settings[1].SavedValue;
            duplicateHowardTemp = (bool)AttackOfTheHowards.Settings[2].SavedValue;
            startingNumberTemp = (int)AttackOfTheHowards.Settings[3].SavedValue;
            maximumHowardsTemp = (int)AttackOfTheHowards.Settings[4].SavedValue;
            howardVisualDistanceTemp = (int)AttackOfTheHowards.Settings[5].SavedValue;
            healOnKillTemp = (bool)AttackOfTheHowards.Settings[6].SavedValue;
            bypassBeltLevelTemp = (bool)AttackOfTheHowards.Settings[7].SavedValue;
            waveModeActiveTemp = (bool)AttackOfTheHowards.Settings[8].SavedValue;
            showScoreTemp = (bool)AttackOfTheHowards.Settings[9].SavedValue;
            if (!leftFlatland)
            {
                if (!enabled)
                {
                    MelonCoroutines.Start(PlayerDied(false));
                }
                if (restart && enabled)
                {
                    AttackOfTheHowards.Settings[1].Value = false;
                    AttackOfTheHowards.Settings[1].SavedValue = false;
                    MelonCoroutines.Start(PlayerDied(true));
                }
            }
            else
            {
                duplicateHoward = duplicateHowardTemp;
                startingNumber = startingNumberTemp;
                maximumHowards = maximumHowardsTemp;
                howardVisualDistance = howardVisualDistanceTemp;
                healOnKill = healOnKillTemp;
                bypassBeltLevel = bypassBeltLevelTemp;
                waveModeActive = waveModeActiveTemp;
                showScore = showScoreTemp;
            }
        }

        private void mapLoaded()
        {
            if (currentScene == "Gym")
            {
                if (!initialized)
                {
                    SpawnStoredHoward();
                    initialized = true;
                }
                if (flatLandFound)
                {
                    MelonCoroutines.Start(listenForFlatLandButton());
                }
            }
        }

        private IEnumerator<WaitForSeconds> listenForFlatLandButton()
        {
            yield return new WaitForSeconds(1);
            GameObject.Find("FlatLand/FlatLandButton/Button").GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                if (!flatLandPressed)
                {
                    flatLandPressed = true;
                    leftFlatland = false;
                    MelonCoroutines.Start(toFlatLand());
                }
            }));
            yield break;
        }
        
        private IEnumerator<WaitForSeconds> toFlatLand()
        {
            nextWaveCount = startingNumber;
            yield return new WaitForSeconds(1f);
            flatLandPressed = false;
            if (enabled)
            {
                SortStacks();
                if (!healthListenerAdded)
                {
                    Calls.Players.GetLocalHealthbarGameObject().transform.parent.GetComponent<PlayerHealth>().onHealthDepleted.AddListener(new Action(() => {
                        //this removes Howards on Player Death
                        MelonCoroutines.Start(PlayerDied(false));
                        running = false;
                    }));
                    healthListenerAdded = true;
                }
                MelonCoroutines.Start(StartingText());
                for (int i = 0; i < startingNumber; i++)
                {
                    if (leftFlatland) { break; }
                    SpawnNewHoward();
                    yield return new WaitForSeconds(0.02f);
                }
            }
            yield break;
        }

        private IEnumerator<WaitForSeconds> StartingText()
        {
            Transform playerHead = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(2).GetChild(0).GetChild(0);
            GameObject startingText = Calls.Create.NewText();
            startingText.name = "StartingText";
            startingText.transform.parent = playerHead;
            startingText.transform.localPosition = new Vector3(0, 2, 5);
            startingText.transform.localRotation = Quaternion.Euler(0, 0, 0);
            TextMeshPro startingTMP = startingText.GetComponent<TextMeshPro>();
            startingTMP.text = "Attack of the Howards Starting!";
            startingTMP.fontSize = 8;
            startingTMP.alignment = TextAlignmentOptions.Center;
            startingTMP.color = Color.red;
            startingTMP.enableWordWrapping = false;
            yield return new WaitForSeconds(5);
            GameObject.Destroy(startingText);
            yield break;
        }

        private IEnumerator<WaitForSeconds> PlayerDied(bool restart = false)
        {
            if (activeHowards.Count != 0)
            {
                foreach (GameObject howard in activeHowards)
                {
                    howard.GetComponent<HowardCombat>().mode = 3;
                }
                yield return new WaitForSeconds(0.25f);
                foreach (GameObject howard in activeHowards)
                {
                    howard.GetComponent<Howard>().OnPlayerDiedInRange();
                }
                yield return new WaitForSeconds(4);
                float sinkSpeed = 0.1f;
                for (int z = 0; z < 25; z++)
                {
                    for (int i = 0; i < activeHowards.Count; i++)
                    {
                        try
                        {
                            activeHowards[i].transform.position = new Vector3(activeHowards[i].transform.position.x, activeHowards[i].transform.position.y - sinkSpeed, activeHowards[i].transform.position.z);
                        }
                        catch { break; }
                    }
                    yield return new WaitForSeconds(0.02f);
                }
                while (activeHowards.Count > 0)
                {
                    GameObject.Destroy(activeHowards[0]);
                    activeHowards.RemoveAt(0);
                }
            }
            running = false;
            GameObject gameOverParent = null;
            if (showScore)
            {
                gameOverParent = new GameObject();
                gameOverParent.transform.position = Vector3.zero;
                gameOverParent.transform.localRotation = Quaternion.identity;
                gameOverParent.name = "Attack of the Howard Score";
                GameObject scoreText = Calls.Create.NewText();
                scoreText.name = "Score";
                scoreText.transform.parent = gameOverParent.transform;
                scoreText.transform.localPosition = new Vector3(0, 1, 0);
                scoreText.transform.localRotation = Quaternion.Euler(0, 180, 0);
                TextMeshPro scoreTMP = scoreText.GetComponent<TextMeshPro>();
                scoreTMP.text = $"Score: {score}";
                scoreTMP.fontSize = 24;
                scoreTMP.alignment = TextAlignmentOptions.Center;
                scoreTMP.color = Color.red;
                scoreTMP.enableWordWrapping = false;
                GameObject settingsText = Calls.Create.NewText();
                settingsText.name = "Settings";
                settingsText.transform.parent = gameOverParent.transform;
                settingsText.transform.localPosition = new Vector3(0, -6, 0);
                settingsText.transform.localRotation = Quaternion.Euler(0, 180, 0);
                TextMeshPro settingsTMP = settingsText.GetComponent<TextMeshPro>();
                settingsTMP.text = $"Settings:{Environment.NewLine}Duplication: {(duplicateHoward ? "True" : "False")}" +
                    $"{Environment.NewLine}Starting Number: {startingNumber}" +
                    $"{Environment.NewLine}Max Number: {maximumHowards}" +
                    $"{Environment.NewLine}Howard Visual Distance: {howardVisualDistance}" +
                    $"{Environment.NewLine}Heal On Kill: {(healOnKill ? "True" : "False")}" +
                    $"{Environment.NewLine}Wave Mode: {(waveModeActive ? "True" : "False")}";
                settingsTMP.fontSize = 16;
                settingsTMP.alignment = TextAlignmentOptions.Center;
                settingsTMP.color = Color.red;
                settingsTMP.enableWordWrapping = false;
                gameOverParent.transform.position = new Vector3(0, 16, 0);
                MelonCoroutines.Start(SignLookAtPlayer(gameOverParent));
            }
            if (restart)
            {
                duplicateHoward = duplicateHowardTemp;
                startingNumber = startingNumberTemp;
                maximumHowards = maximumHowardsTemp;
                howardVisualDistance = howardVisualDistanceTemp;
                healOnKill = healOnKillTemp;
                bypassBeltLevel = bypassBeltLevelTemp;
                waveModeActive = waveModeActiveTemp;
                showScore = showScoreTemp;
                if (gameOverParent != null) { GameObject.Destroy(gameOverParent); }
                Calls.Players.GetLocalHealthbarGameObject().GetComponent<PlayerHealth>().ForceHealthReset();
                MelonCoroutines.Start(toFlatLand());
            }
            yield break;
        }

        private IEnumerator<WaitForFixedUpdate> SignLookAtPlayer(GameObject gameOverParent)
        {
            while (gameOverParent != null)
            {
                gameOverParent.transform.LookAt(PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(2).GetChild(0).GetChild(0));
                yield return new WaitForFixedUpdate();
            }
            yield break;
        }

        private void SpawnStoredHoward()
        {
            if (Main.maximumHowards <= Main.activeHowards.Count) { return; }
            storedHoward = GameObject.Instantiate(Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Howardroot.GetGameObject());
            storedHoward.transform.position = flatLandOffsetCenter + new Vector3(0, -10, 0);
            storedHoward.name = "Howard";
            storedHoward.transform.localRotation = Quaternion.Euler(0, -245.1242f, 0);
            GameObject.Destroy(storedHoward.transform.GetChild(5).gameObject); //TutorialChecklist
            GameObject howardDummy = storedHoward.transform.GetChild(3).gameObject;
            GameObject.Destroy(storedHoward.transform.GetChild(1).gameObject); //props
            storedHoward.transform.GetChild(4).transform.localPosition = new Vector3(0, -100, 0); //console //cant scale zero or turn off console
            storedHoward.transform.GetChild(4).transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); //console
            Howard thisHowardComponent = storedHoward.GetComponent<Howard>();
            thisHowardComponent.CurrentSelectedLogic.DodgeBehaviour = null;
            thisHowardComponent.CurrentSelectedLogic.SequenceSets.Clear();
            thisHowardComponent.CurrentSelectedLogic.reactions = null;
            storedHoward.SetActive(false);
            storedHoward.AddComponent<HowardCombat>();
            GameObject.DontDestroyOnLoad(storedHoward);
        }

        public static void SpawnNewHoward()
        {
            if ((Main.maximumHowards <= Main.activeHowards.Count) || !enabled) { return; }
            int mapSize = (int)FlatLand.main.FlatLand.Settings[0].SavedValue;
            float edgeLength = (((float)mapSize) / 2) - 1;
            GameObject newHoward = GameObject.Instantiate(storedHoward);
            int randomSide = random.Next(0, 4);
            float randomSpot = ((float)random.Next(-(int)edgeLength * 100, (int)edgeLength * 100)) / 100;
            switch (randomSide)
            {
                case 0:
                    newHoward.transform.position = flatLandOffsetCenter + new Vector3(-edgeLength, -10, randomSpot);
                    break;
                case 1:
                    newHoward.transform.position = flatLandOffsetCenter + new Vector3(edgeLength, -10, randomSpot);
                    break;
                case 2:
                    newHoward.transform.position = flatLandOffsetCenter + new Vector3(randomSpot, -10, -edgeLength);
                    break;
                case 3:
                    newHoward.transform.position = flatLandOffsetCenter + new Vector3(randomSpot, -10, edgeLength);
                    break;
                default:
                    break;
            }
            newHoward.SetActive(true);
            activeHowards.Add(newHoward);
        }

        private void SortStacks()
        {
            availableStacks = new Stack[8];
            Il2CppReferenceArray<HowardLogic> logicLevels = storedHoward.GetComponent<Howard>().LogicLevels;
            foreach(HowardLogic logicLevel in logicLevels)
            {
                Il2CppSystem.Collections.Generic.List<HowardLogic.SequenceSet> sequenceSets = logicLevel.SequenceSets;
                foreach (HowardLogic.SequenceSet sequenceSet in sequenceSets)
                {
                    Il2CppReferenceArray<HowardAttackBehaviour.TimedStack> timedStacks;
                    try
                    {
                        timedStacks = sequenceSet.Sequence.BehaviourTimings[0].Behaviour.Cast<HowardAttackBehaviour>().timedStacks;
                    }
                    catch { continue; } //to catch HowardMoveBehaviour Failed Conversions
                    foreach (HowardAttackBehaviour.TimedStack timedStack in timedStacks)
                    {
                        bool foundStack = false;
                        foreach(Stack playerStack in PlayerManager.instance.localPlayer.Controller.GetComponent<PlayerStackProcessor>().availableStacks)
                        {
                            if (playerStack.name == timedStack.Stack.name)
                            {
                                foundStack = true;
                                break;
                            }
                        }
                        if (foundStack || bypassBeltLevel)
                        {
                            switch (timedStack.Stack.name)
                            {
                                case "Disc":
                                    availableStacks[0] = timedStack.Stack;
                                    break;
                                case "SpawnPillar":
                                    availableStacks[1] = timedStack.Stack;
                                    break;
                                case "SpawnBall":
                                    availableStacks[2] = timedStack.Stack;
                                    break;
                                case "SpawnCube":
                                    availableStacks[3] = timedStack.Stack;
                                    break;
                                case "SpawnWall":
                                    availableStacks[4] = timedStack.Stack;
                                    break;
                                case "Straight":
                                    availableStacks[5] = timedStack.Stack;
                                    break;
                                case "Uppercut":
                                    availableStacks[6] = timedStack.Stack;
                                    break;
                                case "Kick":
                                    availableStacks[7] = timedStack.Stack;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Howard), "DealDamage", new Type[] { typeof(int), typeof(Vector3) })]
        public static class howardDamage
        {
            public static bool Prefix(ref Howard __instance, ref int damage, ref Vector3 hitmarkerPos)
            {
                if (__instance.transform.parent == null)
                {
                    HowardCombat thisHowardCombat = __instance.GetComponent<HowardCombat>();
                    if ((__instance.currentHp - damage <= 0) && (thisHowardCombat.mode != 3))
                    {
                        score++;
                        thisHowardCombat.RunDeath();
                        if (!healOnKill)
                        {
                            return false;
                        }
                    }
                    else if (!healOnKill && thisHowardCombat.mode == 3)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
