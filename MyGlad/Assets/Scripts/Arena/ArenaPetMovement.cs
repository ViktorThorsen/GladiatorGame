using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaPetMovement : MonoBehaviour
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
    private ArenaEnemyPetMovement enemyPetMovement;
    private ArenaEnemyPlayerMovement enemyGladMovement;
    private MonsterStats enemyStats;
    private MonsterStats monsterStats;
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
        foreach (GameObject enemy in gameManager.GetEnemies())
        {
            availableEnemies.Add(enemy);
        }
        petFeet = pet.transform.Find("Feet");
        anim = GetComponent<Animator>();
        petSpeed = 30;
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
        // Ensure the pet faces the target position
        if (petFeet.position.x > targetPosition.x)
        {
            pet.transform.localScale = new Vector3(Mathf.Abs(pet.transform.localScale.x) * -1, pet.transform.localScale.y, pet.transform.localScale.z);
        }
        else
        {
            pet.transform.localScale = new Vector3(Mathf.Abs(pet.transform.localScale.x), pet.transform.localScale.y, pet.transform.localScale.z);
        }

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
        gameManager.leftPositionsAvailable[positionIndex] = true;
        StartCoroutine(Fight());
    }

    IEnumerator Fight()
    {
        yield return new WaitForSeconds(0.5f);

        // Always trigger the first hit animation and movement
        anim.SetTrigger("hit");
        MovePetToRight(0.5f);
        if (monsterStats.LifeSteal > 0)
        {
            petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
        }
        // Check if the enemy dodges the first hit
        if (EnemyDodges())
        {
            if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
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
            MoveBackToRandomStart(); // Stop combo and move back to start
            yield break; // End the coroutine here
        }
        else
        {
            if (!gameManager.IsGameOver)
            {
                if (enemy.tag == "EnemyGlad")
                {
                    enemyGladMovement.MovePlayerToRight(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyGladMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                }



                else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                {
                    enemyPetMovement.MovePetToRight(0.5f);

                    bool playerStunned = CalculateStun();
                    if (playerStunned) { enemyPetMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                }
            }

        }

        yield return new WaitForSeconds(0.5f);

        int randomNumber = Random.Range(0, 100);
        if (randomNumber > 50)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePetToRight(0.5f);
            if (monsterStats.LifeSteal > 0)
            {
                petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
            }
            // Check if the enemy dodges the second hit
            if (EnemyDodges())
            {
                if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
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
                MoveBackToRandomStart(); // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                if (enemy.tag == "EnemyGlad")
                {
                    enemyGladMovement.MovePlayerToRight(0.5f);
                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyGladMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                }


                else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                {
                    enemyPetMovement.MovePetToRight(0.5f);
                    bool playerStunned = CalculateStun();
                    if (playerStunned) { enemyPetMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                    bool isCrit = false;
                    int randomValue = Random.Range(0, 100);
                    if (randomValue < monsterStats.CritRate)
                    {
                        isCrit = true;
                        damage = damage * 2;
                    }
                    enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                }
            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100);
            if (randomNumber1 > 50)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hit2");
                MovePetToRight(0.5f);
                if (monsterStats.LifeSteal > 0)
                {
                    petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
                }
                // Check if the enemy dodges the third hit
                if (EnemyDodges())
                {
                    if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
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
                    MoveBackToRandomStart(); // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    if (enemy.tag == "EnemyGlad")
                    {
                        enemyGladMovement.MovePlayerToRight(0.5f);
                        bool enemyStunned = CalculateStun();
                        if (enemyStunned) { enemyGladMovement.Stun(); }
                        int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < monsterStats.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;
                        }
                        enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                    }


                    else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                    {
                        enemyPetMovement.MovePetToRight(0.5f);
                        bool playerStunned = CalculateStun();
                        if (playerStunned) { enemyPetMovement.Stun(); }
                        int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                        bool isCrit = false;
                        int randomValue = Random.Range(0, 100);
                        if (randomValue < monsterStats.CritRate)
                        {
                            isCrit = true;
                            damage = damage * 2;
                        }
                        enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                    }
                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100);
                if (randomNumber2 > 50)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("hit3");
                    MovePetToRight(0.5f);
                    if (monsterStats.LifeSteal > 0)
                    {
                        petHealthManager.IncreaseHealth(monsterStats.LifeSteal);
                    }
                    // Check if the enemy dodges the fourth hit
                    if (EnemyDodges())
                    {
                        if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
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
                        MoveBackToRandomStart(); // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        if (enemy.tag == "EnemyGlad")
                        {
                            enemyGladMovement.MovePlayerToRight(0.5f);
                            bool enemyStunned = CalculateStun();
                            if (enemyStunned) { enemyGladMovement.Stun(); }
                            int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                            bool isCrit = false;
                            int randomValue = Random.Range(0, 100);
                            if (randomValue < monsterStats.CritRate)
                            {
                                isCrit = true;
                                damage = damage * 2;
                            }
                            enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                        }


                        else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                        {
                            enemyPetMovement.MovePetToRight(0.5f);
                            bool playerStunned = CalculateStun();
                            if (playerStunned) { enemyPetMovement.Stun(); }
                            int damage = CalculateRandomDamage(monsterStats.AttackDamage);
                            bool isCrit = false;
                            int randomValue = Random.Range(0, 100);
                            if (randomValue < monsterStats.CritRate)
                            {
                                isCrit = true;
                                damage = damage * 2;
                            }
                            enemyHealthManager.ReduceHealth(monsterStats.AttackDamage, "Normal", pet, isCrit);
                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && enemy.tag == "EnemyGlad" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
                    else
                    {
                        MoveBackToRandomStart();
                    }
                }
                else
                {
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && enemy.tag == "EnemyGlad" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
                    else
                    {
                        MoveBackToRandomStart();
                    }
                }
            }
            else
            {
                anim.SetTrigger("stophit");
                yield return new WaitForSeconds(0.05f);
                int randomNumberCounter = Random.Range(0, 100);
                if (randomNumberCounter > 50 && enemy.tag == "EnemyGlad" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
                else
                {
                    MoveBackToRandomStart();
                }
            }
        }
        else
        {
            anim.SetTrigger("stophit");
            yield return new WaitForSeconds(0.05f);
            int randomNumberCounter = Random.Range(0, 100);
            if (randomNumberCounter > 50 && enemy.tag == "EnemyGlad" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
            else
            {
                MoveBackToRandomStart();
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
            gameManager.GameOver("Player");
            return false;
        }

        // Rolla en slumpmässig index för att välja fiende
        int randomIndex = Random.Range(0, aliveEnemies.Count);
        GameObject selectedEnemy = aliveEnemies[randomIndex];

        if (selectedEnemy.tag == "EnemyGlad")
        {
            enemy = selectedEnemy;
            enemyFeet = enemy.transform.Find("Feet");
            enemyHealthManager = enemy.GetComponent<ArenaHealthManager>();
            enemyGladMovement = enemy.GetComponent<ArenaEnemyPlayerMovement>();
            targetEnemySet = true;
        }
        else if (selectedEnemy.tag == "EnemyPet1" || selectedEnemy.tag == "EnemyPet2" || selectedEnemy.tag == "EnemyPet3")
        {
            enemy = selectedEnemy;
            enemyFeet = enemy.transform.Find("Feet");
            enemyHealthManager = enemy.GetComponent<ArenaHealthManager>();
            enemyPetMovement = enemy.GetComponent<ArenaEnemyPetMovement>();
            targetEnemySet = true;
        }
        // Om inga fiender finns kvar
        return true;
    }

    private bool EnemyDodges()
    {
        int dodgeChance = monsterStats.DodgeRate;
        int randomValue = Random.Range(0, 100);
        return randomValue < dodgeChance;
    }

    public void MovePetToRight(float distance)
    {
        if (enemy.transform.position.x < screenRight.x - 1f)
        {
            // Get the target position for the pet
            Vector3 targetPosition = transform.position;
            targetPosition.x += distance; // Move to the right by 'distance' units

            // Move the pet towards the enemy's new position
            transform.position = targetPosition;
        }
    }

    public void MovePetToLeft(float distance)
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
        CombatTextManager.Instance.SpawnText("Stunned", pet.transform.position + Vector3.up * 1.5f, "#FFFFFF");
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

    private bool CalculateStun()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < monsterStats.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }

    private int CalculateRandomDamage(int baseDamage)
    {

        // Calculate a random damage between baseDamage - 2 and baseDamage + 2
        int minDamage = baseDamage - 2;
        int maxDamage = baseDamage + 2;

        // Ensure the minimum damage is at least 1
        if (minDamage < 1) { minDamage = 1; }
        int randomDmg = Random.Range(minDamage, maxDamage + 1);
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
            }
        }
    }

    IEnumerator MoveBackToStart(Vector2 targetPosition)
    {
        float tolerance = 0.1f;

        while (Vector2.Distance(transform.position, targetPosition) > tolerance)
        {
            // Ensure the pet is always facing left (flip if necessary) during the movement
            if (transform.position.x > targetPosition.x)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
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

        // Determine dodge direction: dodge up if below center, down if above
        float dodgeDistance = 1f; // Customize dodge distance
        float dodgeDirection = transform.position.y < screenCenterY ? 1f : -1f; // Dodge up if below center, down if above

        // Calculate the dodge target position based on direction and distance
        Vector2 dodgePosition = new Vector2(transform.position.x, transform.position.y + dodgeDirection * dodgeDistance);

        // Move the pet to the dodge position over time
        StartCoroutine(DodgeMove(dodgePosition, dodgeDirection));
    }

    IEnumerator DodgeMove(Vector3 targetPosition, float dodgeDirection)
    {
        // Instantly set the pet's position to the dodge target position
        CombatTextManager.Instance.SpawnText("Dodge", pet.transform.position + Vector3.up * 1.5f, "#FFFFFF");
        transform.position = targetPosition;

        yield return null; // Yield to ensure any other logic can complete if necessary
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
