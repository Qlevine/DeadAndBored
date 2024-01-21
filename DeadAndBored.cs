using BepInEx;
using UnityEngine;
using SpectateEnemy;
using GameNetcodeStuff;
using UnityEngine.SceneManagement;
using DeadAndBored.Patches;
using DeadAndBored.Configuration;

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
                    if (UnityInput.Current.GetKeyDown(Config.deadAndTalkingKey) && !isDeadAndTalking) //Begin talking as enemy
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
                if (UnityInput.Current.GetKeyUp(Config.deadAndTalkingKey) && isDeadAndTalking) //We let go of our proximity chat button so stop talk
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
            LC_API.Networking.Network.Broadcast<BroadcastParameters>(deadAndTalkingUniqueName, param);
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
            LC_API.Networking.Network.Broadcast<string>(deadAndStopTalkingUniqueName, controllerName.ToString());
            DABLogging($"Stop talk for player: {controllerName}");
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        public void Init()
        {
            System.Action<ulong, BroadcastParameters> talkAction = (sender, args) => { TheDeadTalk(args); };
            System.Action<ulong, string> stopTalkAction = (sender, args) => { TheDeadStopTalk(args); };
            LC_API.Networking.Network.RegisterMessage<BroadcastParameters>(deadAndTalkingUniqueName, false, talkAction);
            LC_API.Networking.Network.RegisterMessage<string>(deadAndStopTalkingUniqueName, false, stopTalkAction);
            Reset();
        }

        public void Reset()
        {
            DABLogging("CALLING RESET");
            isDeadAndTalking = false;
            UpdatePlayerVoiceEffectsPatch.Configs.Clear();
        }

        private void TheDeadTalk(BroadcastParameters param)
        {
            PlayerControllerB talkingPlayer = FindPlayerByNetworkID(param.controllerName);
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
                }
                UpdatePlayerVoiceEffectsPatch.Configs[talkingPlayer] = audioInfo;
            }
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        private void TheDeadStopTalk(string controllerID)
        {

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
            UpdatePlayerVoiceEffectsPatch.Configs[controller] = audioInfo;
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
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
