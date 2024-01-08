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

        public static void Init()
        {
            var filePath = Path.Combine(Paths.ConfigPath, CONFIG_FILE_NAME);
            config = new ConfigFile(filePath, true);
            deadAndTalkingKeyConfig = config.Bind("Config", "Key To Talk", KeyCode.T, "Key press to talk as enemy.");
        }

        public static KeyCode deadAndTalkingKey => deadAndTalkingKeyConfig.Value;
    }
}