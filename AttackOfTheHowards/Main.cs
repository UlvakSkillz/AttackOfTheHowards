using HarmonyLib;
using Il2CppRUMBLE.Environment.Howard;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players.Subsystems;
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
        public const string ModVersion = "1.0.0";
        public const string Author = "UlvakSkillz";
    }

    public class Main : MelonMod
    {
        public static System.Random random = new System.Random();
        private string currentScene = "Loader";
        private bool flatLandFound = false;
        private bool flatLandPressed = false;
        public static Vector3 flatLandOffsetCenter = new Vector3(2.8007f, 0f, -1.9802f);
        private Mod AttackOfTheHowards = new Mod();
        private bool enabled = true;
        private int startingNumber = 1;
        public static bool duplicateHoward = true;
        public static int maximumHowards = 10;
        public static float howardVisualDistance = 150f;
        private bool initialized = false;
        public static GameObject storedHoward;
        public static List<GameObject> activeHowards = new List<GameObject>();
        public static Stack[] availableStacks;
        private bool leftFlatland = false;

        public static void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
            leftFlatland = true;
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
            AttackOfTheHowards.AddToList("Starting Number of Howards", 1, "Sets the Starting number of Howards to Fight", new Tags { });
            AttackOfTheHowards.AddToList("Duplicate When Howard Dies", true, 0, "Respawns 2 Howards instead of 1", new Tags { });
            AttackOfTheHowards.AddToList("Max Number of Howards", 10, "Sets the Max number of Howards to Fight", new Tags { });
            AttackOfTheHowards.AddToList("Howard Visual Distance", 150, "Sets the Distance in which Howard will Start Chasing the Player", new Tags { });
            AttackOfTheHowards.GetFromFile();
            Save();
            AttackOfTheHowards.ModSaved += Save;
            UI.instance.AddMod(AttackOfTheHowards);
        }

        private void Save()
        {
            enabled = (bool)AttackOfTheHowards.Settings[0].SavedValue;
            startingNumber = (int)AttackOfTheHowards.Settings[1].SavedValue;
            duplicateHoward = (bool)AttackOfTheHowards.Settings[2].SavedValue;
            maximumHowards = (int)AttackOfTheHowards.Settings[3].SavedValue;
            howardVisualDistance = (int)AttackOfTheHowards.Settings[4].SavedValue;
        }

        private void mapLoaded()
        {
            if (currentScene == "Gym")
            {
                if (!initialized)
                {
                    storedHoward = GameObject.Instantiate(Calls.GameObjects.Gym.Logic.HeinhouserProducts.HowardRoot.GetGameObject());
                    storedHoward.name = "Howard";
                    storedHoward.transform.localPosition = Vector3.zero;
                    storedHoward.transform.localRotation = Quaternion.Euler(0, -245.1242f, 0);
                    GameObject.Destroy(storedHoward.transform.GetChild(5).gameObject);
                    storedHoward.transform.GetChild(4).localScale = Vector3.zero;
                    GameObject howardDummy = storedHoward.transform.GetChild(3).gameObject;
                    howardDummy.transform.localPosition = Vector3.zero;
                    howardDummy.transform.localRotation = Quaternion.identity;
                    GameObject.Destroy(storedHoward.transform.GetChild(1).gameObject); //props
                    storedHoward.transform.GetChild(4).transform.localScale = Vector3.zero; //console
                    Howard howardComponent = storedHoward.GetComponent<Howard>();
                    howardComponent.CurrentSelectedLogic.DodgeBehaviour = null;
                    howardComponent.CurrentSelectedLogic.SequenceSets.Clear();
                    howardComponent.CurrentSelectedLogic.reactions = null;
                    storedHoward.SetActive(false);
                    storedHoward.AddComponent<HowardCombat>();
                    GameObject.DontDestroyOnLoad(storedHoward);
                    initialized = true;
                    MelonCoroutines.Start(Test());
                }
                if (flatLandFound)
                {
                    MelonCoroutines.Start(listenForFlatLandButton());
                }
            }
        }

        private IEnumerator Test()
        {
            yield return new WaitForSeconds(15);
            GameObject.Find("FlatLand/FlatLandButton/Button").GetComponent<InteractionButton>().RPC_OnPressed();
            yield break;
        }

        private IEnumerator listenForFlatLandButton()
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
        private IEnumerator toFlatLand()
        {
            yield return new WaitForSeconds(1);
            flatLandPressed = false;
            if (enabled)
            {
                SortStacks();
                //this removes Howards on Player Death
                //Calls.Players.GetLocalHealthbarGameObject().GetComponent<PlayerHealth>().onHealthDepleted.AddListener(new Action(() => {
                //    while (activeHowards.Count > 0)
                //    {
                //        GameObject.Destroy(activeHowards[0]);
                //        activeHowards.RemoveAt(0);
                //    }
                //}));
                for (int i = 0; i < startingNumber; i++)
                {
                    SpawnNewHoward();
                    yield return new WaitForFixedUpdate();
                }
                Transform playerHead = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(1).GetChild(0).GetChild(0);
                PlayerMovement playerMovement = PlayerManager.instance.localPlayer.Controller.GetComponent<PlayerMovement>();
                while (!leftFlatland)
                {
                    if (playerMovement.IsGrounded() && ((playerMovement.activeMovementType == PlayerMovement.MovementType.AirealKnockback) || (playerMovement.activeMovementType == PlayerMovement.MovementType.Airborne)))
                    {
                        PlayerManager.instance.localPlayer.Controller.GetComponent<PlayerMovement>().activeMovementType = PlayerMovement.MovementType.Normal;
                    }
                    yield return new WaitForFixedUpdate();
                }
            }
            yield break;
        }

        public static void SpawnNewHoward()
        {
            if (Main.maximumHowards <= Main.activeHowards.Count - 1) { return; }
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
            }
            newHoward.SetActive(true);
            activeHowards.Add(newHoward);
        }

        private void SortStacks()
        {
            availableStacks = new Stack[9];
            Log("0");
            Il2CppSystem.Collections.Generic.List<Stack> stacks = PlayerManager.instance.localPlayer.Controller.GetComponent<PlayerStackProcessor>().availableStacks;
            Log("1");
            for (int i = 0; i < stacks.Count; i++)
            {
                Log("2 - " + stacks[i].name);
                switch (stacks[i].name)
                {
                    case "Disc":
                        availableStacks[0] = stacks[i];
                        break;
                    case "SpawnPillar":
                        availableStacks[1] = stacks[i];
                        break;
                    case "SpawnBall":
                        availableStacks[2] = stacks[i];
                        break;
                    case "SpawnCube":
                        availableStacks[3] = stacks[i];
                        break;
                    case "SpawnWall":
                        availableStacks[4] = stacks[i];
                        break;
                    case "Straight":
                        availableStacks[5] = stacks[i];
                        break;
                    case "Uppercut":
                        availableStacks[6] = stacks[i];
                        break;
                    case "Kick":
                        availableStacks[7] = stacks[i];
                        break;
                    case "Explode":
                        availableStacks[8] = stacks[i];
                        break;
                }
            }
            Log("3");
        }

        [HarmonyPatch(typeof(Howard), "DealDamage", new Type[] { typeof(int), typeof(Vector3) })]
        public static class howardDamage
        {
            public static void Prefix(ref Howard __instance, ref int damage, ref Vector3 hitmarkerPos)
            {
                if (__instance.transform.parent == null)
                {
                    HowardCombat thisHowardCombat = __instance.GetComponent<HowardCombat>();
                    if ((__instance.currentHp - damage <= 0) && (thisHowardCombat.mode != 3))
                    {
                        thisHowardCombat.RunDeath();
                    }
                }
            }
        }
    }
}
