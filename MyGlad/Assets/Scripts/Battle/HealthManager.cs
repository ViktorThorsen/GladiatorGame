using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;
using Cinemachine;
using System.Linq;

public class HealthManager : MonoBehaviour
{
    private Animator anim;
    public int maxHealth;
    private int currentHealth;
    private ParticleSystem hitParticles;
    private Transform hitParticleTransform;
    public Slider healthBar;
    private Transform healthSliderParent;
    private GameObject thisUnit;
    private GameManager gameManager;
    private Transform visualTransform;

    private GameObject venomDealer;


    public GameObject damageTextPrefab; // Reference to the txtCombat prefab
    [SerializeField] private CameraShakeManager cameraShakeManager;

    public bool IsPosioned;
    int startVenomRounds;

    public bool standAgain = false;
    public bool standAgainUsed = false;
    public int standAgainRound;

    private bool isDead;

    public bool IsDead
    {
        get { return isDead; }
        set { isDead = value; }
    }
    public int CurrentHealth
    {
        get { return currentHealth; }
        set { currentHealth = value; }
    }


    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        thisUnit = gameObject;
        visualTransform = transform.Find("Visual");
        if (visualTransform == null)
        {
            visualTransform = transform; // fallback om "Visual" inte finns
        }

        // Hämta Animator från rätt plats
        anim = visualTransform.GetComponent<Animator>();

        GameObject canvas = GameObject.Find("Canvas");

