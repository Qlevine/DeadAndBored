
using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using SpectateEnemy;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.SceneManagement;
using LC_API;
using System;

//[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
//[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
//internal class Plugin : BaseUnityPlugin
//{
//    public static MethodInfo raycastSpectate = null;
//    public static MethodInfo displaySpectatorTip = null;

//    public static Inputs Inputs = new();

//    private Harmony harmony;

//    private void Awake()
//    {
//        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
//        harmony.PatchAll();

//        raycastSpectate = AccessTools.Method(typeof(PlayerControllerB), "RaycastSpectateCameraAroundPivot");
//        displaySpectatorTip = AccessTools.Method(typeof(HUDManager), "DisplaySpectatorTip");
//        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} loaded!");
//    }
//}

namespace DeadAndBored
{
    [BepInPlugin("DeadAndBored", "DeadAndBored", "1.0")]
    //[BepInDependency("com.EBro912.SpectateEnemies", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        //Keycode used to begin talking when spectating an enemy
        public static BepInEx.Configuration.ConfigEntry<KeyCode> pressToTalkAsEnemyKey;

        private Harmony harmony;
        private void Awake()
        {
            pressToTalkAsEnemyKey = this.Config.Bind<KeyCode>("Config", "Key To Activate Enemy Proximity Chat", KeyCode.T);
            harmony = new Harmony("DeadAndBored");
            harmony.PatchAll();
            Logger.LogInfo("Dead And Bored loaded!");
        }
    }


    public class AudioData
    {
        public bool lowPassFilter = false;
        public bool highPassFilter = false;

        public float panStero = 0f;

        public float playerPitch = 0f;

        public float spatialBlend = 0f;

        public float volume = 0f;
    }

    public class DeadAndBored : MonoBehaviour
    {
        public static DeadAndBored Instance = null;
        //Dictionary from online player to audio source/enemy position
        private static Dictionary<PlayerControllerB, PlayAudioInfo> playerToAudioDict;

        //Used to determine if this online player is currently talking as an enemy
        public static bool isDeadAndTalking = false;

        private bool wasSpectatingEnemies = false;
        private GameObject oldSpectatingEnemy = null;

        private static string deadAndTalkingUniqueName = "TheDeadTalk";
        private static string deadAndStopTalkingUniqueName = "TheDeadStopTalk";

        public class BroadcastParameters
        {
            public string controllerName;
            public float x;
            public float y;
            public float z;
        }

        public struct PlayAudioInfo
        {
            public AudioSource audioSource;
            public AudioData audioData;
            public Vector3  enemyPos;
            public Vector3 originalPosition;

            private AudioData oldAudioData;

            public PlayAudioInfo()
            {
                enemyPos = Vector3.negativeInfinity;
                audioSource = null;
                originalPosition = Vector3.zero;
                audioData = new AudioData();
                oldAudioData = null;
            }

            public void OverwriteAudioData(PlayerControllerB playerControllerB)
            {
                oldAudioData = new AudioData();
                SetAudioData(ref oldAudioData, audioSource, playerControllerB);
                SetAudioSource(ref audioSource, audioData);
            }

            public void UndoAudioData()
            {
                SetAudioSource(ref audioSource, oldAudioData);
                oldAudioData = null;
            }
        }

        private void Awake()
        {
            Debug.Log("Awake for DeadAndBored");
            DontDestroyOnLoad(gameObject);
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

        private void Start()
        {
            SceneManager.activeSceneChanged += ResetSceneData;
            playerToAudioDict = new Dictionary<PlayerControllerB, PlayAudioInfo>();
            System.Action<ulong, BroadcastParameters> talkAction = (sender, args) => { TheDeadTalk(args); };
            System.Action<ulong, string> stopTalkAction = (sender, args) => { TheDeadStopTalk(args); };
            LC_API.Networking.Network.RegisterMessage<BroadcastParameters>(deadAndTalkingUniqueName, false, talkAction);
            LC_API.Networking.Network.RegisterMessage<string>(deadAndStopTalkingUniqueName, false, stopTalkAction);
            Debug.Log("Registered Network messages");
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
                if (isDeadAndTalking) //If they changed who they are spectating, then they should have to re-press the talk key
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
            //Debug.Log("Check 1");
            if (wasSpectatingEnemies != SpectateEnemiesAPI.IsSpectatingEnemies) //We changed if we are spectating humans or enemies
            {
                wasSpectatingEnemies = SpectateEnemiesAPI.IsSpectatingEnemies;
                if (isDeadAndTalking)
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
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
                    if (UnityInput.Current.GetKeyDown(Plugin.pressToTalkAsEnemyKey.Value) && !isDeadAndTalking) //Begin talking as enemy
                    {
                        Debug.Log("KeyDown");
                        BroadcastParameters param = new BroadcastParameters();
                        param.controllerName = GameNetworkManager.Instance.localPlayerController.NetworkObjectId.ToString();
                        GameObject spectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                        if (spectatingEnemy != null)
                        {
                            param.x = spectatingEnemy.transform.position.x;
                            param.y = spectatingEnemy.transform.position.y;   
                            param.z = spectatingEnemy.transform.position.z;
                            Debug.Log("TALKING");
                            Talk(param);
                        }
                    }
                }
                //Debug.Log("Check 5");
                if (UnityInput.Current.GetKeyUp(Plugin.pressToTalkAsEnemyKey.Value) && isDeadAndTalking) //We let go of our proximity chat button so stop talk
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
            //Debug.Log("Check 6");
            foreach (PlayAudioInfo audioInfo in playerToAudioDict.Values)
            {
                if (audioInfo.enemyPos == Vector3.negativeInfinity)
                {
                    continue; //Ignore players that are not actively talking
                }
                if (audioInfo.audioSource == null)
                {
                    continue; //This shouldn't be possible
                }
                Debug.Log($"Dead and Talking: Transform -> {audioInfo.enemyPos}");
                Debug.Log($"AudioSource: {audioInfo.audioSource}, Is Talking: {audioInfo.audioSource.isPlaying}");
                Debug.Log($"Player Position: {GameNetworkManager.Instance.localPlayerController.transform.position}");
                audioInfo.audioSource.transform.position = audioInfo.enemyPos;
            }
        }

        private void TheDeadTalk(BroadcastParameters param)
        {
            PlayerControllerB talkingPlayer = FindPlayerByNetworkID(param.controllerName);
            Debug.Log($"The dead talk: {talkingPlayer}");
            if (talkingPlayer == null)
            {
                return;
            }
            SetupAudioDictionary();
            if (playerToAudioDict.ContainsKey(talkingPlayer))
            {
                PlayAudioInfo audioInfo = playerToAudioDict[talkingPlayer];
                audioInfo.originalPosition = audioInfo.audioSource.transform.position;
                audioInfo.enemyPos = new Vector3(param.x, param.y, param.z);
                audioInfo.OverwriteAudioData(talkingPlayer);
                Debug.Log($"Setup enemyTransform correctly!");
            }
        }

        private void TheDeadStopTalk(string controllerID)
        {
            Debug.Log($"The dead stop talk: {controllerID}");

            PlayerControllerB controller = FindPlayerByNetworkID(controllerID);
            if(controller == null)
            {
                return;
            }

            SetupAudioDictionary();
            if (!playerToAudioDict.ContainsKey(controller))
            {
                return;
            }
            PlayAudioInfo audioInfo = playerToAudioDict[controller];
            audioInfo.enemyPos = Vector3.negativeInfinity;
            audioInfo.audioSource.transform.position = audioInfo.originalPosition;
            audioInfo.UndoAudioData();
            Debug.Log("Stopped audio correctly!");
        }

        private void ResetSceneData(Scene oldScene, Scene newScene)
        {
            playerToAudioDict.Clear();
            Debug.Log("Active scene change. Clearing dictionary");
        }

        private static void SetupAudioDictionary()
        {
            Debug.Log("Begin setup dictionary");
            if (playerToAudioDict == null)
            {
                playerToAudioDict = new Dictionary<PlayerControllerB, PlayAudioInfo>();
            }
            foreach (PlayerControllerB playerControllerB in FindObjectsByType<PlayerControllerB>(FindObjectsSortMode.None))
            {
                if (playerToAudioDict.ContainsKey(playerControllerB) && playerToAudioDict[playerControllerB].audioSource != null)
                {
                    continue; //This player is already setup
                }
                PlayAudioInfo audioInfo = new PlayAudioInfo();
                audioInfo.audioSource = playerControllerB.currentVoiceChatAudioSource;
                if (audioInfo.audioSource == null)
                {
                    continue; //Don't add a player with a null audio source
                }

                SetAudioData(ref audioInfo.audioData, audioInfo.audioSource, playerControllerB);
                playerToAudioDict[playerControllerB] = audioInfo; //This will add or update depending on if the key already exists
                Debug.Log($"Getting the audio source: {playerControllerB},{playerControllerB.currentVoiceChatAudioSource}");
            }
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
        private static void SetAudioData(ref AudioData audioData, AudioSource audioSource, PlayerControllerB playerControllerB)
        {
            audioData.playerPitch = audioSource.pitch;
            audioData.volume = audioSource.volume;
            audioData.spatialBlend = audioSource.spatialBlend;
            audioData.panStero = 0f;
            audioData.lowPassFilter = audioSource.GetComponent<AudioLowPassFilter>().enabled;
            audioData.highPassFilter = audioSource.GetComponent<AudioHighPassFilter>().enabled;
        }
        
        private static void SetAudioSource(ref AudioSource audioSource, AudioData audioData)
        {
            audioSource.pitch = audioData.playerPitch;
            audioSource.volume = audioData.volume;
            audioSource.spatialBlend = audioData.spatialBlend;
            audioSource.panStereo = audioData.panStero;
            audioSource.GetComponent<AudioLowPassFilter>().enabled = audioData.lowPassFilter;
            audioSource.GetComponent<AudioHighPassFilter>().enabled = audioData.highPassFilter;
        }
    }

    namespace Patches
    {
        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        internal class QuickMenuManager_Patches
        {
            private static void Postfix(QuickMenuManager __instance)
            {
                if (DeadAndBored.Instance == null)
                {
                    GameObject obj = new("DeadAndBoredObject");
                    DeadAndBored deadAndBored = obj.AddComponent<DeadAndBored>();
                }
            }
        }
    }
}