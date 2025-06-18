using UnityEngine;
using UnityEngine.EventSystems;

public class SwapItemUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject highlight;
    private int itemIndex;
    private RewardSystem rewardSystem;

    public void Setup(int index, RewardSystem system)
    {
        itemIndex = index;
        rewardSystem = system;
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlight != null)
        {
            highlight.SetActive(active);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (rewardSystem != null)
        {
            rewardSystem.SelectItemToReplace(itemIndex);
            rewardSystem.HighlightSelectedSlot(this);
        }
        else
        {
            Debug.LogError("❌ rewardSystem is not set in SwapItemUI");
        }
    }
}