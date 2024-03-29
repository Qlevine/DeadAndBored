﻿using BepInEx;
using UnityEngine;
using SpectateEnemy;
using GameNetcodeStuff;
using UnityEngine.SceneManagement;
using DeadAndBored.Patches;
using DeadAndBored.Configuration;
using BepInEx.Bootstrap;
using Unity.Netcode;

namespace DeadAndBored
{
    internal class BroadcastParameters
    {
        public string controllerName;
        public string enemyName;
        public float x;
        public float y;
        public float z;
    }

    internal class DeadAndBoredObject : MonoBehaviour
    {

        public static void DABLogging(string logString)
        {
            if (Config.enableDebugLogging)
            {
                Debug.Log($"DEAD AND BORED: {logString}");
            }
        }

        public static bool IsInputUtilsInstalled()
        {
            return Chainloader.PluginInfos.ContainsKey("com.rune580.LethalCompanyInputUtils");
        }

        public static DeadAndBoredObject Instance = null;

        //Both of these are used to disable network talking if the user changes which enemy they are spectating
        private bool wasSpectatingEnemies = false;
        private GameObject oldSpectatingEnemy = null;

        //Used to determine if this online player is currently talking as an enemy
        public bool isDeadAndTalking = false;
        private bool wasPushToTalk = false;

        //Used for sending and recieving network commands
        private static string deadAndTalkingUniqueName = "TheDeadTalk";
        private static string deadAndStopTalkingUniqueName = "TheDeadStopTalk";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();

            SceneManager.activeSceneChanged += OnSceneChanged;
            Debug.Log($"DeadAndBored Debugger: {Configuration.Config.enableDebugLogging}");
        }

        private void OnSceneChanged(Scene old, Scene newScene)
        {
            Reset();
        }

