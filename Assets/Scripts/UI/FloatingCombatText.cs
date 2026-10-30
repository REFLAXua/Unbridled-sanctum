using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnbridledSanctum.UI
{
    public enum CombatTextType
    {
        Damage,        // Orange-Red (-X)
        ShieldBlock,   // Sky-Blue (🛡️ BLOCKED / -X Shield)
        Seduction,     // Hot Pink (+X% 💕)
        ClothesDamage, // Amber-Gold (👗 -X)
        HealOrRepair   // Emerald Green (+X)
    }

    /// <summary>
    /// Spawns animated bouncing floating text inside a Canvas above targets for high game-feel juice.
    /// </summary>
    public class FloatingCombatText : MonoBehaviour
    {
        public static FloatingCombatText Instance { get; private set; }

        private RectTransform canvasContainer;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            canvasContainer = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Spawns a floating combat number or message near targetRect with an explosive bounce and float.
        /// </summary>
        public void Spawn(RectTransform targetRect, string text, CombatTextType type, Vector2 customOffset = default)
        {
            if (targetRect == null && canvasContainer == null) return;

            Transform parent = (canvasContainer != null) ? canvasContainer : targetRect.parent;
            GameObject fObj = new GameObject("FloatingText", typeof(RectTransform));
            fObj.transform.SetParent(parent, false);

            RectTransform frt = fObj.GetComponent<RectTransform>();
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(300f, 60f);

            // Calculate starting position near target
            Vector3 worldPos = targetRect != null ? targetRect.position : Vector3.zero;
            frt.position = worldPos;

            // Apply custom and slight random scatter
            float randomX = Random.Range(-25f, 25f);
            float startOffsetY = (customOffset != default) ? customOffset.y : 120f;
            float startOffsetX = (customOffset != default) ? customOffset.x : 0f;
            frt.anchoredPosition += new Vector2(startOffsetX + randomX, startOffsetY);

            TextMeshProUGUI tmp = fObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Style and color based on type
            switch (type)
            {
                case CombatTextType.Damage:
                    tmp.fontSize = 38;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(1f, 0.28f, 0.10f, 1f); // Vibrant orange-red
                    break;

                case CombatTextType.ShieldBlock:
                    tmp.fontSize = 32;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.35f, 0.88f, 1f, 1f); // Sky blue
                    break;

                case CombatTextType.Seduction:
                    tmp.fontSize = 36;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(1f, 0.35f, 0.75f, 1f); // Hot lust pink
                    break;

                case CombatTextType.ClothesDamage:
                    tmp.fontSize = 30;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(1f, 0.78f, 0.25f, 1f); // Amber gold
                    break;

                case CombatTextType.HealOrRepair:
                    tmp.fontSize = 32;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.35f, 1f, 0.55f, 1f); // Emerald green
                    break;
            }

            StartCoroutine(AnimateRoutine(fObj, frt, tmp));
        }

        private IEnumerator AnimateRoutine(GameObject obj, RectTransform rt, TextMeshProUGUI text)
        {
            float duration = 0.95f;
            float elapsed = 0f;

            Vector2 startPos = rt.anchoredPosition;
            Vector2 targetPos = startPos + new Vector2(Random.Range(-15f, 15f), 90f);

            Color baseColor = text.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Bounce Scale: pop in explosive (1.45x -> 1.0x with overshoot)
                float scale;
                if (t < 0.25f)
                {
                    float popT = t / 0.25f;
                    scale = Mathf.Lerp(0.5f, 1.45f, popT);
                }
                else if (t < 0.45f)
                {
                    float settleT = (t - 0.25f) / 0.20f;
                    scale = Mathf.Lerp(1.45f, 1.0f, settleT);
                }
                else
                {
                    scale = 1.0f;
                }
                rt.localScale = new Vector3(scale, scale, 1f);

                // Upward Float motion with smooth deceleration
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));

                // Fade out near the end
                if (t > 0.55f)
                {
                    float fadeT = (t - 0.55f) / 0.45f;
                    float alpha = Mathf.Lerp(1f, 0f, fadeT);
                    text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }

                yield return null;
            }

            Destroy(obj);
        }
    }
}
