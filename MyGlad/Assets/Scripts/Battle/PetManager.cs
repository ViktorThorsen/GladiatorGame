using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetManager : MonoBehaviour
{
    public static GameObject InstantiatePet(GameObject petPrefab, Transform parentObj, Transform charPos, Vector3 scale)
    {
        // Instantiate a new pet based on the prefab
        GameObject petObject = Instantiate(petPrefab);

        // Re-parent the petObject to the Canvas
        if (parentObj != null)
        {
            petObject.transform.SetParent(parentObj.transform, false); // 'false' keeps local position, rotation, and scale
        }
        else
        {
            return null;
        }

        // Set the position of the pet to the charPos position
        if (charPos != null)
        {
            petObject.transform.position = charPos.position;
            petObject.transform.rotation = charPos.rotation; // Match rotation if necessary
        }
        else
        {
            return null;
        }

        petObject.transform.localScale = scale;

        // **Set the correct sorting layer for internal canvases**
        Canvas[] internalCanvases = petObject.GetComponentsInChildren<Canvas>();
        foreach (Canvas canvas in internalCanvases)
        {
            canvas.sortingLayerName = "frontPos"; // Set sorting layer to frontPos
            canvas.sortingOrder = 0; // Or adjust the order as needed
            canvas.overrideSorting = true; // Ensure the canvas uses the specified sorting layer
        }
        return petObject;
    }
}
