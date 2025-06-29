using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class ArenaEnemyPlayerMovement : MonoBehaviour
{
    private ArenaGameManager gameManager;
    private Animator anim;
    private GameObject player;
    private ArenaEnemyInventoryBattleHandler playerInventoryBattleHandler;
    private SkillBattleHandler skillBattleHandler;
    [SerializeField] private Transform playerFeet;
    private ArenaHealthManager playerHealthManager;

    [SerializeField] private int playerSpeed;
    private GameObject enemy;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private Transform enemyFeet;
    private ArenaHealthManager enemyHealthManager;
    private ArenaPlayerMovement enemyPlayerMovement;
    private ArenaPetMovement enemyPetMovement;
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

        gameManager = FindObjectOfType<ArenaGameManager>();

        player = gameObject;
        // Lägg till alla fiender i availableEnemies-listan
        foreach (GameObject enemy in gameManager.GetPlayerAndPets())
        {
            availableEnemies.Add(enemy);
        }
        playerFeet = player.transform.Find("Feet");
        anim = GetComponent<Animator>();
        playerSpeed = 40;
        playerInventoryBattleHandler = player.GetComponent<ArenaEnemyInventoryBattleHandler>();
        skillBattleHandler = player.GetComponent<SkillBattleHandler>();
        playerHealthManager = player.GetComponent<ArenaHealthManager>();

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
        if (playerFeet.position.x < targetPosition.x)
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
            Vector2 newPosition = Vector2.MoveTowards(playerFeet.position, targetPosition, playerSpeed * Time.deltaTime);
            // Update the main transform's position based on feet's new position difference
            transform.position += (Vector3)(newPosition - (Vector2)playerFeet.position);
            yield return null;
        }

        anim.SetBool("run", false);
        isMoving = false;
        moveCoroutine = null;
        gameManager.rightPositionsAvailable[positionIndex] = true;

        StartCoroutine(Fight());
    }

    IEnumerator Fight()
    {

        Item currentWeapon = playerInventoryBattleHandler.currentWeapon;
        yield return new WaitForSeconds(0.5f);

        // Always trigger the first hit animation and movement
        anim.SetTrigger("hit");
        MovePlayerToLeft(0.5f);
        // Check if the enemy dodges the first hit
        if (EnemyDodges())
        {
            if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
            ReplayData.Instance.AddAction(new MatchEventDTO
            {
                Turn = gameManager.RoundsCount,
                Actor = GetCharacterType(enemy.tag), //Den som dodgar
                Action = "dodge",
                Target = CharacterType.EnemyGlad,
                Value = 0
            });
            if (playerHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
            else
            {
                anim.SetBool("run", false);
                moveCoroutine = null;
                IsMoving = false;
                gameManager.RollTime = true;
            } // Stop combo and move back to start
            yield break; // End the coroutine here
        }
        else
        {
            if (!gameManager.IsGameOver)
            {
                if (enemy.tag == "Player")
                {
                    enemyPlayerMovement.MovePlayerToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned && !Inventory.Instance.HasSkill("IronWill")) { enemyPlayerMovement.Stun(); }
                    if (EnemyInventory.Instance.HasSkill("VenomousTouch") && !Inventory.Instance.HasSkill("CleanseBorn"))
                    {
                        ApplyVenom(player);
                    }
                    int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < EnemyGladiatorData.Instance.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;

                    }
                    enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                    CalcLifesteal(damage + berserkDamage);
                    if (EnemyInventory.Instance.HasSkill("Cleave"))
                    {
                        skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon();

                }
                else
                {
                    enemyPetMovement.MovePetToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                    {
                        ApplyVenom(player);
                    }
                    int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < EnemyGladiatorData.Instance.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;

                    }
                    enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                    CalcLifesteal(damage + berserkDamage);
                    if (EnemyInventory.Instance.HasSkill("Cleave"))
                    {
                        skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon();
                }
            }
        }
        yield return new WaitForSeconds(0.5f);

        int randomNumber = Random.Range(0, 100) + EnemyGladiatorData.Instance.combo;
        if (playerHealthManager.CurrentHealth <= 0) { randomNumber = 0; }
        if (randomNumber > 60)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePlayerToLeft(0.5f);
            // Check if the enemy dodges the second hit
            if (EnemyDodges())
            {
                if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                anim.SetTrigger("stophit");
                ReplayData.Instance.AddAction(new MatchEventDTO
                {
                    Turn = gameManager.RoundsCount,
                    Actor = GetCharacterType(enemy.tag), //Den som dodgar
                    Action = "dodge",
                    Target = CharacterType.EnemyGlad,
                    Value = 0
                });
                if (playerHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
                else
                {
                    anim.SetBool("run", false);
                    moveCoroutine = null;
                    IsMoving = false;
                    gameManager.RollTime = true;
                } // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                if (enemy.tag == "Player")
                {
                    enemyPlayerMovement.MovePlayerToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned && !Inventory.Instance.HasSkill("IronWill")) { enemyPlayerMovement.Stun(); }
                    int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < EnemyGladiatorData.Instance.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;

                    }
                    enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                    CalcLifesteal(damage + berserkDamage);
                    if (EnemyInventory.Instance.HasSkill("Cleave"))
                    {
                        skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon();
                }
                else
                {
                    enemyPetMovement.MovePetToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < EnemyGladiatorData.Instance.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;

                    }
                    enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                    CalcLifesteal(damage + berserkDamage);
                    if (EnemyInventory.Instance.HasSkill("Cleave"))
                    {
                        skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon();

                }
            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100) + EnemyGladiatorData.Instance.combo;
            if (playerHealthManager.CurrentHealth <= 0) { randomNumber1 = 0; }
            if (randomNumber1 > 60)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hook");
                MovePlayerToLeft(0.5f);
                // Check if the enemy dodges the third hit
                if (EnemyDodges())
                {
                    if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    ReplayData.Instance.AddAction(new MatchEventDTO
                    {
                        Turn = gameManager.RoundsCount,
                        Actor = GetCharacterType(enemy.tag), //Den som dodgar
                        Action = "dodge",
                        Target = CharacterType.EnemyGlad,
                        Value = 0
                    });
                    if (playerHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
                    else
                    {
                        anim.SetBool("run", false);
                        moveCoroutine = null;
                        IsMoving = false;
                        gameManager.RollTime = true;
                    } // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    if (enemy.tag == "Player")
                    {
                        enemyPlayerMovement.MovePlayerToLeft(0.5f);

                        bool enemyStunned = CalculateStun();
                        if (enemyStunned && !Inventory.Instance.HasSkill("IronWill")) { enemyPlayerMovement.Stun(); }

                        int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < EnemyGladiatorData.Instance.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;

                        }
                        enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                        CalcLifesteal(damage + berserkDamage);
                        if (EnemyInventory.Instance.HasSkill("Cleave"))
                        {
                            skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                        }
                        RollForDestroyWeapon();

                    }
                    else
                    {
                        enemyPetMovement.MovePetToLeft(0.5f);

                        bool enemyStunned = CalculateStun();
                        if (enemyStunned) { enemyPetMovement.Stun(); }

                        int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < EnemyGladiatorData.Instance.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;

                        }
                        enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                        CalcLifesteal(damage + berserkDamage);
                        if (EnemyInventory.Instance.HasSkill("Cleave"))
                        {
                            skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                        }
                        RollForDestroyWeapon();

                    }
                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100) + EnemyGladiatorData.Instance.combo;
                if (playerHealthManager.CurrentHealth <= 0) { randomNumber2 = 0; }
                if (randomNumber2 > 60)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("uppercut");
                    MovePlayerToLeft(0.5f);
                    // Check if the enemy dodges the fourth hit
                    if (EnemyDodges())
                    {
                        if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
                        ReplayData.Instance.AddAction(new MatchEventDTO
                        {
                            Turn = gameManager.RoundsCount,
                            Actor = GetCharacterType(enemy.tag), //Den som dodgar
                            Action = "dodge",
                            Target = CharacterType.EnemyGlad,
                            Value = 0
                        });
                        if (playerHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
                        else
                        {
                            anim.SetBool("run", false);
                            moveCoroutine = null;
                            IsMoving = false;
                            gameManager.RollTime = true;
                        } // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        if (enemy.tag == "Player")
                        {
                            enemyPlayerMovement.MovePlayerToLeft(0.5f);

                            bool enemyStunned = CalculateStun();
                            if (enemyStunned && !Inventory.Instance.HasSkill("IronWill")) { enemyPlayerMovement.Stun(); }

                            int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                            bool isCrit = false;
                            int randomValue = Random.Range(0, 100);
                            if (randomValue < EnemyGladiatorData.Instance.CritRate)
                            {
                                isCrit = true;
                                damage = damage * 2;

                            }
                            enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                            CalcLifesteal(damage + berserkDamage);
                            if (EnemyInventory.Instance.HasSkill("Cleave"))
                            {
                                skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                            }
                            RollForDestroyWeapon();

                        }
                        else
                        {
                            enemyPetMovement.MovePetToLeft(0.5f);

                            bool enemyStunned = CalculateStun();
                            if (enemyStunned) { enemyPetMovement.Stun(); }

                            int damage = CalculateRandomDamage(EnemyGladiatorData.Instance.Strength, enemy);
                            bool isCrit = false;
                            int randomValue = Random.Range(0, 100);
                            if (randomValue < EnemyGladiatorData.Instance.CritRate)
                            {
                                isCrit = true;
                                damage = damage * 2;

                            }
                            enemyHealthManager.ReduceHealth(damage + berserkDamage, "Normal", player, isCrit);
                            CalcLifesteal(damage + berserkDamage);
                            if (EnemyInventory.Instance.HasSkill("Cleave"))
                            {
                                skillBattleHandler.ArenaCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                            }
                            RollForDestroyWeapon();

                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && EnemyInventory.Instance.HasSkill("Unreturnable") && !enemyPlayerMovement.IsStunned)
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ArenaCounterStrike(player, () =>
                        {
                            if (playerHealthManager.CurrentHealth > 0)
                            {
                                MoveBackToRandomStart();
                            }
                            else
                            {
                                anim.SetBool("run", false);
                                moveCoroutine = null;
                                IsMoving = false;
                                gameManager.RollTime = true;
                            }
                        });
                    }
                    else if (playerHealthManager.CurrentHealth > 0)
                    { MoveBackToRandomStart(); }
                    else
                    {
                        anim.SetBool("run", false);
                        moveCoroutine = null;
                        IsMoving = false;
                        gameManager.RollTime = true;
                    }
                }
                else
                {
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && EnemyInventory.Instance.HasSkill("Unreturnable") && !enemyPlayerMovement.IsStunned)
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ArenaCounterStrike(player, () =>
                        {
                            if (playerHealthManager.CurrentHealth > 0)
                            {
                                MoveBackToRandomStart();
                            }
                            else
                            {
                                anim.SetBool("run", false);
                                moveCoroutine = null;
                                IsMoving = false;
                                gameManager.RollTime = true;
                            }
                        });
                    }
                    else if (playerHealthManager.CurrentHealth > 0)
                    { MoveBackToRandomStart(); }
                    else
                    {
                        anim.SetBool("run", false);
                        moveCoroutine = null;
                        IsMoving = false;
                        gameManager.RollTime = true;
                    }
                }
            }
            else
            {
                anim.SetTrigger("stophit");
                yield return new WaitForSeconds(0.05f);
                int randomNumberCounter = Random.Range(0, 100);
                if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && EnemyInventory.Instance.HasSkill("Unreturnable") && !enemyPlayerMovement.IsStunned)
                {
                    SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                    // Call CounterStrike and pass MoveBackToRandomStart as the callback
                    playerSkills.ArenaCounterStrike(player, () =>
                    {
                        if (playerHealthManager.CurrentHealth > 0)
                        {
                            MoveBackToRandomStart();
                        }
                        else
                        {
                            anim.SetBool("run", false);
                            moveCoroutine = null;
                            IsMoving = false;
                            gameManager.RollTime = true;
                        }
                    });
                }
                else if (playerHealthManager.CurrentHealth > 0)
                { MoveBackToRandomStart(); }
                else
                {
                    anim.SetBool("run", false);
                    moveCoroutine = null;
                    IsMoving = false;
                    gameManager.RollTime = true;
                }
            }
        }
        else
        {
            anim.SetTrigger("stophit");
            yield return new WaitForSeconds(0.05f);
            int randomNumberCounter = Random.Range(0, 100);
            if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && EnemyInventory.Instance.HasSkill("Unreturnable") && !enemyPlayerMovement.IsStunned)
            {
                SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                // Call CounterStrike and pass MoveBackToRandomStart as the callback
                playerSkills.ArenaCounterStrike(player, () =>
                {
                    if (playerHealthManager.CurrentHealth > 0)
                    {
                        MoveBackToRandomStart();
                    }
                    else
                    {
                        anim.SetBool("run", false);
                        moveCoroutine = null;
                        IsMoving = false;
                        gameManager.RollTime = true;
                    }
                });
            }
            else if (playerHealthManager.CurrentHealth > 0)
            { MoveBackToRandomStart(); }
            else
            {
                anim.SetBool("run", false);
                moveCoroutine = null;
                IsMoving = false;
                gameManager.RollTime = true;
            }
        }
    }

    public bool RollForEnemy()
    {
        // Filter out dead enemies/pets from the availableEnemies list
        List<GameObject> aliveEnemies = new List<GameObject>();

        foreach (GameObject enemy in availableEnemies)
        {
            ArenaHealthManager HM = enemy.GetComponent<ArenaHealthManager>();
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
        if (selectedEnemy.tag == "Player")
        {
            enemy = selectedEnemy;
            enemyFeet = enemy.transform.Find("Feet");
            enemyHealthManager = enemy.GetComponent<ArenaHealthManager>();
            enemyPlayerMovement = enemy.GetComponent<ArenaPlayerMovement>();
            targetEnemySet = true;
        }
        else if (selectedEnemy.tag == "Pet1" || selectedEnemy.tag == "Pet2" || selectedEnemy.tag == "Pet3")
        {
            enemy = selectedEnemy;
            enemyFeet = enemy.transform.Find("Feet");
            enemyHealthManager = enemy.GetComponent<ArenaHealthManager>();
            enemyPetMovement = enemy.GetComponent<ArenaPetMovement>();
            targetEnemySet = true;
        }

        return true;
    }

    private void CalcLifesteal(int damage)
    {
        int baseLifeSteal = EnemyGladiatorData.Instance.LifeSteal;

        if (baseLifeSteal > 0)
        {
            float lifeStealMultiplier = baseLifeSteal / 100f;

            var vampSkillInstance = EnemyInventory.Instance.GetSkills()
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

    private bool EnemyDodges()
    {
        ArenaHealthManager arenaHealthManager = enemy.GetComponent<ArenaHealthManager>();
        if (!arenaHealthManager.IsDead)
        {
            if (enemy.tag == "Player")
            {
                if (enemyPlayerMovement.IsStunned)
                {
                    return false;
                }
                else
                {
                    int dodgeChance = CharacterData.Instance.DodgeRate; // Assume dodge chance is 30%
                    int randomValue = Random.Range(0, 100);
                    dodgeChance = dodgeChance - EnemyGladiatorData.Instance.HitRate;
                    return randomValue < dodgeChance;
                }
            }
            else
            {
                if (enemyPetMovement.IsStunned)
                {
                    return false;
                }
                else
                {
                    MonsterStats enemyPetstats = enemy.GetComponent<MonsterStats>();
                    int dodgeChance = enemyPetstats.DodgeRate; // Assume dodge chance is 30%
                    int randomValue = Random.Range(0, 100);
                    return randomValue < dodgeChance;
                }
            }
        }
        else return false;
    }

    public void MovePlayerToRight(float distance)
    {
        anim.SetTrigger("takedamage");

        if (transform.position.x < screenRight.x - 2f)
        {
            Vector3 newPosition = transform.position;
            newPosition.x += distance; // Move to the right by 'distance' units
            transform.position = newPosition;
        }
    }

    public void MovePlayerToLeft(float distance)
    {
        if (enemy != null)
        {
            if (player.transform.position.x > screenLeft.x + 2f)
            {
                Vector3 newPosition = transform.position;
                newPosition.x -= distance; // Move to the right by 'distance' units
                transform.position = newPosition;
            }
        }
        else
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
        ReplayData.Instance.AddAction(new MatchEventDTO
        {
            Turn = gameManager.RoundsCount,
            Actor = CharacterType.EnemyGlad,
            Action = "stunned",
            Target = CharacterType.None,
            Value = 0
        });
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

    private bool CalculateStun()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < EnemyGladiatorData.Instance.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }

    private void ApplyVenom(GameObject dealer)
    {
        enemyHealthManager.ApplyVenom(dealer);
    }
    private List<GameObject> GetAllAliveEnemies()
    {
        List<GameObject> aliveEnemies = new List<GameObject>();

        foreach (GameObject enemy in availableEnemies)
        {
            ArenaHealthManager HM = enemy.GetComponent<ArenaHealthManager>();
            if (!HM.IsDead)
            {
                aliveEnemies.Add(enemy); // Only add alive enemies to the list
            }
        }
        return aliveEnemies;
    }

    public int CalculateRandomDamage(int baseDamage, GameObject enemy)
    {
        if (enemy.tag == "Player")
        {
            // Calculate a random damage between baseDamage - 2 and baseDamage + 2
            int minDamage = Mathf.RoundToInt(baseDamage * 0.9f);
            int maxDamage = Mathf.RoundToInt(baseDamage * 1.1f);

            // Ensure the minimum damage is at least 1
            if (minDamage < 1) { minDamage = 1; }
            int randomDmg = Random.Range(minDamage, maxDamage + 1);
            int effectiveDefense;
            if (EnemyInventory.Instance.HasSkill("SurgicalCut"))
            {
                effectiveDefense = Mathf.RoundToInt(CharacterData.Instance.Defense * 0.5f);
            }
            else
            {
                effectiveDefense = CharacterData.Instance.Defense;
            }

            randomDmg = randomDmg - effectiveDefense;
            if (randomDmg < 1)
            {
                randomDmg = 1;
            }
            return randomDmg; // Random.Range is inclusive for integers
        }
        else
        {
            monsterStats = enemy.GetComponent<MonsterStats>();
            int minDamage = Mathf.RoundToInt(baseDamage * 0.9f);
            int maxDamage = Mathf.RoundToInt(baseDamage * 1.1f);

            // Ensure the minimum damage is at least 1
            if (minDamage < 1) { minDamage = 1; }
            int randomDmg = Random.Range(minDamage, maxDamage + 1);
            int effectiveDefense;
            if (EnemyInventory.Instance.HasSkill("SurgicalCut"))
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
    }

    public void MoveBackToRandomStart()
    {
        if (moveCoroutine == null)
        {
            // Hämta en slumpmässig ledig position
            int randomPositionIndex = gameManager.GetRandomAvailablePosition(gameManager.rightPositionsAvailable);

            // Om det finns en tillgänglig position
            if (randomPositionIndex != -1)
            {
                Transform randomStartPos = gameManager.enemyStartPositions[randomPositionIndex];

                // Starta flytt-koroutinen till den slumpmässiga positionen
                moveCoroutine = StartCoroutine(MoveBackToStart(randomStartPos.position));

                // Ställ in den valda positionen som upptagen
                gameManager.rightPositionsAvailable[randomPositionIndex] = false;
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

    IEnumerator MoveBackToStart(Vector2 targetPosition)
    {
        float tolerance = 0.1f;

        while (Vector2.Distance(transform.position, targetPosition) > tolerance)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            anim.SetBool("run", true);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, playerSpeed * Time.deltaTime);
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
                ReplayData.Instance.AddAction(new MatchEventDTO
                {
                    Turn = gameManager.RoundsCount,
                    Actor = CharacterType.EnemyGlad,
                    Action = "weapondestroyed",
                    Target = CharacterType.EnemyGlad,
                    Value = 0
                });
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

        // Determine dodge direction: dodge up if below center, down if above
        float dodgeDistance = 1f; // Customize dodge distance
        float dodgeDirection = transform.position.y < screenCenterY ? 1f : -1f; // Dodge up if below center, down if above

        // Calculate the dodge target position based on direction and distance
        Vector3 dodgePosition = new Vector3(transform.position.x, transform.position.y + dodgeDirection * dodgeDistance, transform.position.z);

        // Move the player to the dodge position over time
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

    private CharacterType GetCharacterType(string tag)
    {
        return tag switch
        {
            "Player" => CharacterType.Player,
            "EnemyGlad" => CharacterType.EnemyGlad,
            "EnemyPet1" => CharacterType.EnemyPet1,
            "EnemyPet2" => CharacterType.EnemyPet2,
            "EnemyPet3" => CharacterType.EnemyPet3,
            "Pet1" => CharacterType.Pet1,
            "Pet2" => CharacterType.Pet2,
            "Pet3" => CharacterType.Pet3,
            _ => CharacterType.None
        };
    }
}

