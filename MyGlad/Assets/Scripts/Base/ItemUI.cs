using UnityEngine;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerClickHandler
{
    public Item Item;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Visa popup med info om itemet
        ItemDetailsPopup.Instance.Show(Item);
    }
}
