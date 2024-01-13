using UnityEngine;
using HarmonyLib;

namespace DeadAndBored.Patches
{
    [HarmonyPatch(typeof(QuickMenuManager), "Start")]
    internal class QuickMenuManager_Patches
    {
        private static void Postfix(QuickMenuManager __instance)
        {
            if (DeadAndBoredObject.Instance == null)
            {
                GameObject obj = new("DeadAndBoredObject");
                obj.AddComponent<DeadAndBoredObject>();
            }
        }
    }
}
