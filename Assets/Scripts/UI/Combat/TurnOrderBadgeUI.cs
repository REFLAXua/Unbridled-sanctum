using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    public class TurnOrderBadgeUI : MonoBehaviour
    {
        public Combatant combatant;
        public Image background;
        public GameObject restingBorder;
        public GameObject border;
        public Image iconImage;
        public TextMeshProUGUI label;
        public TextMeshProUGUI orderNumberText;
        public CanvasGroup canvasGroup;

        public void SetActiveTurn()
        {
            if (border != null) border.SetActive(true);
            if (restingBorder != null) restingBorder.SetActive(false);
            if (background != null) background.color = new Color(0.24f, 0.18f, 0.08f, 0.98f);
            if (iconImage != null) iconImage.color = Color.white;
            if (label != null) label.color = Color.white;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            transform.localScale = new Vector3(1.12f, 1.12f, 1f);
        }

        public void SetWaiting()
        {
            if (border != null) border.SetActive(false);
            if (restingBorder != null) restingBorder.SetActive(true);
            if (background != null) background.color = new Color(0.07f, 0.09f, 0.13f, 0.96f);
            if (iconImage != null) iconImage.color = new Color(0.92f, 0.94f, 0.98f, 0.95f);
            if (label != null) label.color = new Color(0.85f, 0.88f, 0.95f, 0.90f);
            if (canvasGroup != null) canvasGroup.alpha = 0.95f;
            transform.localScale = Vector3.one;
        }

        public void SetEnded()
        {
            if (border != null) border.SetActive(false);
            if (restingBorder != null) restingBorder.SetActive(true);
            if (background != null) background.color = new Color(0.04f, 0.05f, 0.07f, 0.60f);
            if (iconImage != null) iconImage.color = new Color(0.45f, 0.50f, 0.55f, 0.45f);
            if (label != null) label.color = new Color(0.45f, 0.50f, 0.55f, 0.45f);
            if (canvasGroup != null) canvasGroup.alpha = 0.50f;
            transform.localScale = Vector3.one;
        }

        public void SetDefeated()
        {
            if (border != null) border.SetActive(false);
            if (restingBorder != null) restingBorder.SetActive(false);
            if (background != null) background.color = new Color(0.03f, 0.03f, 0.04f, 0.30f);
            if (iconImage != null) iconImage.color = new Color(0.25f, 0.25f, 0.25f, 0.25f);
            if (label != null)
            {
                label.text = $"<s>{label.text}</s>";
                label.color = new Color(0.4f, 0.4f, 0.4f, 0.35f);
            }
            if (canvasGroup != null) canvasGroup.alpha = 0.30f;
            transform.localScale = Vector3.one;
        }
    }
}
