using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    public class CombatCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button btn;
        public Image cardBg;
        public Outline cardOutline;
        public GameObject cdOverlay;
        public TextMeshProUGUI cdText;
        public GameObject selectedBorder;
        public RectTransform visualRoot;
        public Canvas cardCanvas;
        public int cardIndex;

        public bool isSelected = false;
        public bool isFocused = false;

        private float targetY = 0f;
        private float targetScale = 1f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.OnCardPointerEnter(cardIndex);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.OnCardPointerExit(cardIndex);
            }
        }

        public void SetVisualState(bool selected, bool focused)
        {
            isSelected = selected;
            isFocused = focused;

            if (cardCanvas != null)
            {
                bool topOrder = isSelected || isFocused;
                cardCanvas.overrideSorting = topOrder;
                cardCanvas.sortingOrder = topOrder ? 25 : 5;
            }

            // Wardrobe-style illumination highlight (subtle warm brighten + noble gold outline)
            if (cardBg != null)
            {
                if (isSelected)
                    cardBg.color = new Color(1.28f, 1.20f, 1.05f, 1f); // Noble warm illumination when selected
                else if (isFocused)
                    cardBg.color = new Color(1.18f, 1.15f, 1.08f, 1f); // Soft warm illumination on hover (wardrobe slot match)
                else
                    cardBg.color = Color.white;
            }

            if (cardOutline != null)
            {
                if (isSelected)
                {
                    cardOutline.enabled = true;
                    cardOutline.effectColor = new Color(1.0f, 0.85f, 0.40f, 0.95f); // Radiant gold outline
                    cardOutline.effectDistance = new Vector2(2f, -2f);
                }
                else if (isFocused)
                {
                    cardOutline.enabled = true;
                    cardOutline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.75f); // Wardrobe warm gold outline
                    cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
                }
                else
                {
                    cardOutline.enabled = false;
                }
            }

            if (selectedBorder != null)
            {
                selectedBorder.SetActive(false); // Legacy filled quad disabled permanently
            }
        }

        private void Update()
        {
            if (visualRoot == null) return;

            float targetX;
            if (isSelected)
            {
                targetX = -26f;
                targetScale = 1.05f;
            }
            else if (isFocused)
            {
                targetX = -15f;
                targetScale = 1.02f;
            }
            else
            {
                targetX = 0f;
                targetScale = 1.0f;
            }

            Vector2 curPos = visualRoot.anchoredPosition;
            curPos.x = Mathf.Lerp(curPos.x, targetX, Time.deltaTime * 16f);
            curPos.y = 0f;
            visualRoot.anchoredPosition = curPos;

            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, Vector3.one * targetScale, Time.deltaTime * 16f);
        }

        public void Refresh(CardInstance card, int currentAp, bool isPlayerTurn, bool isActionInProgress, CombatStance stance = CombatStance.Neutral, bool selected = false, bool focused = false)
        {
            if (card == null) return;
            SetVisualState(selected, focused);

            bool canPlay = isPlayerTurn && !isActionInProgress && card.CanPlay(currentAp, stance);
            if (btn != null) btn.interactable = canPlay;

            if (cdOverlay != null)
            {
                bool showOverlay = card.currentCooldown > 0 || !isPlayerTurn || isActionInProgress;
                cdOverlay.SetActive(showOverlay);

                if (cdText != null)
                {
                    if (card.currentCooldown > 0)
                        cdText.text = $"CD: {card.currentCooldown}t";
                    else if (!isPlayerTurn)
                        cdText.text = "Wait";
                    else if (isActionInProgress)
                        cdText.text = "...";
                    else
                        cdText.text = "";
                }
            }
        }
    }
}
