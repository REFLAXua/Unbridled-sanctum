using UnityEngine;
using UnityEngine.EventSystems;

namespace UnbridledSanctum.UI
{
    public class EndTurnButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.OnEndTurnPointerEnter();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.OnEndTurnPointerExit();
            }
        }
    }
}
