using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GenericConfirmPopup : MonoBehaviour
{
    public static GenericConfirmPopup Instance;

    [Header("Popup Components")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirmAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            popupPanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Show a confirmation popup with optional icon and actions.
    /// </summary>
    /// <param name="title">Popup title</param>
    /// <param name="message">Main message</param>
    /// <param name="icon">Optional icon (can be null)</param>
    /// <param name="onConfirm">Callback when Yes is pressed</param>
    public void Show(string title, string message, Sprite icon, Action onConfirm)
    {
        popupPanel.SetActive(true);
        titleText.text = title;
        messageText.text = message;

        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        onConfirmAction = onConfirm;

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() =>
        {
            popupPanel.SetActive(false);
            onConfirmAction?.Invoke();
        });

        noButton.onClick.AddListener(() =>
        {
            popupPanel.SetActive(false);
        });
    }
}

