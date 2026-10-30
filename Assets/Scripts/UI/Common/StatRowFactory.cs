using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Shared factory for building stat HUD rows with icon + tooltip + value text.
    /// Used by both CombatUI and ExpeditionMapUI.
    /// </summary>
    public static class StatRowFactory
    {
        public static TextMeshProUGUI Create(Transform parent, Sprite iconSprite, string fallbackText, Color chipColor, string tooltip, string initialVal, Color valColor, TooltipPopup targetPopup = null, Vector2 customIconSize = default)
        {
            GameObject rowObj = new GameObject($"StatRow_{fallbackText}", typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);
            RectTransform rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(300f, 44f);

            Image rowHitImg = rowObj.AddComponent<Image>();
            rowHitImg.color = Color.clear;
            rowHitImg.raycastTarget = true;

            HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Icon Container
            GameObject chipObj = new GameObject("IconChip", typeof(RectTransform));
            chipObj.transform.SetParent(rowObj.transform, false);
            RectTransform chipRt = chipObj.GetComponent<RectTransform>();

            Vector2 iconSize = (customIconSize != default && customIconSize != Vector2.zero) ? customIconSize : new Vector2(44f, 44f);
            if (iconSprite != null)
            {
                chipRt.sizeDelta = iconSize;
                Image iconImg = chipObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = true;
            }
            else
            {
                chipRt.sizeDelta = new Vector2(52f, 36f);
                Image chipBg = chipObj.AddComponent<Image>();
                chipBg.color = chipColor;
                chipBg.raycastTarget = true;

                GameObject chipTxtObj = new GameObject("Icon", typeof(RectTransform));
                chipTxtObj.transform.SetParent(chipObj.transform, false);
                TextMeshProUGUI chipTxt = chipTxtObj.AddComponent<TextMeshProUGUI>();
                chipTxt.text = fallbackText;
                chipTxt.fontSize = 15;
                chipTxt.fontStyle = FontStyles.Bold;
                chipTxt.alignment = TextAlignmentOptions.Center;
                chipTxt.color = Color.white;
                chipTxt.raycastTarget = false;
                RectTransform ctr = chipTxtObj.GetComponent<RectTransform>();
                ctr.anchorMin = Vector2.zero;
                ctr.anchorMax = Vector2.one;
                ctr.sizeDelta = Vector2.zero;
            }

            // Attach tooltip to chip & row
            StatTooltipTrigger tt = chipObj.AddComponent<StatTooltipTrigger>();
            tt.tooltipText = tooltip;
            tt.targetPopup = targetPopup;

            StatTooltipTrigger rowTt = rowObj.AddComponent<StatTooltipTrigger>();
            rowTt.tooltipText = tooltip;
            rowTt.targetPopup = targetPopup;

            // Value text (large, crisp font with dark outline for maximum legibility over transparent background)
            GameObject valObj = new GameObject("Value", typeof(RectTransform));
            valObj.transform.SetParent(rowObj.transform, false);
            TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
            valTxt.text = initialVal;
            valTxt.fontSize = 24;
            valTxt.fontStyle = FontStyles.Bold;
            valTxt.alignment = TextAlignmentOptions.MidlineLeft;
            valTxt.color = valColor;
            valTxt.textWrappingMode = TextWrappingModes.NoWrap;
            valTxt.raycastTarget = false;

            Outline valOutline = valObj.AddComponent<Outline>();
            valOutline.effectColor = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            valOutline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform valRt = valObj.GetComponent<RectTransform>();
            valRt.sizeDelta = new Vector2(230f, 44f);

            return valTxt;
        }
    }
}
