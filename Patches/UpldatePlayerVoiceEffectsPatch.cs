﻿using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace DeadAndBored.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
    internal class UpdatePlayerVoiceEffectsPatch
    {
        private static bool updateStarted = false;
        private static Dictionary<PlayerControllerB, AudioConfig> configs = new Dictionary<PlayerControllerB, AudioConfig>();

        public static Dictionary<PlayerControllerB, AudioConfig> Configs { get => configs; }

        [HarmonyBefore(new string[] { "BiggerLobby" })]
        private static void Prefix()
        {
            if (configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();

            if (!updateStarted)
            {
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
                            DeadAndBoredObject.DABLogging($"------- -- - -- ADD Player to configs: {playerControllerB.NetworkObjectId}");

                            DeadAndBoredObject.DABLogging("-------------------------------");

                            configs.Add(playerControllerB,
                                new AudioConfig(
                                        playerControllerB,
                                        true,
                                        false,
                                        currentVoiceChatAudioSource.panStereo = 0f,
                                        1,
                                        1,
                                        1,
                                        false,
                                        1
                                    )
                            ) ;
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

                DeadAndBoredObject.DABLogging($"PlayerControllerB: {playerControllerB.NetworkObjectId}");
                //Makes it so that if you are dead, and someone else is talking as an enemy, you won't hear them
                if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && 
                    playerControllerB.isPlayerDead && 
                    configs[playerControllerB].EnemyT != null &&
                    !Configuration.Config.hearOtherDeadTeammates)
                {
                    playerControllerB.currentVoiceChatAudioSource.spatialBlend = 1f;
                    playerControllerB.currentVoiceChatIngameSettings.set2D = false;
                    playerControllerB.voicePlayerState.Volume = 0f;
                    DeadAndBoredObject.DABLogging($"Stopping volume of Player {playerControllerB.NetworkObjectId}");
                    continue;
                }

                DeadAndBoredObject.DABLogging($"---------------------------------------------------------");
                DeadAndBoredObject.DABLogging($"Player Controlled: {playerControllerB.isPlayerControlled}");
                DeadAndBoredObject.DABLogging($"Is Player Dead: {playerControllerB.isPlayerDead}");
                DeadAndBoredObject.DABLogging($"Local Player: {GameNetworkManager.Instance.localPlayerController}");
                DeadAndBoredObject.DABLogging($"---------------------------------------------------------");
                if ((playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead) && !(playerControllerB == GameNetworkManager.Instance.localPlayerController))
                {
                    DeadAndBoredObject.DABLogging($"Current Voice Chat Audio Source: {playerControllerB.currentVoiceChatAudioSource != null}");
                    if (playerControllerB.currentVoiceChatAudioSource == null) continue;
                    AudioSource currentVoiceChatAudioSource = playerControllerB.currentVoiceChatAudioSource;

                    DeadAndBoredObject.DABLogging($"Enemy Voice Chat Audio Source: {configs[playerControllerB].EnemyT != null}");
                    if (configs[playerControllerB].EnemyT != null)
                    {
                        currentVoiceChatAudioSource.transform.position = configs[playerControllerB].EnemyT.position;

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

                        playerControllerB.currentVoiceChatIngameSettings.set2D = config.Set2D;
                        playerControllerB.voicePlayerState.Volume = config.Volume;
                        playerControllerB.currentVoiceChatAudioSource.volume = config.Volume;
                    }
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