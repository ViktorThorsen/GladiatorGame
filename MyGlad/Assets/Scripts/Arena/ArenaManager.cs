using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private GameObject loadingScreen;

    [Header("Challenge UI")]
    [SerializeField] private GameObject challengePanel;
    [SerializeField] private GameObject pendingChallengePrefab;
    [SerializeField] private GameObject receivedChallengePrefab;
    [SerializeField] private Transform pendingContainer;
    [SerializeField] private Transform receivedContainer;

    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private PetDataBase petDataBase;
    [SerializeField] private SkillDataBase skillDataBase;

    [SerializeField] private GameObject noPendingText;
    [SerializeField] private GameObject noReceivedText;




    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        coinsText.text = CharacterData.Instance.coins.ToString();
    }


    public void Cleanup()
    {
        if (Instance == this)
        {
            Instance = null;
            Destroy(gameObject);
        }
    }
}