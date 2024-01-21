using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace DeadAndBored.Configuration
{
    internal static class Config
    {
        private const string CONFIG_FILE_NAME = "DeadAndBored.cfg";

        private static ConfigFile config;
        private static ConfigEntry<KeyCode> deadAndTalkingKeyConfig;
        private static ConfigEntry<bool> enableTooltipConfig;
        private static ConfigEntry<bool> hearOtherTeammates;
        private static ConfigEntry<bool> enableLogging;
        private static ConfigEntry<KeyCode> manuallyResetAudioDataConfig;

        public static void Init()
        {
            var filePath = Path.Combine(Paths.ConfigPath, CONFIG_FILE_NAME);
            config = new ConfigFile(filePath, true);
            deadAndTalkingKeyConfig = config.Bind("Config", "Key To Talk", KeyCode.Y, "Key press to talk as enemy (when spectating enemy).");
            enableTooltipConfig = config.Bind("Config", "Enable Tooltip", true, "Enable the tooltip menu");
            hearOtherTeammates = config.Bind("Config", "Hear Other Dead Teammates While They Are Talking As An Enemy", false, "With this set to False, you will not hear your other dead teammates when they are talking as an enemy. You will hear them again when they stop talking as an enemy. Note there is a delay so you may still hear/not hear them for a brief period of time.");
            enableLogging = config.Bind("Config", "Enable Debug Logging", false, "Set to true for debugging");
            manuallyResetAudioDataConfig = config.Bind("Config", "If a player loses audio, they can press this key to reset their audio data.", KeyCode.U, "Key press to manually reset audio data");
        }

        public static KeyCode deadAndTalkingKey => deadAndTalkingKeyConfig.Value;
        public static bool enableTooltip => enableTooltipConfig.Value;
        public static bool hearOtherDeadTeammates => hearOtherTeammates.Value;
        public static bool enableDebugLogging => enableLogging.Value;

        public static KeyCode manuallyResetAudioData => manuallyResetAudioDataConfig.Value;
    }
}