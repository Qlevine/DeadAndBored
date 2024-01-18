using HarmonyLib;
using DeadAndBored.Configuration;

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
                    if (DeadAndBoredObject.Instance.isDeadAndTalking)
                    {
                        __instance.holdButtonToEndGameEarlyText.text += $"\n\n<color=#1c73ff>Talk As Enemy: [{Config.deadAndTalkingKey.ToString()}]</color>";
                    }
                    else
                    {
                        __instance.holdButtonToEndGameEarlyText.text += $"\n\nTalk As Enemy: [{Config.deadAndTalkingKey.ToString()}]";
                    }
                }
            }
        }
    }

}
