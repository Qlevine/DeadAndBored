
using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using SpectateEnemy;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace DeadAndBored
{
    [BepInPlugin("DeadAndBored", "DeadAndBored", "1.0")]
    public class Plugin : BaseUnityPlugin
    {

        //Keycode used to begin talking when spectating an enemy
        private BepInEx.Configuration.ConfigEntry<KeyCode> pressToTalkAsEnemyKey; 

        //Dictionary from online player to audio source/enemy position
        private static Dictionary<PlayerControllerB, PlayAudioInfo> playerToAudioDict;

        //Used to determine if this online player is currently talking as an enemy
        public static bool isDeadAndTalking = false;

        private bool wasSpectatingEnemies = false;
        private GameObject oldSpectatingEnemy = null;

        public struct BroadcastParameters
        {
            public PlayerControllerB controller;
            public Transform enemyTransform;
        }

        public struct PlayAudioInfo
        {
            public AudioSource audioSource;
            public Transform enemyTransform;
            public Vector3 originalPosition;
        }

        private Harmony harmony;

        public static void Talk(BroadcastParameters param)
        {
            isDeadAndTalking = true;
            GameNetworkManager.Instance.BroadcastMessage("TheDeadTalk", param);
        }

        public static void StopTalk(PlayerControllerB controller)
        {
            isDeadAndTalking = false;
            GameNetworkManager.Instance.BroadcastMessage("TheDeadStopTalk", controller);
        }

        private void Awake()
        {
            pressToTalkAsEnemyKey = this.Config.Bind<KeyCode>("Config", "Key To Activate Enemy Proximity Chat", KeyCode.T);
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo("Dead And Bored loaded!");
        }

        private void Start()
        {
            SceneManager.activeSceneChanged += ResetSceneData;
        }

        private void Update()
        {
            if(oldSpectatingEnemy != SpectateEnemiesAPI.CurrentEnemySpectating()) //We changed which enemy we are spectating
            {
                oldSpectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                if (isDeadAndTalking) //If they changed who they are spectating, then they should have to re-press the talk key
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController);
                }
            }
            if(wasSpectatingEnemies != SpectateEnemiesAPI.IsSpectatingEnemies) //We changed if we are spectating humans or enemies
            {
                wasSpectatingEnemies = SpectateEnemiesAPI.IsSpectatingEnemies;
                if (isDeadAndTalking)
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController);
                }
            }

            if(GameNetworkManager.Instance == null)
            {
                return; //Early exit if not online
            }

            if(GameNetworkManager.Instance.localPlayerController == null)
            {
                return; //Early exit if the local player is not setup
            }

            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
            {
                if (SpectateEnemiesAPI.IsSpectatingEnemies)
                {
                    if (UnityInput.Current.GetKeyDown(pressToTalkAsEnemyKey.Value) && !isDeadAndTalking) //Begin talking as enemy
                    {
                        BroadcastParameters param = new BroadcastParameters();
                        param.controller = GameNetworkManager.Instance.localPlayerController;
                        GameObject spectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                        if(spectatingEnemy != null)
                        {
                            param.enemyTransform = spectatingEnemy.transform;
                            Talk(param);
                        }
                    }
                }
                if (UnityInput.Current.GetKeyUp(pressToTalkAsEnemyKey.Value) && isDeadAndTalking) //We let go of our proximity chat button so stop talk
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController);
                }
            }

            foreach(PlayAudioInfo audioInfo in playerToAudioDict.Values)
            {
                if(audioInfo.enemyTransform == null)
                {
                    continue; //Ignore players that are not actively talking
                }
                if(audioInfo.audioSource == null)
                {
                    continue; //This shouldn't be possible
                }
                Debug.Log("Found dead and talking player!");
                audioInfo.audioSource.transform.position = audioInfo.enemyTransform.position;
            }
        }

        private void TheDeadTalk(BroadcastParameters param)
        {
            PlayerControllerB talkingPlayer = param.controller;
            Debug.Log($"The dead talk: {talkingPlayer}");
            if(talkingPlayer == null)
            {
                return;
            }
            SetupAudioDictionary();
            if (playerToAudioDict.ContainsKey(talkingPlayer))
            {
                PlayAudioInfo audioInfo = playerToAudioDict[talkingPlayer];
                audioInfo.originalPosition = audioInfo.audioSource.transform.position;
                audioInfo.enemyTransform = param.enemyTransform;
                Debug.Log($"Setup enemyTransform correctly!");
            }
        }

        private void TheDeadStopTalk(PlayerControllerB controller)
        {
            Debug.Log($"The dead stop talk: {controller}");
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
            audioInfo.enemyTransform = null;
            audioInfo.audioSource.transform.position = audioInfo.originalPosition;
            Debug.Log("Stopped audio correctly!");
        }

        private void ResetSceneData(Scene oldScene, Scene newScene)
        {
            playerToAudioDict.Clear();
            Logger.LogInfo("Active scene change. Clearing dictionary");
        }

        private static void SetupAudioDictionary()
        {
            Debug.Log("Begin setup dictionary");
            if(playerToAudioDict == null)
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
                if(audioInfo.audioSource == null)
                {
                    continue; //Don't add a player with a null audio source
                }
                playerToAudioDict[playerControllerB] = audioInfo; //This will add or update depending on if the key already exists
                Debug.Log($"Getting the audio source: {playerControllerB},{playerControllerB.currentVoiceChatAudioSource}");
            }
        }
    }
}