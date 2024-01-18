using HarmonyLib;
using UnityEngine;

namespace DeadAndBored.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "Start")]
    internal class StartOfRoundPatch
    {
        private static void Postfix()
        {
            Debug.Log($"PostFix Start: {DeadAndBoredObject.Instance != null}");
            if(DeadAndBoredObject.Instance != null)
            {
                DeadAndBoredObject.Instance.Reset();
            }

        }
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
    internal class ShipLeavePatch
    {
        private static void Postfix()
        {
            Debug.Log($"Postfix Leave: {DeadAndBoredObject.Instance != null}");
            if(DeadAndBoredObject.Instance != null)
            {
                DeadAndBoredObject.Instance.Reset();
            }
        }
    }
}
