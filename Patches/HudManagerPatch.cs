using HarmonyLib;
using DeadAndBored.Configuration;
using LethalCompanyInputUtils.Api;
using BepInEx.Bootstrap;
using UnityEngine.InputSystem;

namespace DeadAndBored.Patches
{
    [HarmonyPatch(typeof(HUDManager), "Update")]
    internal class HUDManagerPatch
    {
        private static void Postfix(HUDManager __instance)
        {
            if (SpectateEnemy.SpectateEnemiesAPI.IsSpectatingEnemies)
            {
                if (Config.enableTooltip)
                {
                    string key = GetButtonString(Plugin.inputActions.TalkKey.bindings[0].effectivePath);
                    if (DeadAndBoredObject.Instance.isDeadAndTalking)
                    {
                        __instance.holdButtonToEndGameEarlyText.text += $"\n\n<color=#1c73ff>Talk As Enemy: [{key}]</color>";
                    }
                    else
                    {
                        __instance.holdButtonToEndGameEarlyText.text += $"\n\nTalk As Enemy: [{key}]";
                    }
                }
            }
        }

        private static string GetButtonString(string path)
        {
            string key = Config.deadAndTalkingKey.ToString();
            if (DeadAndBoredObject.IsInputUtilsInstalled())
            {
                key = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            }

            return key;
        }
    }

}
