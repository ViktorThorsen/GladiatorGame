using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class SkillBattleHandler : MonoBehaviour
{
    GameObject thisUnit;
    private int davidAgiBonus;
    private int davidHitBonus;

    void Start()
    {
        thisUnit = gameObject;
    }
    public void LifeBlood(GameObject character)
    {
        HealthManager healthManager = character.GetComponent<HealthManager>();
        int maxHealth = CharacterData.Instance.Health;

        // Hämta skill-level och data
        var skillInstance = Inventory.Instance.GetSkills().FirstOrDefault(s => s.skillName == "LifeBlood");
        if (skillInstance == null) return;

        Skill skillData = skillInstance.GetSkillData(); // från databasen
        int level = skillInstance.level;

        // Hämta rätt procent (100 = 1% osv)
        int percent = level switch
        {
            1 => skillData.effectPercentIncreaseLevel1,
            2 => skillData.effectPercentIncreaseLevel2,
            3 => skillData.effectPercentIncreaseLevel3,
            _ => 0
        };

        // Beräkna hur mycket liv som ska återställas
        int regenAmount = Mathf.FloorToInt(maxHealth * (percent / 100f));
        if (regenAmount < 1)
        {
            regenAmount = 1;
        }

        if (healthManager.CurrentHealth < maxHealth && regenAmount > 0)
        {
            healthManager.IncreaseHealth(regenAmount);
        }
    }
    public void ArenaLifeBlood(GameObject character)
    {
        ArenaHealthManager healthManager = character.GetComponent<ArenaHealthManager>();

        int maxHealth;
        SkillInstance skillInstance;

        if (character.tag == "Player")
        {
            maxHealth = CharacterData.Instance.Health;
            skillInstance = Inventory.Instance.GetSkills().FirstOrDefault(s => s.skillName == "LifeBlood");
        }
        else
        {
            maxHealth = EnemyGladiatorData.Instance.Health;
            skillInstance = EnemyInventory.Instance.GetSkills().FirstOrDefault(s => s.skillName == "LifeBlood");
        }

        if (skillInstance == null) return;

        Skill skillData = skillInstance.GetSkillData();
        int level = skillInstance.level;

        int percent = level switch
        {
            1 => skillData.effectPercentIncreaseLevel1,
            2 => skillData.effectPercentIncreaseLevel2,
            3 => skillData.effectPercentIncreaseLevel3,
            _ => 0
        };

        int regenAmount = Mathf.FloorToInt(maxHealth * (percent / 100f));
        if (regenAmount < 1)
        {
            regenAmount = 1;
        }
        if (healthManager.CurrentHealth < maxHealth && regenAmount > 0)
        {
            healthManager.IncreaseHealth(regenAmount);
        }
    }
    public void ReplayLifeBlood(GameObject character)
    {
        ReplayHealthManager healthManager = character.GetComponent<ReplayHealthManager>();

        int maxHealth;
        SkillEntrySerializable skillEntry;

        if (character.tag == "Player")
        {
            maxHealth = ReplayCharacterData.Instance.Health;
            skillEntry = ReplayManager.Instance.selectedReplay.player.skills.skills
                .FirstOrDefault(s => s.skillName == "LifeBlood");
        }
        else
        {
            maxHealth = ReplayEnemyGladData.Instance.Health;
            skillEntry = ReplayManager.Instance.selectedReplay.enemy.skills.skills
                .FirstOrDefault(s => s.skillName == "LifeBlood");
        }

        if (skillEntry == null) return;

        Skill skillData = SkillDataBase.Instance.GetSkillByName("LifeBlood");
        int level = skillEntry.level;

        int percent = level switch
        {
            1 => skillData.effectPercentIncreaseLevel1,
            2 => skillData.effectPercentIncreaseLevel2,
            3 => skillData.effectPercentIncreaseLevel3,
            _ => 0
        };

        int regenAmount = Mathf.FloorToInt(maxHealth * (percent / 100f));
        if (regenAmount < 1)
        {
            regenAmount = 1;
        }
        if (healthManager.CurrentHealth < maxHealth && regenAmount > 0)
        {
            healthManager.IncreaseHealth(regenAmount);
        }
    }

    public int Berserk(GameObject character)
    {
        int baseStrength;
        SkillInstance skillInstance;

        if (character.tag == "Player")
        {
            baseStrength = CharacterData.Instance.Strength;
            skillInstance = Inventory.Instance.GetSkillInstance("Berserk");
        }
        else
        {
            baseStrength = EnemyGladiatorData.Instance.Strength;
            skillInstance = EnemyInventory.Instance.GetSkillInstance("Berserk");
        }

        float multiplier = 1f;

        if (skillInstance != null)
        {
            var skillData = SkillDataBase.Instance.GetSkillByName("Berserk");
            if (skillData != null)
            {
                int level = skillInstance.level;

                int percentIncrease = level switch
                {
                    1 => skillData.effectPercentIncreaseLevel1,
                    2 => skillData.effectPercentIncreaseLevel2,
                    3 => skillData.effectPercentIncreaseLevel3,
                    _ => 0
                };

                multiplier += percentIncrease / 100f;
            }
        }

        int finalStrength = Mathf.RoundToInt(baseStrength * multiplier);

        // 🔴 Visuell effekt
        foreach (var renderer in character.GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.gameObject.name != "Shadow" && renderer.gameObject.name != "MainHandSocket")
            {
                renderer.color = Color.red;
            }
        }

        return finalStrength;
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

    public int VenomousTouch(GameObject damageDealer, GameObject targetenemy)
    {
        int targetMaxHealth;

        if (damageDealer.tag == "Player")
        {
            if (targetenemy.tag == "EnemyGlad")
                targetMaxHealth = EnemyGladiatorData.Instance.Health;
            if (targetenemy.tag == "Player")
            {
                targetMaxHealth = CharacterData.Instance.Health;
            }
            else
                targetMaxHealth = targetenemy.GetComponent<MonsterStats>().Health;
        }
        else
        {
            if (targetenemy.tag == "Player")
                targetMaxHealth = CharacterData.Instance.Health;
            if (targetenemy.tag == "EnemyGlad")
            {
                targetMaxHealth = EnemyGladiatorData.Instance.Health;
            }
            else
                targetMaxHealth = targetenemy.GetComponent<MonsterStats>().Health;
        }

        // Default venom percent is 100 = 1%
        int percentHundredths = 100;

        var venomSkillInstance = Inventory.Instance.GetSkills()
            .FirstOrDefault(s => s.skillName == "VenomousTouch");

        if (venomSkillInstance != null)
        {
            Skill venomSkillData = venomSkillInstance.GetSkillData();
            int level = venomSkillInstance.level;

            percentHundredths = level switch
            {
                1 => venomSkillData.effectPercentIncreaseLevel1,
                2 => venomSkillData.effectPercentIncreaseLevel2,
                3 => venomSkillData.effectPercentIncreaseLevel3,
                _ => 100
            };
        }

        float venomMultiplier = percentHundredths / 10000f; // 100 = 1%, 250 = 2.5%, etc.
        int venom = Mathf.FloorToInt(targetMaxHealth * venomMultiplier);

        return Mathf.Max(1, venom); // Ensure at least 1 damage
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
        characterToCounterHealthManager.ReduceHealth(movement.CalculateRandomDamage(CharacterData.Instance.Strength, enemy), "Normal", thisUnit, false);

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
            characterToCounterHealthManager.ReduceHealth(movement.CalculateRandomDamage(CharacterData.Instance.Strength, enemy), "Normal", thisUnit, false);
        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            ArenaEnemyPlayerMovement movement = thisUnit.GetComponent<ArenaEnemyPlayerMovement>();
            characterToCounterHealthManager.ReduceHealth(movement.CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy), "Normal", thisUnit, false);
        }

        // Start the coroutine and pass the callback for when the attack is finished
        StartCoroutine(StopHitWithDelay(anim, onCounterAttackFinished));

        return true;
    }

    public bool ReplayCounterStrike(GameObject enemy, System.Action onCounterAttackFinished)
    {
        ReplayHealthManager characterToCounterHealthManager = enemy.GetComponent<ReplayHealthManager>();
        Animator anim = GetComponent<Animator>();

        // Trigger the hit animation
        anim.SetTrigger("hit");


        if (thisUnit.tag == "Player")
        {
            ReplayPlayerMovement movement = thisUnit.GetComponent<ReplayPlayerMovement>();
            movement.MovePlayerToRight(0.5f);
        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            ReplayEnemyPlayerMovement movement = thisUnit.GetComponent<ReplayEnemyPlayerMovement>();
            movement.MovePlayerToLeft(0.5f);
        }

        if (thisUnit.tag == "Player")
        {
            if (enemy.tag == "EnemyGlad")
            {
                ReplayEnemyPlayerMovement enemyMovement = enemy.GetComponent<ReplayEnemyPlayerMovement>();
                enemyMovement.MovePlayerToRight(0.5f);
            }
            else if (enemy.tag == "EnemyPet1" && enemy.tag == "EnemyPet2" && enemy.tag == "EnemyPet3")
            {
                ReplayEnemyPetMovement enemyPetMovement = enemy.GetComponent<ReplayEnemyPetMovement>();
                enemyPetMovement.MovePetToRight(0.5f);
            }

        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            if (enemy.tag == "Player")
            {
                ReplayPlayerMovement enemyMovement = enemy.GetComponent<ReplayPlayerMovement>();
                enemyMovement.MovePlayerToLeft(0.5f);
            }
            else if (enemy.tag == "Pet1" && enemy.tag == "Pet2" && enemy.tag == "Pet3")
            {
                ReplayPetMovement enemyPetMovement = enemy.GetComponent<ReplayPetMovement>();
                enemyPetMovement.MovePetToLeft(0.5f);
            }
        }
        if (thisUnit.tag == "Player")
        {
            ReplayPlayerMovement movement = thisUnit.GetComponent<ReplayPlayerMovement>();
            characterToCounterHealthManager.ReduceHealth(GetCounterAttackDamage(), "Normal", thisUnit, false);
        }
        else if (thisUnit.tag == "EnemyGlad")
        {
            ReplayEnemyPlayerMovement movement = thisUnit.GetComponent<ReplayEnemyPlayerMovement>();
            characterToCounterHealthManager.ReduceHealth(GetCounterAttackDamage(), "Normal", thisUnit, false);
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

    private int GetCounterAttackDamage()
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = ReplayGameManager.Instance.RoundsCount;

        var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

        var lastAction = actionsThisTurn.Last();

        return lastAction.Value;
    }
    public void CleaveDamage(List<GameObject> allAliveEnemies, GameObject mainTarget, int damage, GameObject doneBy)
    {
        foreach (GameObject enemy in allAliveEnemies)
        {
            if (enemy.tag != mainTarget.tag)
            {
                HealthManager HM = enemy.GetComponent<HealthManager>();
                HM.ReduceHealth(damage / 2, "Normal", doneBy, false);
            }
        }

    }

    public void ArenaCleaveDamage(List<GameObject> allAliveEnemies, GameObject mainTarget, int damage, GameObject doneBy)
    {
        foreach (GameObject enemy in allAliveEnemies)
        {
            if (enemy.tag != mainTarget.tag)
            {
                ArenaHealthManager HM = enemy.GetComponent<ArenaHealthManager>();
                HM.ReduceHealth(damage / 2, "Normal", doneBy, false);
            }
        }

    }
    public void ReplayCleaveDamage(List<GameObject> allAliveEnemies, GameObject mainTarget, int damage, GameObject doneBy)
    {
        foreach (GameObject enemy in allAliveEnemies)
        {
            if (enemy.tag != mainTarget.tag)
            {
                ReplayHealthManager HM = enemy.GetComponent<ReplayHealthManager>();
                HM.ReduceHealth(damage / 2, "Normal", doneBy, false);
            }
        }

    }

    public void AddDavidStats()
    {
        int Str = 0;
        davidAgiBonus = Mathf.RoundToInt(CharacterData.Instance.Agility * 0.1f);
        int Inte = 0;
        int Health = 0;
        davidHitBonus = Mathf.RoundToInt(CharacterData.Instance.precision * 2);
        int Defense = 0;
        int Stun = 5;
        int LifeSt = 0;

        if (thisUnit.tag == "Player")
            CharacterData.Instance.AddEquipStats(Str, davidAgiBonus, Inte, Health, davidHitBonus, Defense, 0, Stun, LifeSt, 0, 0);
        else if (thisUnit.tag == "EnemyGlad")
            EnemyGladiatorData.Instance.AddEquipStats(Str, davidAgiBonus, Inte, Health, davidHitBonus, Defense, 0, Stun, LifeSt, 0, 0);
    }

    public void RemoveDavidStats()
    {
        int Str = 0;
        int Inte = 0;
        int Health = 0;
        int Defense = 0;
        int Stun = 5;
        int LifeSt = 0;

        if (thisUnit.tag == "Player")
            CharacterData.Instance.RemoveEquipStats(Str, davidAgiBonus, Inte, Health, davidHitBonus, Defense, 0, Stun, LifeSt, 0, 0);
        else if (thisUnit.tag == "EnemyGlad")
            EnemyGladiatorData.Instance.RemoveEquipStats(Str, davidAgiBonus, Inte, Health, davidHitBonus, Defense, 0, Stun, LifeSt, 0, 0);
    }

}

