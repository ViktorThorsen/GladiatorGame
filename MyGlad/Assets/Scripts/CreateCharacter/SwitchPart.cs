using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Completed
{
    public class SwitchPart : MonoBehaviour
    {
        [SerializeField]
        public BodyParts[] bodyParts;
        [SerializeField] string[] labels;

        // Start is called before the first frame update
        void Start()
        {

            for (int i = 0; i < bodyParts.Length; i++)
            {
                bodyParts[i].Init(labels);
            }
        }
    }

    [System.Serializable]
    public class BodyParts
    {
        [SerializeField] Button button;
        [SerializeField] UnityEngine.U2D.Animation.SpriteResolver[] spriteResolver;
        public int id;

        public UnityEngine.U2D.Animation.SpriteResolver[] SpriteResolver { get => spriteResolver; }

        public string CurrentLabel { get; private set; } // Property to store the current label

        // List of valid labels for each resolver. You'll need to fill this out in the inspector for each body part.
        [SerializeField] List<string> validLabels;

        // Method to init the button callback
        public void Init(string[] labels)
        {
            if (button != null)
                button.onClick.AddListener(delegate { SwitchParts(labels); });
        }

        // Method to switch the sprites of each resolver list.
        public void SwitchParts(string[] labels)
        {
            id++;
            id = id % labels.Length;

            CurrentLabel = labels[id]; // Store the current label

            foreach (var item in spriteResolver)
            {
                // Check if the current Sprite Resolver contains the current label
                string validLabel = GetValidLabel(item, CurrentLabel);

                if (validLabel != null)
                {

                    item.SetCategoryAndLabel(item.GetCategory(), validLabel);
                }
                else
                {
                    Debug.LogWarning($"No valid label found for {item.GetCategory()}.");
                }
            }
        }

        // Helper method to check if a label exists in the valid labels list
        private string GetValidLabel(UnityEngine.U2D.Animation.SpriteResolver resolver, string currentLabel)
        {
            // Check if the current label is in the validLabels list for this body part
            if (validLabels.Contains(currentLabel))
            {
                return currentLabel;
            }

            // If the current label isn't valid, find the next valid one
            foreach (var label in validLabels)
            {
                if (validLabels.Contains(label))
                {
                    return label; // Return the first valid label found
                }
            }

            // Return null if no valid label is found
            return null;
        }
    }
}