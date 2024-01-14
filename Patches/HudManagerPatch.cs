using HarmonyLib;
using UnityEngine;
using TMPro;
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

                if (DeadAndBored.DeadAndBoredObject.Instance.isDeadAndTalking)
                {
                    __instance.holdButtonToEndGameEarlyText.text += $"\n\n<color=#1c73ff>Talk As Enemy: [{Config.deadAndTalkingKey.ToString()}]</color>";
                }
                else
                {
                    __instance.holdButtonToEndGameEarlyText.text += $"\n\n\n\nTalk As Enemy: [{Config.deadAndTalkingKey.ToString()}]";
                }
            }
        }

        //private static void CreateDescriptionText(HUDManager instance)
        //{
        //    TextMeshProUGUI endGameEarlyText = instance.holdButtonToEndGameEarlyText;
        //    Transform textParent = endGameEarlyText.transform.GetParent();

        //    GameObject newTextObj = new GameObject("DeadAndBoredText");
        //    newTextObj.transform.parent = textParent;
        //    TextMeshProUGUI newText = newTextObj.AddComponent<TextMeshProUGUI>();
        //    RectTransform endGameEarlyTextTransform = endGameEarlyText.rectTransform;
        //    RectTransform rectTransform = newTextObj.GetComponent<RectTransform>();
        //    rectTransform.anchorMin = endGameEarlyTextTransform.anchorMin;
        //    rectTransform.anchorMax = endGameEarlyTextTransform.anchorMax;
        //    rectTransform.pivot = endGameEarlyTextTransform.pivot;
        //    Debug.Log("DEAD AND BORED: Creating new description text");
        //    Debug.Log($"DEAD AND BORED: Old Anchor = {endGameEarlyTextTransform.anchoredPosition}");
        //    Debug.Log($"DEAD AND BORED: Old Anchor Min = {endGameEarlyTextTransform.anchorMin}");
        //    Debug.Log($"DEAD AND BORED: Old Anchor Max = {endGameEarlyTextTransform.anchorMax}");
        //    Debug.Log($"DEAD AND BORED: Old Font Size = {endGameEarlyText.fontSize}");
        //    Debug.Log($"DEAD AND BORED: Old sizeDelta.y = {endGameEarlyTextTransform.sizeDelta.y}");
        //    rectTransform.anchoredPosition = new Vector2(
        //        endGameEarlyTextTransform.anchoredPosition.x,
        //        endGameEarlyTextTransform.anchoredPosition.y - endGameEarlyTextTransform.sizeDelta.y - 20 //for padding
        //        );
        //    rectTransform.sizeDelta = new Vector2(300, 20);

        //    newText.SetText($"Press {Config.deadAndTalkingKey.ToString()} to talk as enemy");
        //    newText.color = Color.white;
        //    newText.fontSize = endGameEarlyText.fontSize;
        //    descriptionText = newText;
        //}
    }

}
