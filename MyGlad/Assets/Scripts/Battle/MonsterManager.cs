using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static GameObject InstantiateMonster(GameObject monsterPrefab, Transform parentCanvas, Transform monsterPos, Vector3 scale)
    {
        // Instantiate a new character based on the prefab
        GameObject monsterObject = Instantiate(monsterPrefab);
        MonsterStats monsterData = monsterPrefab.GetComponent<MonsterStats>();

        // Re-parent the characterObject to the Canvas
        if (parentCanvas != null)
        {
            monsterObject.transform.SetParent(parentCanvas.transform, false); // 'false' keeps local position, rotation, and scale
        }
        else
        {
            return null;
        }

        // Set the position of the character to the charPos position
        if (monsterPos != null)
        {
            monsterObject.transform.position = monsterPos.position;
            monsterObject.transform.rotation = monsterPos.rotation; // Match rotation if necessary
        }
        else
        {
            return null;
        }
        // Set the character's scale
        monsterObject.transform.localScale = scale;

        // **Set the correct sorting layer for internal canvases**
        Canvas[] internalCanvases = monsterObject.GetComponentsInChildren<Canvas>();
        foreach (Canvas canvas in internalCanvases)
        {
            canvas.sortingLayerName = "firstFront";
            canvas.sortingOrder = 0; // Or adjust the order as needed
            canvas.overrideSorting = true; // Ensure the canvas uses the specified sorting layer
        }
        TrailRenderer[] internalTrail = monsterObject.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trail in internalTrail)
        {
            trail.sortingLayerName = "firstFront"; // Set sorting layer at the top
            trail.sortingOrder = 0; // Set sorting order

            Renderer trailRenderer = trail.GetComponent<Renderer>();
            if (trailRenderer != null)
            {
                trailRenderer.sortingLayerName = "firstFront"; // Set sorting layer at the bottom part
                trailRenderer.sortingOrder = 0; // Set sorting order at the bottom part
            }
        }
        return monsterObject;
    }
}
