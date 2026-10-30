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

            if (selectedBorder != null)
            {
                selectedBorder.SetActive(isSelected || isFocused);
            }
        }

        private void Update()
        {
            if (visualRoot == null) return;

            if (isSelected)
            {
                targetY = 28f;
                targetScale = 1.08f;
            }
            else if (isFocused)
            {
                targetY = 20f;
                targetScale = 1.05f;
            }
            else
            {
                targetY = 0f;
                targetScale = 1.0f;
            }

            Vector2 curPos = visualRoot.anchoredPosition;
            curPos.y = Mathf.Lerp(curPos.y, targetY, Time.deltaTime * 16f);
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
                bool showOverlay = card.currentCooldown > 0 || !isPlayerTurn || currentAp < card.data.apCost || isActionInProgress;
                cdOverlay.SetActive(showOverlay);

                if (cdText != null)
                {
                    if (card.currentCooldown > 0)
                        cdText.text = $"CD: {card.currentCooldown}t";
                    else if (currentAp < card.data.apCost)
                        cdText.text = "Need AP";
                    else if (!isPlayerTurn)
                        cdText.text = "Enemy Turn";
                    else if (isActionInProgress)
                        cdText.text = "...";
                    else
                        cdText.text = "";
                }
            }
        }
    }
}
