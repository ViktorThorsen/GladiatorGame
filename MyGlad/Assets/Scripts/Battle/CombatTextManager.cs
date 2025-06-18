using UnityEngine;
using TMPro;

public class CombatTextManager : MonoBehaviour
{
    public static CombatTextManager Instance;
    [SerializeField] private GameObject combatTextPrefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnText(string message, Vector3 worldPos, string hexColor, float heightOffset = 3f)
    {
        Vector3 spawnPosition = worldPos + new Vector3(0f, heightOffset, 0f);
        spawnPosition.z = 5f; // Viktigt! Så den syns framför allt

        GameObject go = Instantiate(combatTextPrefab, spawnPosition, Quaternion.identity);

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = message;

        if (ColorUtility.TryParseHtmlString(hexColor, out Color parsedColor))
            tmp.color = parsedColor;
        else
            tmp.color = Color.white;
    }
}
