using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemDetailsPopup : MonoBehaviour
{
    public static ItemDetailsPopup Instance;

    [SerializeField] private GameObject popupOverlay;
    [SerializeField] private GameObject popupContent;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [SerializeField] private GameObject itemDetailPrefab;
    [SerializeField] private Transform col1Row1;
    [SerializeField] private Transform col1Row2;
    [SerializeField] private Transform col2Row1;
    [SerializeField] private Transform col2Row2;

    private void Awake()
    {
        Instance = this;
        popupOverlay.SetActive(false);
    }

    public void Show(Item item)
    {
        popupOverlay.SetActive(true);
        nameText.text = item.itemName;
        descriptionText.text = item.description;
        iconImage.sprite = item.itemIcon;

        ClearPreviousStats();

        int count = 0;

        void AddStat(string label, int value)
        {
            if (value == 0) return;

            Transform targetRow1 = count < 6 ? col1Row1 : col2Row1;
            Transform targetRow2 = count < 6 ? col1Row2 : col2Row2;

            GameObject labelObj = Instantiate(itemDetailPrefab, targetRow1);
            labelObj.GetComponent<TMP_Text>().text = label;

            GameObject valueObj = Instantiate(itemDetailPrefab, targetRow2);
            valueObj.GetComponent<TMP_Text>().text = value.ToString();

            count++;
        }

        // Lägg till stats
        AddStat("Heals", item.healthRestorationAmount);
        AddStat("Durability", item.durability);
        AddStat("Strength", item.strength);
        AddStat("Agility", item.agility);
        AddStat("Intellect", item.intellect);
        AddStat("Health", item.health);
        AddStat("Precision", item.hit);
        AddStat("Defense", item.defense);
        AddStat("Stun", item.stunRate);
        AddStat("Lifesteal", item.lifesteal);
        AddStat("Initiative", item.initiative);
        AddStat("Combo", item.combo);

    }

    private void ClearPreviousStats()
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