        private void Update()
        {
            //Prioritize the input system. Otherwise use configs
            if (IsInputUtilsInstalled())
            {
                if (Plugin.inputActions.ResetAudio.triggered)
                {
                    Reset();
                }
            }
            //else if (UnityInput.Current.GetKeyDown(Configuration.Config.manuallyResetAudioData))
            //{
            //    Reset();
            //}


            if (!SpectateEnemiesAPI.IsLoaded)
            {
                return;
            }
            if (oldSpectatingEnemy != SpectateEnemiesAPI.CurrentEnemySpectating()) //We changed which enemy we are spectating
            {
                oldSpectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                if (isDeadAndTalking) //If they changed who they are spectating, then they should have to re-press the talk key
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
            if (wasSpectatingEnemies != SpectateEnemiesAPI.IsSpectatingEnemies) //We changed if we are spectating humans or enemies
            {
                wasSpectatingEnemies = SpectateEnemiesAPI.IsSpectatingEnemies;
                if (isDeadAndTalking)
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
            if (GameNetworkManager.Instance == null)
            {
                return; //Early exit if not online
            }
            if (GameNetworkManager.Instance.localPlayerController == null)
            {
                return; //Early exit if the local player is not setup
            }
            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
            {
                if (SpectateEnemiesAPI.IsSpectatingEnemies)
                {
                    //Prioritize the input system. Otherwise use configs
                    bool shouldTalk = false;
                    if (IsInputUtilsInstalled())
                    {
                        if (Plugin.inputActions.TalkKey.triggered && !isDeadAndTalking)
                        {
                            shouldTalk = true;
                        }
                    }
                    //else if(UnityInput.Current.GetKeyDown(Config.deadAndTalkingKey) && !isDeadAndTalking)
                    //{
                    //    shouldTalk = true;
                    //}


                    if (shouldTalk) //Begin talking as enemy
                    {
                        BroadcastParameters param = new BroadcastParameters();
                        param.controllerName = GameNetworkManager.Instance.localPlayerController.NetworkObjectId.ToString();
                        GameObject spectatingEnemy = SpectateEnemiesAPI.CurrentEnemySpectating();
                        if (spectatingEnemy != null)
                        {
                            param.enemyName = spectatingEnemy.name;
                            param.x = spectatingEnemy.transform.position.x;
                            param.y = spectatingEnemy.transform.position.y;
                            param.z = spectatingEnemy.transform.position.z;
                            Talk(param);
                        }
                    }
                }

                //Prioritize the input system. Otherwise use configs
                bool shouldStopTalk = false;
                if (IsInputUtilsInstalled())
                {
                    if (Plugin.inputActions.TalkKey.IsPressed() == false && isDeadAndTalking)
                    {
                        shouldStopTalk = true;
                    }
                }
                //else if (UnityInput.Current.GetKeyDown(Config.deadAndTalkingKey) && isDeadAndTalking)
                //{
                //    shouldStopTalk = true;
                //}
                if (shouldStopTalk) //We let go of our proximity chat button so stop talk
                {
                    StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
                }
            }
        }

        private void Talk(BroadcastParameters param)
        {
            if (IngamePlayerSettings.Instance.settings.pushToTalk) //We want to force talking here so they don't have to press two buttons
            {
                IngamePlayerSettings.Instance.settings.pushToTalk = false;
                wasPushToTalk = true;
            }
            isDeadAndTalking = true;
            string json = JsonUtility.ToJson(param);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            NetworkUtils.instance.SendToAll(deadAndTalkingUniqueName, bytes);
            DABLogging($"Begin talk for player: {param.controllerName}");
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        private void StopTalk(ulong controllerName)
        {
            if (wasPushToTalk) //We want to force talking here so they don't have to press two buttons
            {
                IngamePlayerSettings.Instance.settings.pushToTalk = true;
                wasPushToTalk = false;
            }
            isDeadAndTalking = false;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(controllerName.ToString());
            NetworkUtils.instance.SendToAll(deadAndStopTalkingUniqueName, bytes);
            DABLogging($"Stop talk for player: {controllerName}");
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        public void Init()
        {
            NetworkUtils.Init();
            NetworkUtils.instance.OnNetworkData = OnRecieveData;
            Reset();
        }

        private void OnRecieveData(string type, byte[] message)
        {
            string messageAsString = System.Text.Encoding.Default.GetString(message, 0, message.Length);
            DABLogging($"Recieved Message: {messageAsString}");
            if (type == deadAndTalkingUniqueName)
            {
                BroadcastParameters broadcastParameters = JsonUtility.FromJson<BroadcastParameters>(messageAsString);
                TheDeadTalk(broadcastParameters);
            }
            else if(type == deadAndStopTalkingUniqueName)
            {
                TheDeadStopTalk(messageAsString);
            }
            else if(type == NetworkUtils.HostRelayID)
            {
                DABLogging($"Host recieved relay message");
                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkUtils.RelayObject relayObject = JsonUtility.FromJson<NetworkUtils.RelayObject>(messageAsString);
                    string tag = relayObject.tag;
                    byte[] data = relayObject.data;

                    NetworkUtils.instance.SendToAll(tag, data);
                }
                else
                {
                    DABLogging("Error: Sent host relay to non-host");
                }
            }
            else
            {
                DABLogging("Invalid message type");
            }
        }

        public void Reset()
        {
			DABLogging("CALLING RESET");
            if (isDeadAndTalking && GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
            {
                StopTalk(GameNetworkManager.Instance.localPlayerController.NetworkObjectId);
            }
            isDeadAndTalking = false;
            if (UpdatePlayerVoiceEffectsPatch.Configs != null)
            {
                UpdatePlayerVoiceEffectsPatch.Configs.Clear();
            }
            if (StartOfRound.Instance != null)
            {
                StartOfRound.Instance.UpdatePlayerVoiceEffects();
                foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
                {
                    if(playerControllerB != null && playerControllerB.currentVoiceChatAudioSource != null)
                    {
                        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
                        {
                            if(GameNetworkManager.Instance.localPlayerController != playerControllerB)
                            {
                                playerControllerB.currentVoiceChatAudioSource.volume = 1;
                            }
                        }
                        else
                        {
                            playerControllerB.currentVoiceChatAudioSource.volume = 1; //Extra precaution to make sure volumes are reset
                        }
                       
                    }
                }
            }
            if (SpectateEnemiesAPI.IsLoaded)
            {
                wasSpectatingEnemies = SpectateEnemiesAPI.IsSpectatingEnemies;
            }
            else
            {
                wasSpectatingEnemies = false;
            }
            wasPushToTalk = false;
            oldSpectatingEnemy = null;
        }

        private void TheDeadTalk(BroadcastParameters param)
        {
            PlayerControllerB talkingPlayer = FindPlayerByNetworkID(param.controllerName);
            DABLogging($"--------------- Talking Player recieved: {param.controllerName}");
            if (talkingPlayer == null)
            {
                return;
            }

            DABLogging($"UpdatePlayerVoiceEffects Contains Player: {UpdatePlayerVoiceEffectsPatch.Configs.ContainsKey(talkingPlayer)}");
            if (UpdatePlayerVoiceEffectsPatch.Configs.ContainsKey(talkingPlayer))
            {
                AudioConfig audioInfo = UpdatePlayerVoiceEffectsPatch.Configs[talkingPlayer];
                GameObject obj = FindEnemyByNameAndPos(param);
                if (obj != null)
                {
                    audioInfo.EnemyT = obj.transform;
                }
                else
                {
                    DABLogging("Could not find enemy from name and position");
                }
                UpdatePlayerVoiceEffectsPatch.Configs[talkingPlayer] = audioInfo;
            }
            DABLogging("-------------- Finishing The Dead Talk");
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        private void TheDeadStopTalk(string controllerID)
        {
            DABLogging("-------------- Start The Dead Stop Talk");
            PlayerControllerB controller = FindPlayerByNetworkID(controllerID);
            DABLogging($"Found Player: {controller != null}");
            if (controller == null)
            {
                return;
            }

            DABLogging($"Config Contains Player: {UpdatePlayerVoiceEffectsPatch.Configs.ContainsKey(controller)}");
            if (!UpdatePlayerVoiceEffectsPatch.Configs.ContainsKey(controller))
            {
                return;
            }
            AudioConfig audioInfo = UpdatePlayerVoiceEffectsPatch.Configs[controller];
            audioInfo.EnemyT = null;
            UpdatePlayerVoiceEffectsPatch.Configs[controller] = audioInfo;
            StartOfRound.Instance.UpdatePlayerVoiceEffects();

            DABLogging("-------------- Finish The Dead Stop Talk");
        }

        private PlayerControllerB FindPlayerByNetworkID(string networkID)
        {
            foreach (PlayerControllerB playerControllerB in FindObjectsByType<PlayerControllerB>(FindObjectsSortMode.None))
            {
                ulong result;
                if (ulong.TryParse(networkID, out result))
                {
                    if(playerControllerB.NetworkObjectId == result)
                    {
                        return playerControllerB;
                    }
                }
                else
                {
                    DABLogging($"Invalid format: {networkID}");
                }
            }

            return null;
        }

        private GameObject FindEnemyByNameAndPos(BroadcastParameters bParams)
        {
            Vector3 enemyPos = new Vector3(bParams.x, bParams.y, bParams.z);
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
                        foundObj = obj;
                        shortestDistance = currDistance;
                    }
                }
            }
            return foundObj;
        }
    }
}
