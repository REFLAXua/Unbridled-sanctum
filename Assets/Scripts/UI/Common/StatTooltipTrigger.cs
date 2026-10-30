using UnityEngine;
using UnityEngine.EventSystems;

namespace UnbridledSanctum.UI
{
    public class StatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string tooltipText;
        public TooltipPopup targetPopup;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(tooltipText)) return;

            TooltipPopup popup = targetPopup;
            if (popup == null || (!popup.gameObject.activeInHierarchy && (popup.transform.parent == null || !popup.transform.parent.gameObject.activeInHierarchy)))
            {
                popup = TooltipPopup.GetActiveInstance();
            }

            if (popup != null)
            {
                popup.Show(tooltipText, transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (targetPopup != null)
            {
                targetPopup.Hide();
            }
            TooltipPopup.HideAll();
        }
    }
}
