using System.Collections.Generic;
using Completed;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    // Method to instantiate and set up the character
    public static GameObject InstantiateCharacter(CharacterData characterData, GameObject characterPrefab, Transform parentObj, Transform charPos, Vector3 scale)
    {
        // Instantiate a new character based on the prefab
        GameObject characterObject = Instantiate(characterPrefab);

        // Re-parent the characterObject to the Canvas
        if (parentObj != null)
        {
            characterObject.transform.SetParent(parentObj.transform, false); // 'false' keeps local position, rotation, and scale
        }
        else
        {

            return null;
        }

        // Set the position of the character to the charPos position
        if (charPos != null)
        {
            characterObject.transform.position = charPos.position;
            characterObject.transform.rotation = charPos.rotation; // Match rotation if necessary
        }
        else
        {

            return null;
        }

        // Set the character's scale
        characterObject.transform.localScale = scale;

        // **Set the correct sorting layer for internal canvases**
        Canvas[] internalCanvases = characterObject.GetComponentsInChildren<Canvas>();
        foreach (Canvas canvas in internalCanvases)
        {
            canvas.sortingLayerName = "firstFront"; // Set sorting layer to frontPos
            canvas.sortingOrder = 0; // Or adjust the order as needed
            canvas.overrideSorting = true; // Ensure the canvas uses the specified sorting layer
        }
        TrailRenderer[] internalTrail = characterObject.GetComponentsInChildren<TrailRenderer>();
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

        // Get the SwitchPart component from the instantiated object
        SwitchPart switchPart = characterObject.GetComponent<SwitchPart>();

        if (switchPart != null)
        {
            // Apply the saved body part labels to the SwitchPart component
            for (int i = 0; i < characterData.BodyPartLabels.Length; i++)
            {
                if (i < switchPart.bodyParts.Length)
                {
                    switchPart.bodyParts[i].SwitchParts(new string[] { characterData.BodyPartLabels[i] });
                }
            }
        }
        else
        {

        }

        // Return the instantiated character object
        return characterObject;
    }
    public static GameObject InstantiateEnemyGladiator(EnemyGladiatorData characterData, GameObject characterPrefab, Transform parentObj, Transform charPos, Vector3 scale)
    {
        // Instantiate a new character based on the prefab
        GameObject characterObject = Instantiate(characterPrefab);

        // Re-parent the characterObject to the Canvas
        if (parentObj != null)
        {
            characterObject.transform.SetParent(parentObj.transform, false); // 'false' keeps local position, rotation, and scale
        }
        else
        {

            return null;
        }

        // Set the position of the character to the charPos position
        if (charPos != null)
        {
            characterObject.transform.position = charPos.position;
            characterObject.transform.rotation = charPos.rotation; // Match rotation if necessary
        }
        else
        {

            return null;
        }

        // Set the character's scale
        characterObject.transform.localScale = scale;

        // **Set the correct sorting layer for internal canvases**
        Canvas[] internalCanvases = characterObject.GetComponentsInChildren<Canvas>();
        foreach (Canvas canvas in internalCanvases)
        {
            canvas.sortingLayerName = "firstFront"; // Set sorting layer to frontPos
            canvas.sortingOrder = 0; // Or adjust the order as needed
            canvas.overrideSorting = true; // Ensure the canvas uses the specified sorting layer
        }
        TrailRenderer[] internalTrail = characterObject.GetComponentsInChildren<TrailRenderer>();
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

        // Get the SwitchPart component from the instantiated object
        SwitchPart switchPart = characterObject.GetComponent<SwitchPart>();

        if (switchPart != null)
        {
            // Apply the saved body part labels to the SwitchPart component
            for (int i = 0; i < characterData.BodyPartLabels.Length; i++)
            {
                if (i < switchPart.bodyParts.Length)
                {
                    switchPart.bodyParts[i].SwitchParts(new string[] { characterData.BodyPartLabels[i] });
                }
            }
        }
        else
        {

        }

        // Return the instantiated character object
        return characterObject;
    }
    public static GameObject InstantiateReplayCharacter(
    CharacterWrapper dto,
    GameObject characterPrefab,
    Transform parentObj,
    Transform charPos,
    Vector3 scale)
    {
        GameObject characterObject = Instantiate(characterPrefab);

        // Parent
        if (parentObj != null)
            characterObject.transform.SetParent(parentObj.transform, false);
        else
            return null;

        // Position & rotation
        if (charPos != null)
        {
            characterObject.transform.position = charPos.position;
            characterObject.transform.rotation = charPos.rotation;
        }
        else
            return null;

        // Scale
        characterObject.transform.localScale = scale;

        // === SORTERING: Canvases ===
        Canvas[] internalCanvases = characterObject.GetComponentsInChildren<Canvas>();
        foreach (Canvas canvas in internalCanvases)
        {
            canvas.sortingLayerName = "firstFront";
            canvas.sortingOrder = 0;
            canvas.overrideSorting = true;
        }

        // === SORTERING: Trails ===
        TrailRenderer[] internalTrail = characterObject.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trail in internalTrail)
        {
            trail.sortingLayerName = "firstFront";
            trail.sortingOrder = 0;
            var trailRenderer = trail.GetComponent<Renderer>();
            if (trailRenderer != null)
            {
                trailRenderer.sortingLayerName = "firstFront";
                trailRenderer.sortingOrder = 0;
            }
        }

        // === BODY PARTS ===
        SwitchPart switchPart = characterObject.GetComponent<SwitchPart>();
        if (switchPart != null && dto.bodyPartLabels != null)
        {
            string[] parts = new string[]
            {
            dto.bodyPartLabels.hair,
            dto.bodyPartLabels.eyes,
            dto.bodyPartLabels.chest,
            dto.bodyPartLabels.legs
            };

            for (int i = 0; i < parts.Length && i < switchPart.bodyParts.Length; i++)
            {
                switchPart.bodyParts[i].SwitchParts(new string[] { parts[i] });
            }
        }

        return characterObject;
    }

}