        if (canvas != null)
        {
            if (thisUnit.tag == "Player")
            {
                // Hitta föräldern (panelen) där health slidern ska placeras
                Transform healthSliderParent = canvas.transform.Find("PlayerHealthSliderPanel");
                // Instansiera health slider-prefab till föräldern
                GameObject healthBarObject = Instantiate(gameManager.healthSliderPrefab, healthSliderParent);

                // Hämta slider-komponenten från den instansierade prefab
                healthBar = healthBarObject.GetComponent<Slider>();

                // Ställ in max och nuvarande hälsa på slidern
                maxHealth = CharacterData.Instance.Health;
                currentHealth = CharacterData.Instance.Health;
                healthBar.maxValue = maxHealth;  // Sätt sliderns max-värde
                healthBar.value = currentHealth; // Sätt sliderns nuvarande värde
                standAgain = Inventory.Instance.HasSkill("StandAgain");
            }
            else if (thisUnit.tag == "Enemy1" || thisUnit.tag == "Enemy2" || thisUnit.tag == "Enemy3" || thisUnit.tag == "Enemy4")
            {
                // Hitta föräldern (panelen) där monster health slidern ska placeras
                Transform healthSliderParent = canvas.transform.Find("EnemyHealthSliderPanel");
                // Instansiera health slider-prefab till föräldern
                GameObject healthBarObject = Instantiate(gameManager.healthSliderPrefab, healthSliderParent);

                // Hämta slider-komponenten från den instansierade prefab
                healthBar = healthBarObject.GetComponent<Slider>();

                // Hämta monsterstatistik och sätt värden på hälsobaren
                MonsterStats monsterStats = thisUnit.GetComponent<MonsterStats>();
                maxHealth = monsterStats.Health;
                currentHealth = monsterStats.Health;

                // Ställ in max och nuvarande hälsa på slidern
                healthBar.maxValue = maxHealth;  // Sätt sliderns max-värde
                healthBar.value = currentHealth; // Sätt sliderns nuvarande värde
            }
            else if (thisUnit.tag == "Pet1" || thisUnit.tag == "Pet2" || thisUnit.tag == "Pet3")
            {
                // Hitta föräldern (panelen) där husdjurets health slider ska placeras
                Transform healthSliderParent = canvas.transform.Find("PetHealthSliderPanel");
                // Instansiera health slider-prefab till föräldern
                GameObject healthBarObject = Instantiate(gameManager.pethealthSliderPrefab, healthSliderParent);

                // Hämta slider-komponenten från den instansierade prefab
                healthBar = healthBarObject.GetComponent<Slider>();

                // Hämta husdjurets statistik och sätt värden på hälsobaren
                MonsterStats petStats = thisUnit.GetComponent<MonsterStats>();
                maxHealth = petStats.Health;
                currentHealth = petStats.Health;

                // Ställ in max och nuvarande hälsa på slidern
                healthBar.maxValue = maxHealth;  // Sätt sliderns max-värde
                healthBar.value = currentHealth; // Sätt sliderns nuvarande värde

                Sprite petIcon = petStats.icon; // du måste ha detta fält i MonsterStats eller motsvarande

                // Hitta Image-komponenten i prefabens hierarki
                Image unitIcon = healthBarObject.transform.Find("unitIcon")?.GetComponent<Image>();
                if (unitIcon != null && petIcon != null)
                {
                    unitIcon.sprite = petIcon;
                }
            }

            // Gemensam logik för att ställa in text och partikeleffekter

            hitParticleTransform = thisUnit.transform.Find("HitParticles/hitParticles");
            hitParticles = hitParticleTransform.GetComponent<ParticleSystem>();
            cameraShakeManager = FindObjectOfType<CameraShakeManager>();
        }
    }

    public void ReduceHealth(int damage, string type, GameObject doneBy, bool IsCrit)
    {
        if (type == "Normal")
        {
            currentHealth -= damage;
            if (thisUnit.tag == "Player")
            {
                var thornsSkill = Inventory.Instance.GetSkills()
                    .FirstOrDefault(s => s.skillName == "Thorns");

                if (thornsSkill != null)
                {
                    Skill thornsData = thornsSkill.GetSkillData(); // Hämta ScriptableObject från databasen
                    int level = thornsSkill.level;

                    int percent = level switch
                    {
                        1 => thornsData.effectPercentIncreaseLevel1,
                        2 => thornsData.effectPercentIncreaseLevel2,
                        3 => thornsData.effectPercentIncreaseLevel3,
                        _ => 0
                    };

                    float multiplier = percent / 100f;
                    int thornsDamage = Mathf.RoundToInt(damage * multiplier);
                    if (thornsDamage < 1) thornsDamage = 1;

                    HealthManager HM = doneBy.GetComponent<HealthManager>();
                    HM.ReduceHealth(thornsDamage, "Thorns", thisUnit, false);
                }
            }
            cameraShakeManager.CameraShake();
            if (IsCrit)
            {
                CombatTextManager.Instance.SpawnText(damage.ToString() + " Critical Strike", thisUnit.transform.position + Vector3.up * 1.5f, "#FFFFFF");
            }
            else
            {
                CombatTextManager.Instance.SpawnText(damage.ToString(), thisUnit.transform.position + Vector3.up * 1.5f, "#FFFFFF");
            }

            hitParticles.transform.position = thisUnit.transform.position;
            ParticleSystem.MainModule mainModule = hitParticles.main;  // Access the main module
            mainModule.startColor = Color.white;
            hitParticles.Play();
            UpdateHealthUI();
            if (currentHealth <= 0)
            {
                if (thisUnit.tag == "Player" && standAgain)
                {
                    if (!standAgainUsed)
                    {
                        currentHealth = 1;
                        standAgainRound = gameManager.RoundsCount;
                        standAgainUsed = true;
                    }
                    else if (standAgainUsed && standAgainRound + 1 <= gameManager.RoundsCount)
                    {
                        anim.SetBool("stunned", false);
                        IsPosioned = false;
                        currentHealth = 0;
                        anim.SetTrigger("death");
                        IsDead = true;
                        gameManager.Died(thisUnit.tag);
                    }
                    else
                    {
                        currentHealth = 1;
                        standAgainUsed = true;
                    }
                }
                else
                {
                    anim.SetBool("stunned", false);
                    IsPosioned = false;
                    currentHealth = 0;
                    anim.SetTrigger("death");
                    IsDead = true;
                    gameManager.Died(thisUnit.tag);
                }
            }
        }
        else if (type == "Venom")
        {
            currentHealth -= damage;
            hitParticles.transform.position = thisUnit.transform.position;
            ParticleSystem.MainModule mainModule = hitParticles.main;  // Access the main module
            mainModule.startColor = Color.green;
            hitParticles.Play();
            UpdateHealthUI();
            if (currentHealth <= 0)
            {
                if (thisUnit.tag == "Player" && standAgain)
                {
                    if (!standAgainUsed)
                    {
                        currentHealth = 1;
                        standAgainRound = gameManager.RoundsCount;
                        standAgainUsed = true;
                    }
                    else if (standAgainUsed && standAgainRound + 1 <= gameManager.RoundsCount)
                    {
                        anim.SetBool("stunned", false);
                        IsPosioned = false;
                        currentHealth = 0;
                        anim.SetTrigger("death");
                        IsDead = true;
                        gameManager.Died(thisUnit.tag);
                    }
                    else
                    {
                        currentHealth = 1;
                        standAgainUsed = true;
                    }
                }
                else
                {
                    anim.SetBool("stunned", false);
                    IsPosioned = false;
                    currentHealth = 0;
                    anim.SetTrigger("death");
                    IsDead = true;
                    gameManager.Died(thisUnit.tag);
                }
            }
        }
        else if (type == "Thorns")
        {
            if (damage < 1)
            {
                damage = 1;
            }
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                anim.SetBool("stunned", false);
                IsPosioned = false;
                currentHealth = 0;
                anim.SetTrigger("death");
                IsDead = true;
                gameManager.Died(thisUnit.tag);
            }
            UpdateHealthUI();
        }
    }

    public void IncreaseHealth(int health)
    {
        currentHealth += health;
        UpdateHealthUI();
        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void ApplyVenom(GameObject venomdealer)
    {
        startVenomRounds = gameManager.RoundsCount;
        venomDealer = venomdealer;
        IsPosioned = true;
    }
    public void RemoveVemon()
    {
        if (IsPosioned)
        {
            if (gameManager.RoundsCount > startVenomRounds + 5)
            {
                IsPosioned = false;
            }
            else
            {
                SkillBattleHandler skillBattleHandler = thisUnit.GetComponent<SkillBattleHandler>();
                ReduceHealth(skillBattleHandler.VenomousTouch(venomDealer, thisUnit), "Venom", null, false);
            }
        }
    }

    private void UpdateHealthUI()
    {
        // Update the health bar slider value
        healthBar.value = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Get the Image component directly from the health bar's fill rect
        Image healthFillImage = healthBar.fillRect.GetComponent<Image>();

        // If health is 0, set the color to fully transparent
        if (currentHealth <= 0)
        {
            healthFillImage.color = new Color(0f, 1f, 0.043f, 0f); // Fully transparent
        }
        else
        {
            // Set the fill color to the default green (00FF0B) when health is greater than 0
            healthFillImage.color = new Color(0f, 1f, 0.043f, 1f); // Opaque green
        }
    }

    // Coroutine to animate the damage text
    private IEnumerator AnimateDamageText(GameObject damageTextInstance, string text)
    {
        TMP_Text damageText = damageTextInstance.GetComponent<TMP_Text>();
        RectTransform rectTransform = damageText.rectTransform;

        Vector3 originalPosition = rectTransform.localPosition;

        // Change target position based on type
        Vector3 targetPosition = originalPosition + (text == "Heal" ? new Vector3(0, 2, 0) : new Vector3(0, -2, 0)); // Move up for heals, down for damage

        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Move the text based on the type
            rectTransform.localPosition = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / duration);

            // Fade the text out over time
            float alpha = Mathf.Lerp(1, 0, elapsedTime / duration);
            damageText.color = new Color(damageText.color.r, damageText.color.g, damageText.color.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Destroy the damage text instance after the animation completes
        Destroy(damageTextInstance);
    }
}