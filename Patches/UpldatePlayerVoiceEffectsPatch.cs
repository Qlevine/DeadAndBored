﻿using Dissonance;
using GameNetcodeStuff;
using HarmonyLib;
using DeadAndBored.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace DeadAndBored.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
    class UpdatePlayerVoiceEffectsPatch
    {
        private static bool updateStarted = false;
        private static Dictionary<PlayerControllerB, AudioConfig> configs = new Dictionary<PlayerControllerB, AudioConfig>();

        public static Dictionary<PlayerControllerB, AudioConfig> Configs { get => configs; }

        [HarmonyBefore(new string[] { "BiggerLobby" })]
        private static void Prefix()
        {
            Debug.Log("Prefix Update Player Voice Effects");
            if (configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();

            if (!updateStarted)
            {
                Debug.Log("Starting Update Coroutine");
                HUDManager.Instance.StartCoroutine(UpdateNumerator());
                updateStarted = true;
            }

            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                return;

            if (StartOfRound.Instance == null || StartOfRound.Instance.allPlayerScripts == null)
                return;

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[i];

                if (playerControllerB == null) continue;

                if ((playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead) && (playerControllerB != GameNetworkManager.Instance.localPlayerController))
                {
                    AudioSource currentVoiceChatAudioSource = playerControllerB.currentVoiceChatAudioSource;
                    if (currentVoiceChatAudioSource == null) continue;

                    if (playerControllerB.isPlayerDead)
                    {
                        if (!configs.ContainsKey(playerControllerB))
                        {
                            Debug.Log($"Adding player {playerControllerB.NetworkObjectId} to config");
                            configs.Add(playerControllerB,
                                new AudioConfig(
                                        playerControllerB,
                                        currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled,
                                        currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled,
                                        currentVoiceChatAudioSource.panStereo = 0f,
                                        SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)playerControllerB.playerClientId)],
                                        GetPitch(playerControllerB),
                                        currentVoiceChatAudioSource.spatialBlend,
                                        playerControllerB.currentVoiceChatIngameSettings.set2D,
                                        playerControllerB.voicePlayerState.Volume
                                    )
                            );
                        }
                    }
                }

            }
        }

        private static void Postfix()
        {
            if (configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();

            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                return;

            foreach (var playerControllerB in configs.Keys.ToArray())
            {
                if (playerControllerB == null) continue;

                AudioConfig config = configs[playerControllerB];

                if (config == null) continue;

                if ((playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead) && !(playerControllerB == GameNetworkManager.Instance.localPlayerController))
                {
                    if (playerControllerB.currentVoiceChatAudioSource == null) continue;
                    AudioSource currentVoiceChatAudioSource = playerControllerB.currentVoiceChatAudioSource;

                    if (configs[playerControllerB].EnemyT != null)
                    {
                        Debug.Log($"Setting audio from player {playerControllerB.NetworkObjectId} to enemyT");
                        currentVoiceChatAudioSource.transform.position = configs[playerControllerB].EnemyT.position;
                    }


                    currentVoiceChatAudioSource.panStereo = config.PanStereo;
                    currentVoiceChatAudioSource.spatialBlend = config.SpatialBlend;

                    AudioLowPassFilter lowPassFilter = currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>();
                    AudioHighPassFilter highPassFilter = currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>();

                    if (lowPassFilter != null) lowPassFilter.enabled = config.LowPassFilter;
                    if (highPassFilter != null) highPassFilter.enabled = config.HighPassFilter;


                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)playerControllerB.playerClientId)] = config.PlayerVoicePitchTargets;
                        SoundManager.Instance.SetPlayerPitch(config.PlayerPitch, unchecked((int)playerControllerB.playerClientId));
                    }

                    playerControllerB.voicePlayerState.Volume = config.Volume;
                    playerControllerB.currentVoiceChatAudioSource.volume = config.Volume;
                }
                else if (!playerControllerB.isPlayerDead)
                {
                    configs.Remove(playerControllerB);
                }
            }
        }

        private static IEnumerator UpdateNumerator()
        {
            yield return 0;

            while (true)
            {
                UpdatePlayersStatus();
                yield return new WaitForFixedUpdate();
            }
        }
        private static void UpdatePlayersStatus()
        {

            if (configs == null)
                return;

            bool voiceEffectsNeedsUpdate = false;


            foreach (var player in configs.ToArray())
            {

                if (player.Key == null) continue;

                if (!player.Key.isPlayerDead)
                {
                    configs.Remove(player.Key);
                    voiceEffectsNeedsUpdate = true;
                    continue;
                }
                else if (player.Value.EnemyT != null && player.Value.AudioSourceT != null)
                {
                    Debug.Log($"Setting the position of audio to EnemyT for player {player.Key.NetworkObjectId}");
                    player.Value.AudioSourceT.position = player.Value.EnemyT.position;
                }
            }

            if (voiceEffectsNeedsUpdate) StartOfRound.Instance.UpdatePlayerVoiceEffects();

        }

        private static float GetPitch(PlayerControllerB playerControllerB)
        {
            int playerObjNum = (int)playerControllerB.playerClientId;
            float pitch;
            SoundManager.Instance.diageticMixer.GetFloat($"PlayerPitch{playerObjNum}", out pitch);
            return pitch;
        }
    }
}