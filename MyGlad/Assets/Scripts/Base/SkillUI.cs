using UnityEngine;
using UnityEngine.EventSystems;

public class SkillUI : MonoBehaviour, IPointerClickHandler
{
    public Skill Skill;
    public int Level;

    public void OnPointerClick(PointerEventData eventData)
    {
        SkillDetailsPopup.Instance.Show(Skill, Level);
    }
}
