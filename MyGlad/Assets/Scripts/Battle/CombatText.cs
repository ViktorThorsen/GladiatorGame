using UnityEngine;
using TMPro;

public class CombatText : MonoBehaviour
{
    public float fallSpeed = 0.5f;
    public float duration = 1.2f;
    public float fadeStartTime = 0.6f;

    private float timer = 0f;
    private TextMeshPro tmp;
    private Color startColor;

    void Start()
    {
        tmp = GetComponent<TextMeshPro>();
        startColor = tmp.color;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Fall neråt lite
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Fade efter en liten stund
        if (timer > fadeStartTime)
        {
            float fadeAmount = 1 - ((timer - fadeStartTime) / (duration - fadeStartTime));
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, fadeAmount);
        }

        // Försvinn efter duration
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
