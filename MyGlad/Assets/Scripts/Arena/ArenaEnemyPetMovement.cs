using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaEnemyPetMovement : MonoBehaviour
{
    private ArenaGameManager gameManager;
    private Animator anim;
    private GameObject pet;
    [SerializeField] private Transform petFeet;
    private ArenaHealthManager petHealthManager;

    [SerializeField] private int petSpeed;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private GameObject enemy;
    private Transform enemyFeet;
    private ArenaHealthManager enemyHealthManager;
    private ArenaPetMovement enemyPetMovement;
    private ArenaPlayerMovement enemyPlayerMovement;
    private MonsterStats enemyStats;
    private MonsterStats monsterStats;
    private Transform visualTransform;
    private bool isMoving;
    private bool isStunned;
    private int stunnedAtRound;
    private Coroutine moveCoroutine;
    Vector3 screenLeft;
    Vector3 screenRight;
    public int positionIndex;

    bool valid = false;
    bool targetEnemySet = false;

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
        pet = gameObject;
        foreach (GameObject enemy in gameManager.GetPlayerAndPets())
        {
            availableEnemies.Add(enemy);
        }
        petFeet = pet.transform.Find("Feet");
        visualTransform = transform.Find("Visual");
        if (visualTransform == null)
        {
            visualTransform = transform; // fallback för vanliga sprites
        }

        anim = visualTransform.GetComponent<Animator>();
        petSpeed = 40;
        petHealthManager = pet.GetComponent<ArenaHealthManager>();
        monsterStats = pet.GetComponent<MonsterStats>();


        screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane));
    }

    void FixedUpdate()
    {
        if (IsMoving && pet.transform != null && petFeet != null)
        {
            if (!valid)
            {
                valid = RollForEnemy();
            }
            if (targetEnemySet)
            {
                float distanceToEnemy = Vector2.Distance(petFeet.position, enemyFeet.position);
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
        if (petFeet.position.x < targetPosition.x)
        {
            pet.transform.localScale = new Vector3(Mathf.Abs(pet.transform.localScale.x) * -1, pet.transform.localScale.y, pet.transform.localScale.z);
        }
        else
        {
            pet.transform.localScale = new Vector3(Mathf.Abs(pet.transform.localScale.x), pet.transform.localScale.y, pet.transform.localScale.z);
        }
        Debug.Log("pet moves");
        while (Vector2.Distance(petFeet.position, targetPosition) > stoppingDistance)
        {
            anim.SetBool("run", true);
            Vector2 newPosition = Vector2.MoveTowards(petFeet.position, targetPosition, petSpeed * Time.deltaTime);
            // Update the main transform's position based on feet's new position difference
            transform.position += (Vector3)(newPosition - (Vector2)petFeet.position);
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
        yield return new WaitForSeconds(0.5f);

        // Always trigger the first hit animation and movement
        anim.SetTrigger("hit");
        MovePetToLeft(0.5f);
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
                Target = GetCharacterType(pet.tag),
                Value = 0
            });
            if (petHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
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
                    if (enemyStunned) { enemyPlayerMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                    CalcLifesteal(damage);
                }



                else if (enemy.tag == "Pet1" || enemy.tag == "Pet2" || enemy.tag == "Pet3")
                {
                    enemyPetMovement.MovePetToLeft(0.5f);

                    bool playerStunned = CalculateStun();
                    if (playerStunned) { enemyPetMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                }
            }

        }

        yield return new WaitForSeconds(0.5f);

        int randomNumber = Random.Range(0, 100) + monsterStats.combo;
        if (petHealthManager.CurrentHealth <= 0) { randomNumber = 0; }
        if (randomNumber > 60)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePetToLeft(0.5f);
            if (monsterStats.LifeSteal > 0)
            {
                petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
            }
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
                    Target = GetCharacterType(pet.tag),
                    Value = 0
                });
                if (petHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
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
                    if (enemyStunned) { enemyPlayerMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);

                }


                else if (enemy.tag == "Pet1" || enemy.tag == "Pet2" || enemy.tag == "Pet3")
                {
                    enemyPetMovement.MovePetToLeft(0.5f);

                    bool playerStunned = CalculateStun();
                    if (playerStunned) { enemyPetMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                }

            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100) + monsterStats.combo;
            if (petHealthManager.CurrentHealth <= 0) { randomNumber1 = 0; }
            if (randomNumber1 > 60)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hit2");
                MovePetToLeft(0.5f);
                if (monsterStats.LifeSteal > 0)
                {
                    petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
                }
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
                        Target = GetCharacterType(pet.tag),
                        Value = 0
                    });
                    if (petHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
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
                        if (enemyStunned) { enemyPlayerMovement.Stun(); }
                        int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < monsterStats.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;
                        }
                        enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);

                    }


                    else if (enemy.tag == "Pet1" || enemy.tag == "Pet2" || enemy.tag == "Pet3")
                    {
                        enemyPetMovement.MovePetToLeft(0.5f);

                        bool playerStunned = CalculateStun();
                        if (playerStunned) { enemyPetMovement.Stun(); }
                        int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < monsterStats.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;
                        }
                        enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);

                    }

                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100) + monsterStats.combo;
                if (petHealthManager.CurrentHealth <= 0) { randomNumber2 = 0; }
                if (randomNumber2 > 60)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("hit3");
                    MovePetToLeft(0.5f);
                    if (monsterStats.LifeSteal > 0)
                    {
                        petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
                    }
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
                            Target = GetCharacterType(pet.tag),
                            Value = 0
                        });
                        if (petHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
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
                            if (enemyStunned) { enemyPlayerMovement.Stun(); }
                            int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                            bool isCrit = false;
                            int randomValue = Random.Range(0, 100);
                            if (randomValue < monsterStats.CritRate)
                            {
                                isCrit = true;
                                damage = damage * 2;
                            }
                            enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);

                        }


                        else if (enemy.tag == "Pet1" || enemy.tag == "Pet2" || enemy.tag == "Pet3")
                        {
                            enemyPetMovement.MovePetToLeft(0.5f);

                            bool playerStunned = CalculateStun();
                            if (playerStunned) { enemyPetMovement.Stun(); }
                            int damage = CalculateRandomDamage(monsterStats.AttackDamage, enemy);
                            bool isCrit = false;
                            int randomValue = Random.Range(0, 100);
                            if (randomValue < monsterStats.CritRate)
                            {
                                isCrit = true;
                                damage = damage * 2;
                            }
                            enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);

                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !enemyPlayerMovement.IsStunned)
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ArenaCounterStrike(pet, () =>
                        {
                            if (petHealthManager.CurrentHealth > 0)
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
                    else if (petHealthManager.CurrentHealth > 0)
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
                    if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !enemyPlayerMovement.IsStunned)
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ArenaCounterStrike(pet, () =>
                        {
                            if (petHealthManager.CurrentHealth > 0)
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
                    else if (petHealthManager.CurrentHealth > 0)
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
                if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !enemyPlayerMovement.IsStunned)
                {
                    SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                    // Call CounterStrike and pass MoveBackToRandomStart as the callback
                    playerSkills.ArenaCounterStrike(pet, () =>
                    {
                        if (petHealthManager.CurrentHealth > 0)
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
                else if (petHealthManager.CurrentHealth > 0)
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
            if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !enemyPlayerMovement.IsStunned)
            {
                SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                // Call CounterStrike and pass MoveBackToRandomStart as the callback
                playerSkills.ArenaCounterStrike(pet, () =>
                {
                    if (petHealthManager.CurrentHealth > 0)
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
            else if (petHealthManager.CurrentHealth > 0)
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

    bool RollForEnemy()
    {
        // Kontrollera om det finns några fiender i listan
        // Kontrollera om alla fiender är döda
        List<GameObject> aliveEnemies = new List<GameObject>();

        foreach (GameObject enemy in availableEnemies)
        {
            ArenaHealthManager HM = enemy.GetComponent<ArenaHealthManager>();
            if (!HM.IsDead)
            {
                aliveEnemies.Add(enemy); // Only add alive enemies to the list
            }
        }

        if (aliveEnemies.Count == 0)
        {
            IsMoving = false;
            gameManager.GameOver("EnemyGlad");
            return false;
        }

        // Rolla en slumpmässig index för att välja fiende
        int randomIndex = Random.Range(0, aliveEnemies.Count);
        GameObject selectedEnemy = aliveEnemies[randomIndex];

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
        // Om inga fiender finns kvar
        return true;
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
                    dodgeChance = dodgeChance - monsterStats.hitRate;
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
                    dodgeChance = dodgeChance - monsterStats.hitRate;
                    return randomValue < dodgeChance;
                }
            }
        }
        else return false;
    }

    public void MovePetToRight(float distance)
    {
        anim.SetTrigger("takedamage");

        if (transform.position.x < screenRight.x - 2f)
        {
            Vector3 newPosition = transform.position;
            newPosition.x += distance; // Move to the right by 'distance' units
            transform.position = newPosition;
        }
    }

    public void MovePetToLeft(float distance)
    {
        if (enemy != null)
        {
            if (transform.position.x > screenLeft.x + 2f)
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
            Actor = GetCharacterType(pet.tag),
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
    private void CalcLifesteal(int damage)
    {
        int baseLifeSteal = monsterStats.LifeSteal;

        if (baseLifeSteal > 0)
        {
            float lifeStealMultiplier = baseLifeSteal / 100f;

            int vampBonus = Mathf.RoundToInt(damage * lifeStealMultiplier);
            if (vampBonus < 1)
            {
                vampBonus = 1;
            }

            petHealthManager.IncreaseHealth(vampBonus);
        }
    }

    private bool CalculateStun()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < monsterStats.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }

    private int CalculateRandomDamage(int baseDamage, GameObject enemy)
    {
        if (enemy.tag == "Player")
        {
            // Calculate a random damage between baseDamage - 2 and baseDamage + 2
            int minDamage = Mathf.RoundToInt(baseDamage * 0.9f);
            int maxDamage = Mathf.RoundToInt(baseDamage * 1.1f);

            // Ensure the minimum damage is at least 1
            if (minDamage < 1) { minDamage = 1; }
            int randomDmg = Random.Range(minDamage, maxDamage + 1);
            randomDmg = randomDmg - CharacterData.Instance.Defense;
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
            randomDmg = randomDmg - monsterStats.defense;
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
            }
        }
    }

    IEnumerator MoveBackToStart(Vector2 targetPosition)
    {
        float tolerance = 0.1f;

        while (Vector2.Distance(transform.position, targetPosition) > tolerance)
        {
            // Ensure the pet is always facing left (flip if necessary) during the movement
            if (transform.position.x < targetPosition.x)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            anim.SetBool("run", true);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, petSpeed * Time.deltaTime);
            yield return null;
        }

        anim.SetBool("run", false);
        moveCoroutine = null;
        IsMoving = false;
        gameManager.RollTime = true;
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
