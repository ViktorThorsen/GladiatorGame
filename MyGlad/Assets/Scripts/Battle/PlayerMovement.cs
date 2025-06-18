using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class PlayerMovement : MonoBehaviour
{
    private GameManager gameManager;
    private Animator anim;
    private GameObject player;
    private InventoryBattleHandler playerInventoryBattleHandler;
    private SkillBattleHandler skillBattleHandler;
    [SerializeField] private Transform playerFeet;
    private HealthManager playerHealthManager;

    [SerializeField] private int playerSpeed;
    private GameObject enemy;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private Transform enemyFeet;
    private HealthManager enemyHealthManager;
    private EnemyMovement enemyMovement;
    private MonsterStats monsterStats;
    public int positionIndex;
    private bool isMoving;
    private bool isStunned;
    private int stunnedAtRound;
    private Coroutine moveCoroutine;

    bool valid = false;
    bool targetEnemySet = false;

    public int berserkDamage;

    public int venomousTouchDamage;

    private int momentum = 10;



    Vector3 screenLeft;
    Vector3 screenRight;

    public bool IsMoving
    {
        get { return isMoving; }
        set { isMoving = value; }
    }
    public bool IsStunned
    {
        get { return isStunned; }
        set { isStunned = value; }
    }
    public int StunnedAtRound
    {
        get { return stunnedAtRound; }
        set { stunnedAtRound = value; }
    }

    void Start()
    {

        gameManager = FindObjectOfType<GameManager>();

        player = gameObject;
        // Lägg till alla fiender i availableEnemies-listan
        foreach (GameObject enemy in gameManager.GetEnemies())
        {
            availableEnemies.Add(enemy);
        }
        player = gameObject;
        playerFeet = player.transform.Find("Feet");
        anim = GetComponent<Animator>();
        playerSpeed = 40;
        playerInventoryBattleHandler = player.GetComponent<InventoryBattleHandler>();
        skillBattleHandler = player.GetComponent<SkillBattleHandler>();
        playerHealthManager = player.GetComponent<HealthManager>();

        screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane));
    }

    void FixedUpdate()
    {
        if (IsMoving && player.transform != null && playerFeet != null)
        {
            if (!valid)
            {
                valid = RollForEnemy();
            }
            if (targetEnemySet)
            {
                float distanceToEnemy = Vector2.Distance(playerFeet.position, enemyFeet.position);
                float stoppingDistance = 0.5f; // Adjust this value as needed to stop before collision

                if (distanceToEnemy > stoppingDistance)
                {
                    // Start moving towards the enemy's feet
                    if (moveCoroutine == null)
                    {
                        moveCoroutine = StartCoroutine(MoveTowards(enemyFeet.position, stoppingDistance));
                    }
                }
            }
        }
    }

    IEnumerator MoveTowards(Vector2 targetPosition, float stoppingDistance)
    {
        // Ensure the player faces the target position
        if (playerFeet.position.x > targetPosition.x)
        {
            player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x) * -1, player.transform.localScale.y, player.transform.localScale.z);
        }
        else
        {
            player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z);
        }

        while (Vector2.Distance(playerFeet.position, targetPosition) > stoppingDistance)
        {
            anim.SetBool("run", true);
            Vector2 newPosition2D = Vector2.MoveTowards(playerFeet.position, targetPosition, playerSpeed * Time.deltaTime);
            Vector3 delta = new Vector3(newPosition2D.x - playerFeet.position.x, newPosition2D.y - playerFeet.position.y, 0);
            transform.position += delta;
            yield return null;
        }

        anim.SetBool("run", false);
        isMoving = false;
        moveCoroutine = null;
        gameManager.leftPositionsAvailable[positionIndex] = true;

        StartCoroutine(Fight());
    }

    IEnumerator Fight()
    {

        Item currentWeapon = playerInventoryBattleHandler.currentWeapon;
        yield return new WaitForSeconds(0.5f);

        // Always trigger the first hit animation and movement
        anim.SetTrigger("hit");
        MovePlayerToRight(0.5f);

        // Check if the enemy dodges the first hit
        if (EnemyDodges())
        {
            enemyMovement.Dodge();
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
            MoveBackToRandomStart(); // Stop combo and move back to start
            yield break; // End the coroutine here
        }
        else
        {
            if (!gameManager.IsGameOver)
            {
                enemyMovement.MoveEnemyToRight(0.5f);
                bool enemyStunned = CalculateStun();
                if (enemyStunned) { enemyMovement.Stun(); }
                if (Inventory.Instance.HasSkill("VenomousTouch"))
                {
                    ApplyVenom(player);
                }
                int damage = CalculateRandomDamage(CharacterData.Instance.Strength, enemy);
                bool isCrit = false;
                int randomValue = Random.Range(0, 100);
                if (randomValue < CharacterData.Instance.CritRate)
                {
                    isCrit = true;
                    damage = damage * 2;
                }
                enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                CalcLifesteal(damage + berserkDamage);
                if (Inventory.Instance.HasSkill("Cleave"))
                {
                    skillBattleHandler.CleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                }
                RollForDestroyWeapon();
            }
        }
        yield return new WaitForSeconds(0.5f);

        int randomNumber = Random.Range(0, 100) + CharacterData.Instance.combo;
        if (randomNumber > 60)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePlayerToRight(0.5f);

            // Check if the enemy dodges the second hit
            if (EnemyDodges())
            {
                enemyMovement.Dodge();
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                anim.SetTrigger("stophit");
                MoveBackToRandomStart(); // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                enemyMovement.MoveEnemyToRight(0.5f);
                int damage = CalculateRandomDamage(CharacterData.Instance.Strength, enemy);
                bool isCrit = false;
                int randomValue = Random.Range(0, 100);
                if (randomValue < CharacterData.Instance.CritRate)
                {
                    isCrit = true;
                    damage = damage * 2;
                }
                enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                CalcLifesteal(damage + berserkDamage);
                if (Inventory.Instance.HasSkill("Cleave"))
                {
                    skillBattleHandler.CleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                }
                RollForDestroyWeapon();
            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100) + CharacterData.Instance.combo;
            if (randomNumber1 > 60)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hook");
                MovePlayerToRight(0.5f);
                // Check if the enemy dodges the third hit
                if (EnemyDodges())
                {
                    enemyMovement.Dodge();
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart(); // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    enemyMovement.MoveEnemyToRight(0.5f);
                    int damage = CalculateRandomDamage(CharacterData.Instance.Strength, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < CharacterData.Instance.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                    CalcLifesteal(damage + berserkDamage);
                    if (Inventory.Instance.HasSkill("Cleave"))
                    {
                        skillBattleHandler.CleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon();
                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100) + CharacterData.Instance.combo;
                if (randomNumber2 > 60)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("uppercut");
                    MovePlayerToRight(0.5f);
                    // Check if the enemy dodges the fourth hit
                    if (EnemyDodges())
                    {
                        enemyMovement.Dodge();
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
                        MoveBackToRandomStart(); // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        enemyMovement.MoveEnemyToRight(0.5f);
                        int damage = CalculateRandomDamage(CharacterData.Instance.Strength, enemy);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < CharacterData.Instance.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;
                        }
                        enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                        CalcLifesteal(damage + berserkDamage);
                        if (Inventory.Instance.HasSkill("Cleave"))
                        {
                            skillBattleHandler.CleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                        }
                        RollForDestroyWeapon();
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart();
                }
                else
                {
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart();
                }
            }
            else
            {
                anim.SetTrigger("stophit");
                MoveBackToRandomStart();
            }
        }
        else
        {
            anim.SetTrigger("stophit");
            MoveBackToRandomStart();
        }
    }

    public bool RollForEnemy()
    {
        // Filter out dead enemies/pets from the availableEnemies list
        List<GameObject> aliveEnemies = new List<GameObject>();

        foreach (GameObject enemy in availableEnemies)
        {
            HealthManager HM = enemy.GetComponent<HealthManager>();
            if (!HM.IsDead)
            {
                aliveEnemies.Add(enemy); // Only add alive enemies to the list
            }
        }

        // If no enemies are alive, end the game
        if (aliveEnemies.Count == 0)
        {
            IsMoving = false;
            gameManager.GameOver("Player");
            return false;
        }

        // Roll a random index for selecting a target from alive enemies
        int randomIndex = Random.Range(0, aliveEnemies.Count);
        GameObject selectedEnemy = aliveEnemies[randomIndex];

        // Now proceed with setting the target as player or pet
        enemy = selectedEnemy;
        enemyFeet = enemy.transform.Find("Feet");
        enemyHealthManager = enemy.GetComponent<HealthManager>();
        enemyMovement = enemy.GetComponent<EnemyMovement>();
        monsterStats = enemy.GetComponent<MonsterStats>();
        targetEnemySet = true;
        return true;
    }
    private List<GameObject> GetAllAliveEnemies()
    {
        List<GameObject> aliveEnemies = new List<GameObject>();

        foreach (GameObject enemy in availableEnemies)
        {
            HealthManager HM = enemy.GetComponent<HealthManager>();
            if (!HM.IsDead)
            {
                aliveEnemies.Add(enemy); // Only add alive enemies to the list
            }
        }
        return aliveEnemies;
    }

    private bool EnemyDodges()
    {
        HealthManager arenaHealthManager = enemy.GetComponent<HealthManager>();
        if (!arenaHealthManager.IsDead)
        {
            if (enemyMovement.IsStunned)
            {
                return false;
            }
            else
            {
                int dodgeChance = monsterStats.DodgeRate;
                int randomValue = Random.Range(0, 100);
                dodgeChance = dodgeChance - CharacterData.Instance.HitRate;
                return randomValue < dodgeChance;
            }
        }
        else return false;
    }

    public void MovePlayerToRight(float distance)
    {
        if (enemy != null)
        {
            if (enemy.transform.position.x < screenRight.x - 1f)
            {
                // Get the target position for the player
                Vector3 targetPosition = transform.position;
                targetPosition.x += distance; // Move to the right by 'distance' units

                // Move the player towards the enemy's new position
                transform.position = targetPosition;
            }
        }
        //This is the counter Attack Without enemyTarget
        else
        {
            Vector3 targetPosition = transform.position;
            targetPosition.x += distance; // Move to the right by 'distance' units

            // Move the player towards the enemy's new position
            transform.position = targetPosition;
        }
    }

    public void MovePlayerToLeft(float distance)
    {
        anim.SetTrigger("takedamage");
        if (transform.position.x > screenLeft.x + 2f)
        {
            Vector3 newPosition = transform.position;
            newPosition.x -= distance; // Move to the right by 'distance' units
            transform.position = newPosition;
        }
    }
    public void Stun()
    {
        isStunned = true;
        anim.SetBool("stunned", true);
        StunnedAtRound = gameManager.RoundsCount;
    }
    public void RemoveStun()
    {
        if (gameManager.RoundsCount > StunnedAtRound + 1)
        {
            isStunned = false;
            anim.SetBool("stunned", false);
        }
    }
    private void CalcLifesteal(int damage)
    {
        int baseLifeSteal = CharacterData.Instance.LifeSteal;

        if (baseLifeSteal > 0)
        {
            float lifeStealMultiplier = baseLifeSteal / 100f;

            var vampSkillInstance = Inventory.Instance.GetSkills()
                .FirstOrDefault(s => s.skillName == "Vampyre");

            if (vampSkillInstance != null)
            {
                Skill vampSkillData = vampSkillInstance.GetSkillData(); // Hämtar ScriptableObject från databasen
                int level = vampSkillInstance.level;

                int bonusPercent = level switch
                {
                    1 => vampSkillData.effectPercentIncreaseLevel1,
                    2 => vampSkillData.effectPercentIncreaseLevel2,
                    3 => vampSkillData.effectPercentIncreaseLevel3,
                    _ => 0
                };

                lifeStealMultiplier += bonusPercent / 100f;
            }

            int vampBonus = Mathf.RoundToInt(damage * lifeStealMultiplier);
            if (vampBonus < 1) vampBonus = 1;

            playerHealthManager.IncreaseHealth(vampBonus);
        }
    }

    private bool CalculateStun()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < CharacterData.Instance.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }

    private void ApplyVenom(GameObject dealer)
    {
        enemyHealthManager.ApplyVenom(dealer);
    }

    public int CalculateRandomDamage(int baseDamage, GameObject enemy)
    {
        monsterStats = enemy.GetComponent<MonsterStats>();
        // Calculate a random damage between baseDamage - 2 and baseDamage + 2
        int minDamage = Mathf.RoundToInt(baseDamage * 0.9f);
        int maxDamage = Mathf.RoundToInt(baseDamage * 1.1f);

        // Ensure the minimum damage is at least 1
        if (minDamage < 1) { minDamage = 1; }
        int randomDmg = Random.Range(minDamage, maxDamage + 1);
        int effectiveDefense;
        if (Inventory.Instance.HasSkill("SurgicalCut"))
        {
            effectiveDefense = Mathf.RoundToInt(monsterStats.defense * 0.5f);
        }
        else
        {
            effectiveDefense = monsterStats.defense;
        }

        randomDmg = randomDmg - effectiveDefense;
        if (randomDmg < 1)
        {
            randomDmg = 1;
        }
        return randomDmg; // Random.Range is inclusive for integers
    }

    public void MoveBackToRandomStart()
    {
        if (moveCoroutine == null)
        {
            // Hämta en slumpmässig ledig position
            int randomPositionIndex = gameManager.GetRandomAvailablePosition(gameManager.leftPositionsAvailable);

            // Om det finns en tillgänglig position
            if (randomPositionIndex != -1)
            {
                Transform randomStartPos = gameManager.playerStartPositions[randomPositionIndex];

                // Starta flytt-koroutinen till den slumpmässiga positionen
                moveCoroutine = StartCoroutine(MoveBackToStart(randomStartPos.position));


                // Ställ in den valda positionen som upptagen
                gameManager.leftPositionsAvailable[randomPositionIndex] = false;
                positionIndex = randomPositionIndex;
                valid = false;
                targetEnemySet = false;
                if (playerHealthManager.CurrentHealth > playerHealthManager.maxHealth / 3)
                {
                    berserkDamage = 0;
                    skillBattleHandler.EndBerserk(player);
                }

            }
        }
    }

    IEnumerator MoveBackToStart(Vector3 targetPosition)
    {
        float tolerance = 0.1f;

        while (Vector2.Distance(transform.position, targetPosition) > tolerance)
        {
            // Ensure the player is always facing left (flip if necessary) during the movement
            if (transform.position.x > targetPosition.x)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            anim.SetBool("run", true);
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, playerSpeed * Time.deltaTime);
            transform.position = newPosition;
            yield return null;
        }

        anim.SetBool("run", false);
        moveCoroutine = null;
        IsMoving = false;
        gameManager.RollTime = true;
    }

    private void RollForDestroyWeapon()
    {
        if (playerInventoryBattleHandler.currentWeapon != null)
        {
            int randomWeaponDurabilityNumber = Random.Range(0, playerInventoryBattleHandler.currentWeapon.durability);
            if (randomWeaponDurabilityNumber == 0)
            {
                playerInventoryBattleHandler.DestroyWeapon();
            }
        }
    }

    // New: Dodge Function
    public void Dodge()
    {
        // Calculate the center Y position of the screen using the top and bottom edges
        float screenBottomY = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).y;
        float screenTopY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, Camera.main.nearClipPlane)).y;
        float screenCenterY = (screenBottomY + screenTopY) / 2;

        float dodgeDistance = 1f;
        float dodgeDirection = transform.position.y < screenCenterY ? 1f : -1f;

        // ✅ Sätt dodgePosition med korrekt Z-värde
        Vector3 dodgePosition = new Vector3(transform.position.x, transform.position.y + dodgeDirection * dodgeDistance, transform.position.z);

        StartCoroutine(DodgeMove(dodgePosition));
    }

    IEnumerator DodgeMove(Vector3 targetPosition)
    {

        float duration = 0.15f; // hur snabbt dodgen ska gå
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // säkerställ exakt slutposition
    }
}

