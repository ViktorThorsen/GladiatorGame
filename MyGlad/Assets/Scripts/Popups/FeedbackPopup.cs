using UnityEngine;
using TMPro;
using System.Collections;


public class FeedbackPopup : MonoBehaviour
{
    [SerializeField] private GameObject feedbackPopup;
    [SerializeField] private TMP_Text feedbackTitleText;
    [SerializeField] private TMP_Text feedbackBodyText;
    public static FeedbackPopup Instance;

    public void ShowFeedback(string title, string message)
    {
        feedbackTitleText.text = title;
        feedbackBodyText.text = message;
        feedbackPopup.SetActive(true);
    }

    public void HideFeedback()
    {
        feedbackPopup.SetActive(false);
    }


}