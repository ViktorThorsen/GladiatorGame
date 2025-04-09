using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class SkillBattleHandler : MonoBehaviour
{
    GameObject thisUnit;


    public bool berserkUsed;

    void Start()
    {
        thisUnit = gameObject;
        berserkUsed = false;
    }
    public void LifeBlood(GameObject character)
    {
        HealthManager healthManager = character.GetComponent<HealthManager>();
        int maxHealth = CharacterData.Instance.Health;
        double reg = maxHealth * 0.01;
        int flooredReg = (int)Math.Floor(reg);
        healthManager.IncreaseHealth(flooredReg);
    }

    public int Berserk(GameObject character)
    {
        berserkUsed = true;
        int attackDamage = CharacterData.Instance.AttackDamage;

        // Get all SpriteRenderer components in the character and its children
        SpriteRenderer[] spriteRenderers = character.GetComponentsInChildren<SpriteRenderer>();

        // Loop through each SpriteRenderer and change color, excluding certain objects
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            // Exclude specific objects like "Shadow" and "MainHandSocket"
            if (spriteRenderer.gameObject.name != "Shadow" && spriteRenderer.gameObject.name != "MainHandSocket")
            {
                spriteRenderer.color = Color.red;  // Set color to red (or any other color)
            }
        }
        return attackDamage;
    }
    public void EndBerserk(GameObject character)
    {
        SpriteRenderer[] spriteRenderers = character.GetComponentsInChildren<SpriteRenderer>();

        // Loop through each SpriteRenderer and reset color, excluding certain objects
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            // Exclude specific objects like "Shadow" and "MainHandSocket"
            if (spriteRenderer.gameObject.name != "Shadow" && spriteRenderer.gameObject.name != "MainHandSocket")
            {
                spriteRenderer.color = Color.white;  // Reset color to white (default)
            }
        }
    }

    public int VenomousTouch()
    {
        int attackDamage = CharacterData.Instance.AttackDamage;
        double venomDmg = attackDamage * 0.01;
        int venom = (int)Math.Floor(venomDmg);
        if (venom < 1)
        {
            venom = 1;
        }
        return venom;
    }

    public void Disarm(GameObject disarmer, GameObject disarmed)
    {

    }

    public void WildRoar()
    {

    }

    public bool CounterStrike(GameObject enemy, System.Action onCounterAttackFinished)
    {
        HealthManager characterToCounterHealthManager = enemy.GetComponent<HealthManager>();
        Animator anim = GetComponent<Animator>();

        // Trigger the hit animation
        anim.SetTrigger("hit");

        // Move player and enemy
        PlayerMovement movement = thisUnit.GetComponent<PlayerMovement>();
        movement.MovePlayerToRight(0.5f);
        EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
        enemyMovement.MoveEnemyToRight(0.5f);

        // Deal damage to the enemy
        characterToCounterHealthManager.ReduceHealth(movement.CalculateRandomDamage(CharacterData.Instance.AttackDamage), "Normal", thisUnit);

        // Start the coroutine and pass the callback for when the attack is finished
        StartCoroutine(StopHitWithDelay(anim, onCounterAttackFinished));

        return true;
    }
    public bool ArenaCounterStrike(GameObject enemy, System.Action onCounterAttackFinished)
    {
        ArenaHealthManager characterToCounterHealthManager = enemy.GetComponent<ArenaHealthManager>();
        Animator anim = GetComponent<Animator>();

        // Trigger the hit animation
        anim.SetTrigger("hit");


        if (thisUnit.tag == "Player")
        {
            ArenaPlayerMovement movement = thisUnit.GetComponent<ArenaPlayerMovement>();
            movement.MovePlayerToRight(0.5f);
        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            ArenaEnemyPlayerMovement movement = thisUnit.GetComponent<ArenaEnemyPlayerMovement>();
            movement.MovePlayerToLeft(0.5f);
        }

        if (thisUnit.tag == "Player")
        {
            if (enemy.tag == "EnemyGlad")
            {
                ArenaEnemyPlayerMovement enemyMovement = enemy.GetComponent<ArenaEnemyPlayerMovement>();
                enemyMovement.MovePlayerToRight(0.5f);
            }
            else if (enemy.tag == "EnemyPet1" && enemy.tag == "EnemyPet2" && enemy.tag == "EnemyPet3")
            {
                ArenaEnemyPetMovement enemyPetMovement = enemy.GetComponent<ArenaEnemyPetMovement>();
                enemyPetMovement.MovePetToRight(0.5f);
            }

        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            if (enemy.tag == "Player")
            {
                ArenaPlayerMovement enemyMovement = enemy.GetComponent<ArenaPlayerMovement>();
                enemyMovement.MovePlayerToLeft(0.5f);
            }
            else if (enemy.tag == "Pet1" && enemy.tag == "Pet2" && enemy.tag == "Pet3")
            {
                ArenaPetMovement enemyPetMovement = enemy.GetComponent<ArenaPetMovement>();
                enemyPetMovement.MovePetToLeft(0.5f);
            }
        }
        if (thisUnit.tag == "Player")
        {
            ArenaPlayerMovement movement = thisUnit.GetComponent<ArenaPlayerMovement>();
            characterToCounterHealthManager.ReduceHealth(movement.CalculateRandomDamage(CharacterData.Instance.AttackDamage), "Normal", thisUnit);
        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            ArenaEnemyPlayerMovement movement = thisUnit.GetComponent<ArenaEnemyPlayerMovement>();
            characterToCounterHealthManager.ReduceHealth(movement.CalculateRandomDamage(CharacterData.Instance.AttackDamage), "Normal", thisUnit);
        }

        // Start the coroutine and pass the callback for when the attack is finished
        StartCoroutine(StopHitWithDelay(anim, onCounterAttackFinished));

        return true;
    }

    private IEnumerator StopHitWithDelay(Animator anim, System.Action onCounterAttackFinished)
    {
        // Wait for a short time (e.g., 0.5 seconds)
        yield return new WaitForSeconds(0.5f);

        // Trigger the stophit animation after the delay
        anim.SetTrigger("stophit");

        // Call the callback to move back after the counterattack is done
        onCounterAttackFinished?.Invoke();
    }
}

