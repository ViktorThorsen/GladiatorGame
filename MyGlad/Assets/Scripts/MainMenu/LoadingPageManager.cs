using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingPageManager : MonoBehaviour
{
    public RectTransform spinner; // 🔥 Ikonen eller bilden som ska snurra
    public float rotationSpeed = 100f; // Hastighet på snurr

    private void Update()
    {
        if (spinner != null)
        {
            spinner.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }
}

