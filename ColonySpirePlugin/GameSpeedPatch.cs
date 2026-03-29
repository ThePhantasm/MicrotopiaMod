using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColonySpireMod
{
    // Inject speed controls in both MainMenu (Title Screen) and UIGame (HUD)
    [HarmonyPatch]
    public static class GameSpeedPatch
    {
        public static Image btn1xBg, btn2xBg, btn4xBg;
        public static Image mmbtn1xBg, mmbtn2xBg, mmbtn4xBg;
        
        [HarmonyPatch(typeof(UIGame), "OnEnable")]
        [HarmonyPostfix]
        static void PostfixUIGame(UIGame __instance)
        {
            try
            {
                if (btn1xBg != null && btn1xBg.gameObject != null) {
                    UpdateSelection();
                    return;
                }

                // Create a container explicitly for HUD
                var containerObj = new GameObject("GameSpeedUI_HUD");
                var containerRt = containerObj.AddComponent<RectTransform>();
                containerRt.SetParent(__instance.transform, false);
                containerRt.anchorMin = new Vector2(0.5f, 0.95f); // Top center
                containerRt.anchorMax = new Vector2(0.5f, 0.95f);
                containerRt.pivot = new Vector2(0.5f, 1f);
                containerRt.anchoredPosition = new Vector2(0, -10f);
                containerRt.localScale = Vector3.one;
                
                var hlGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
                hlGroup.childControlWidth = true;
                hlGroup.childControlHeight = true;
                hlGroup.childForceExpandWidth = false;
                hlGroup.childForceExpandHeight = false;
                hlGroup.spacing = 15f; 

                // Try to get font
                TMP_FontAsset font = null;
                var lbAntCountField = AccessTools.Field(typeof(UIGame), "lbAntCount");
                if (lbAntCountField != null) {
                    var antCount = lbAntCountField.GetValue(__instance) as TextMeshProUGUI;
                    if (antCount != null) font = antCount.font;
                }

                btn1xBg = CreateRawSpeedButton("1x", 1f, containerRt, font);
                btn2xBg = CreateRawSpeedButton("2x", 2f, containerRt, font);
                btn4xBg = CreateRawSpeedButton("4x", 4f, containerRt, font);
                
                containerObj.SetActive(true);
                UpdateSelection();
                Debug.Log("[SpeedupFeature] Game speed options injected into UIGame HUD.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpeedupFeature] Exception in HUD: {ex}");
            }
        }
        
        public static void UpdateSelection()
        {
            float speed = Time.timeScale;
            
            Color normalColor = new Color(0.15f, 0.25f, 0.45f, 1f);
            Color selectedColor = new Color(0.3f, 0.7f, 1.0f, 1f);
            
            if (btn1xBg != null) btn1xBg.color = (speed == 1f ? selectedColor : normalColor);
            if (btn2xBg != null) btn2xBg.color = (speed == 2f ? selectedColor : normalColor);
            if (btn4xBg != null) btn4xBg.color = (speed >= 4f ? selectedColor : normalColor);

            if (mmbtn1xBg != null) mmbtn1xBg.color = (speed == 1f ? selectedColor : normalColor);
            if (mmbtn2xBg != null) mmbtn2xBg.color = (speed == 2f ? selectedColor : normalColor);
            if (mmbtn4xBg != null) mmbtn4xBg.color = (speed >= 4f ? selectedColor : normalColor);
        }

        static Image CreateRawSpeedButton(string text, float speedMultiplier, Transform parent, TMP_FontAsset font)
        {
            // Root Object
            var btnGo = new GameObject($"BtnSpeed_{text}");
            var rt = btnGo.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(50f, 35f);
            rt.localScale = Vector3.one;

            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredWidth = 50f;
            le.preferredHeight = 35f;
            le.minWidth = 50f;
            le.minHeight = 35f;

            // Background & Interaction
            var img = btnGo.AddComponent<Image>();
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                Time.timeScale = speedMultiplier;
                UpdateSelection();
                Debug.Log($"[SpeedupFeature] Game speed clicked -> {speedMultiplier}x");
            });

            // Text Object
            var textGo = new GameObject("Text");
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.SetParent(btnGo.transform, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = Vector2.zero;
            textRt.localScale = Vector3.one;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            
            if (font != null) {
                tmp.font = font;
            }

            return img;
        }
    }
}
