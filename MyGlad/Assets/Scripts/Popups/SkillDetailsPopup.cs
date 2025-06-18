using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SkillDetailsPopup : MonoBehaviour
{
    public static SkillDetailsPopup Instance;

    [SerializeField] private GameObject popupOverlay;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [SerializeField] private GameObject detailPrefab;
    [SerializeField] private Transform col1Row1;
    [SerializeField] private Transform col1Row2;
    [SerializeField] private Transform col2Row1;
    [SerializeField] private Transform col2Row2;

    [Header("Skill Level Images")]
    public GameObject clawmark1;
    public GameObject clawmark2;
    public GameObject clawmark3;

    private void Awake()
    {
        Instance = this;
        popupOverlay.SetActive(false);
    }

    public void Show(Skill skill, int level)
    {
        popupOverlay.SetActive(true);
        nameText.text = skill.skillName;
        descriptionText.text = skill.description;
        iconImage.sprite = skill.skillIcon;
        clawmark1.SetActive(level >= 1);
        clawmark2.SetActive(level >= 2);
        clawmark3.SetActive(level >= 3);

        ClearPreviousDetails();

        int count = 0;

        void AddStat(string label, int value)
        {
            if (value == 0) return;

            Transform targetRow1 = count < 6 ? col1Row1 : col2Row1;
            Transform targetRow2 = count < 6 ? col1Row2 : col2Row2;

            GameObject labelObj = Instantiate(detailPrefab, targetRow1);
            labelObj.GetComponent<TMP_Text>().text = label;

            GameObject valueObj = Instantiate(detailPrefab, targetRow2);
            valueObj.GetComponent<TMP_Text>().text = value.ToString();

            count++;
        }

        // Samma stats som i ItemDetailsPopup
        AddStat("Strength", skill.strength);
        AddStat("Agility", skill.agility);
        AddStat("Intellect", skill.intellect);
        AddStat("Health", skill.health);
        AddStat("Precision", skill.hit);
        AddStat("Defense", skill.defense);
        AddStat("Stun", skill.stunRate);
        AddStat("Lifesteal", skill.lifesteal);
        AddStat("Initiative", skill.initiative);
        AddStat("Combo", skill.combo);
    }

    private void ClearPreviousDetails()
    {
        foreach (Transform child in col1Row1) Destroy(child.gameObject);
        foreach (Transform child in col1Row2) Destroy(child.gameObject);
        foreach (Transform child in col2Row1) Destroy(child.gameObject);
        foreach (Transform child in col2Row2) Destroy(child.gameObject);
    }

    public void Hide()
    {
        popupOverlay.SetActive(false);
    }
}
