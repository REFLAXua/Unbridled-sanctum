using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace UnbridledSanctum.UI
{
    public class TooltipPopup : MonoBehaviour
    {
        public static TooltipPopup Instance { get; private set; }
        private static readonly List<TooltipPopup> allPopups = new List<TooltipPopup>();

        public GameObject panel;
        public TextMeshProUGUI text;
        public RectTransform rectTransform;

        private void Awake()
        {
            Instance = this;
            if (!allPopups.Contains(this)) allPopups.Add(this);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            allPopups.Remove(this);
            if (Instance == this) Instance = null;
        }

        public static TooltipPopup GetActiveInstance()
        {
            for (int i = allPopups.Count - 1; i >= 0; i--)
            {
                var p = allPopups[i];
                if (p != null && p.transform.parent != null && p.transform.parent.gameObject.activeInHierarchy)
                {
                    return p;
                }
            }
            return Instance;
        }

        public static void HideAll()
        {
            for (int i = 0; i < allPopups.Count; i++)
            {
                if (allPopups[i] != null) allPopups[i].Hide();
            }
        }

        public Vector2 fixedAnchoredPosition;
        public bool hasFixedPosition = false;

        public void SetFixedPosition(Vector2 anchoredPos)
        {
            fixedAnchoredPosition = anchoredPos;
            hasFixedPosition = true;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = anchoredPos;
            }
        }

        public void Show(string msg, Vector3 worldPos = default)
        {
            if (panel == null || text == null) return;
            text.text = msg;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();

            if (rectTransform != null)
            {
                if (hasFixedPosition)
                {
                    rectTransform.anchorMin = new Vector2(0f, 1f);
                    rectTransform.anchorMax = new Vector2(0f, 1f);
                    rectTransform.pivot = new Vector2(0f, 1f);
                    rectTransform.anchoredPosition = fixedAnchoredPosition;
                }
                else
                {
                    rectTransform.position = worldPos + new Vector3(150f, -10f, 0f);
                }
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
