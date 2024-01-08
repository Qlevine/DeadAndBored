
using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using SpectateEnemy;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.SceneManagement;
using LC_API;
using System;
using Dissonance;
using Dissonance.Audio.Playback;
using System.Linq;
using System.Reflection;
using DeadAndBored.Patches;
using DeadAndBored.Configuration;

namespace DeadAndBored
{
    [BepInPlugin(modGUID, modName, modVersion)]
    //[BepInDependency("com.EBro912.SpectateEnemies", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {

        private const string modGUID = "quixler.DeadAndBored";
        private const string modName = "DeadAndBored";
        private const string modVersion = "1.0.0";

        private Harmony harmony;
        private void Awake()
        {
            harmony = new Harmony(modGUID);
            Configuration.Config.Init();
            Logger.LogInfo("Dead And Bored loaded!");
            harmony.PatchAll();
        }
    }

    public class BroadcastParameters
    {
        public string controllerName;
        public string enemyName;
        public float x;
        public float y;
        public float z;
    }

    namespace DeadAndBoredPatches
    {
        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        internal class QuickMenuManager_Patches
        {
            private static void Postfix(QuickMenuManager __instance)
            {
                if (DeadAndBored.Instance == null)
                {
                    GameObject obj = new("DeadAndBoredObject");
                    obj.AddComponent<DeadAndBored>();
                    Debug.Log("Dead And Bored instance created");
                }
            }
        }
    }

        public class DeadAndBored : MonoBehaviour
    {
        public static DeadAndBored Instance = null;

        private bool wasSpectatingEnemies = false;
        private GameObject oldSpectatingEnemy = null;

        //Used to determine if this online player is currently talking as an enemy
        public static bool isDeadAndTalking = false;

        private static string deadAndTalkingUniqueName = "TheDeadTalk";
        private static string deadAndStopTalkingUniqueName = "TheDeadStopTalk";

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(Scene old, Scene newScene)
        {
            Reset();
        }

        private void Update()
        {
            if (!SpectateEnemiesAPI.IsLoaded)
            {
                return;
            }
            //Debug.Log("Spectate enemies loaded");
            if (oldSpectatingEnemy != SpectateEnemiesAPI.CurrentEnemySpectating()) //We changed which enemy we are spectating
            {
                oldSpectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                if (DeadAndBored.isDeadAndTalking) //If they changed who they are spectating, then they should have to re-press the talk key
                {
                    DeadAndBored.StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
            //Debug.Log("Check 1");
            if (wasSpectatingEnemies != SpectateEnemiesAPI.IsSpectatingEnemies) //We changed if we are spectating humans or enemies
            {
                wasSpectatingEnemies = SpectateEnemiesAPI.IsSpectatingEnemies;
                if (DeadAndBored.isDeadAndTalking)
                {
                    DeadAndBored.StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
            //Debug.Log("Check 2");
            if (GameNetworkManager.Instance == null)
            {
                return; //Early exit if not online
            }
            //Debug.Log("Check 3");
            if (GameNetworkManager.Instance.localPlayerController == null)
            {
                return; //Early exit if the local player is not setup
            }
            //Debug.Log("Check 4");
            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
            {
                if (SpectateEnemiesAPI.IsSpectatingEnemies)
                {
                    if (UnityInput.Current.GetKeyDown(Config.deadAndTalkingKey) && !DeadAndBored.isDeadAndTalking) //Begin talking as enemy
                    {
                        Debug.Log("KeyDown");
                        BroadcastParameters param = new BroadcastParameters();
                        param.controllerName = GameNetworkManager.Instance.localPlayerController.NetworkObjectId.ToString();
                        GameObject spectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                        if (spectatingEnemy != null)
                        {
                            param.enemyName = spectatingEnemy.name;
                            param.x = spectatingEnemy.transform.position.x;
                            param.y = spectatingEnemy.transform.position.y;
                            param.z = spectatingEnemy.transform.position.z;
                            Debug.Log($"Enemy Name: {spectatingEnemy.name}");
                            Debug.Log($"TALKING with ID: {param.controllerName}");
                            DeadAndBored.Talk(param);
                        }
                    }
                }
                //Debug.Log("Check 5");
                if (!UnityInput.Current.GetKey(Config.deadAndTalkingKey)) //We let go of our proximity chat button so stop talk
                {
                    Debug.Log("Stopping talk");
                    if (isDeadAndTalking)
                    {
                        DeadAndBored.StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                    }
                }
            }
        }

        public static void Talk(BroadcastParameters param)
        {
            isDeadAndTalking = true;
            LC_API.Networking.Network.Broadcast<BroadcastParameters>(deadAndTalkingUniqueName, param);
        }

        public static void StopTalk(ulong controllerName)
        {
            isDeadAndTalking = false;
            LC_API.Networking.Network.Broadcast<string>(deadAndStopTalkingUniqueName, controllerName.ToString());
        }

        public void Init()
        {
            System.Action<ulong, BroadcastParameters> talkAction = (sender, args) => { TheDeadTalk(args); };
            System.Action<ulong, string> stopTalkAction = (sender, args) => { TheDeadStopTalk(args); };
            LC_API.Networking.Network.RegisterMessage<BroadcastParameters>(deadAndTalkingUniqueName, false, talkAction);
            LC_API.Networking.Network.RegisterMessage<string>(deadAndStopTalkingUniqueName, false, stopTalkAction);
            Reset();
        }

        private void Reset()
        {
            isDeadAndTalking = false;
            UpdatePlayerVoiceEffectsPatch.Configs.Clear();
        }

        private void TheDeadTalk(BroadcastParameters param)
        {
            PlayerControllerB talkingPlayer = FindPlayerByNetworkID(param.controllerName);
            Debug.Log($"The dead talk: {talkingPlayer}");
            if (talkingPlayer == null)
            {
                return;
            }

            if (UpdatePlayerVoiceEffectsPatch.Configs.ContainsKey(talkingPlayer))
            {
                AudioConfig audioInfo = UpdatePlayerVoiceEffectsPatch.Configs[talkingPlayer];
                GameObject obj = FindEnemyByNameAndPos(param);
                if (obj != null)
                {
                    audioInfo.EnemyT = obj.transform;
                    Debug.Log($"Setup enemyTransform correctly!");
                }
                UpdatePlayerVoiceEffectsPatch.Configs[talkingPlayer] = audioInfo;
            }
        }

        private void TheDeadStopTalk(string controllerID)
        {
            Debug.Log($"The dead stop talk: {controllerID}");

            PlayerControllerB controller = FindPlayerByNetworkID(controllerID);
            if (controller == null)
            {
                return;
            }

            
            if (!UpdatePlayerVoiceEffectsPatch.Configs.ContainsKey(controller))
            {
                return;
            }
            AudioConfig audioInfo = UpdatePlayerVoiceEffectsPatch.Configs[controller];
            audioInfo.EnemyT = null;
            Debug.Log("Stopped audio correctly!");
        }

        private PlayerControllerB FindPlayerByNetworkID(string networkID)
        {
            foreach (PlayerControllerB playerControllerB in FindObjectsByType<PlayerControllerB>(FindObjectsSortMode.None))
            {
                if (playerControllerB.NetworkObjectId == ulong.Parse(networkID))
                {
                    return playerControllerB;
                }
            }

            return null;
        }

        private GameObject FindEnemyByNameAndPos(BroadcastParameters bParams)
        {
            Vector3 enemyPos = new Vector3(bParams.x, bParams.y, bParams.z);
            Debug.Log($"Looking for enemy {bParams.enemyName}, with position {enemyPos}");
            GameObject[] allObjs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            float shortestDistance = Mathf.Infinity;
            GameObject foundObj = null;
            foreach (GameObject obj in allObjs)
            {
                if (obj.name == bParams.enemyName)
                {
                    float currDistance = Vector3.Distance(obj.transform.position, enemyPos);
                    if (Vector3.Distance(obj.transform.position, enemyPos) < shortestDistance)
                    {
                        Debug.Log($"Found enemy {bParams.enemyName}");
                        foundObj = obj;
                        shortestDistance = currDistance;
                    }
                }
            }
            return foundObj;
        }
    }
}