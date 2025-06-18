using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class ReplayPetMovement : MonoBehaviour
{
    private ReplayGameManager gameManager;
    private Animator anim;
    private GameObject pet;
    [SerializeField] private Transform petFeet;
    private ReplayHealthManager petHealthManager;

    [SerializeField] private int petSpeed;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private GameObject enemy;
    private Transform enemyFeet;
    private ReplayHealthManager enemyHealthManager;
    private ReplayEnemyPetMovement enemyPetMovement;
    private ReplayEnemyPlayerMovement enemyGladMovement;
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
        gameManager = FindObjectOfType<ReplayGameManager>();
        pet = gameObject;
        foreach (GameObject enemy in gameManager.GetEnemies())
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
        petHealthManager = pet.GetComponent<ReplayHealthManager>();
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
        // Check if the enemy dodges the first hit
        if (EnemyDodges(0))
        {
            if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
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

                    // Hitta första attacken eller crit från detta pet
                    var attack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 1); // eller Pet2/Pet3 beroende på vilket pet
                    if (attack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = attack.Action == "crit";
                    int damage = attack.Value;

                    // Stun efter attack
                    bool enemyStunned = CalculateStun(0);
                    if (enemyStunned) { enemyGladMovement.Stun(); }

                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                    CalcLifesteal(damage);
                }



                else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                {
                    enemyPetMovement.MovePetToRight(0.5f);

                    // Hitta första attacken eller crit från detta pet
                    var attack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 1); // eller Pet2/Pet3 beroende på vilket pet
                    if (attack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = attack.Action == "crit";
                    int damage = attack.Value;
                    bool enemyStunned = CalculateStun(0);
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                    CalcLifesteal(damage);
                }
            }

        }

        yield return new WaitForSeconds(0.5f);

        var secondAttackControll2 = GetNthAttackThisTurn(GetCharacterType(pet.tag), 2);
        if (secondAttackControll2 != null)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePetToRight(0.5f);
            // Check if the enemy dodges the second hit
            if (EnemyDodges(1))
            {
                if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                MoveBackToRandomStart(); // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                if (enemy.tag == "EnemyGlad")
                {
                    enemyGladMovement.MovePlayerToRight(0.5f);

                    var secondAttack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 2); // eller Pet2/Pet3 beroende på vilket pet
                    if (secondAttack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = secondAttack.Action == "crit";
                    int damage = secondAttack.Value;

                    bool enemyStunned = CalculateStun(1);
                    if (enemyStunned)
                    {
                        enemyGladMovement.Stun();
                    }

                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                    CalcLifesteal(damage);
                }


                else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                {
                    enemyPetMovement.MovePetToRight(0.5f);
                    var secondAttack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 2); // eller Pet2/Pet3 beroende på vilket pet
                    if (secondAttack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = secondAttack.Action == "crit";
                    int damage = secondAttack.Value;

                    bool enemyStunned = CalculateStun(1);
                    if (enemyStunned)
                    {
                        enemyPetMovement.Stun();
                    }

                    enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                    CalcLifesteal(damage);
                }
            }

            yield return new WaitForSeconds(0.5f);
            var secondAttackControll3 = GetNthAttackThisTurn(GetCharacterType(pet.tag), 3);
            if (secondAttackControll3 != null)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hit2");
                MovePetToRight(0.5f);
                // Check if the enemy dodges the third hit
                if (EnemyDodges(2))
                {
                    if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart(); // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    if (enemy.tag == "EnemyGlad")
                    {
                        enemyGladMovement.MovePlayerToRight(0.5f);

                        var secondAttack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 3); // eller Pet2/Pet3 beroende på vilket pet
                        if (secondAttack == null)
                        {
                            Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                        }

                        bool isCrit = secondAttack.Action == "crit";
                        int damage = secondAttack.Value;

                        bool enemyStunned = CalculateStun(2);
                        if (enemyStunned)
                        {
                            enemyGladMovement.Stun();
                        }

                        enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                        CalcLifesteal(damage);
                    }


                    else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                    {
                        enemyPetMovement.MovePetToRight(0.5f);
                        var secondAttack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 3); // eller Pet2/Pet3 beroende på vilket pet
                        if (secondAttack == null)
                        {
                            Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                        }

                        bool isCrit = secondAttack.Action == "crit";
                        int damage = secondAttack.Value;

                        bool enemyStunned = CalculateStun(2);
                        if (enemyStunned)
                        {
                            enemyPetMovement.Stun();
                        }

                        enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                        CalcLifesteal(damage);
                    }
                }

                yield return new WaitForSeconds(0.5f);
                var secondAttackControll4 = GetNthAttackThisTurn(GetCharacterType(pet.tag), 4);
                if (secondAttackControll4 != null)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("hit3");
                    MovePetToRight(0.5f);
                    // Check if the enemy dodges the fourth hit
                    if (EnemyDodges(3))
                    {
                        if (enemy.tag == "EnemyGlad") { enemyGladMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
                        MoveBackToRandomStart(); // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        if (enemy.tag == "EnemyGlad")
                        {
                            enemyGladMovement.MovePlayerToRight(0.5f);

                            var secondAttack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 4); // eller Pet2/Pet3 beroende på vilket pet
                            if (secondAttack == null)
                            {
                                Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                            }

                            bool isCrit = secondAttack.Action == "crit";
                            int damage = secondAttack.Value;

                            bool enemyStunned = CalculateStun(3);
                            if (enemyStunned)
                            {
                                enemyGladMovement.Stun();
                            }

                            enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                            CalcLifesteal(damage);
                        }


                        else if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                        {
                            enemyPetMovement.MovePetToRight(0.5f);
                            var secondAttack = GetNthAttackThisTurn(GetCharacterType(pet.tag), 4); // eller Pet2/Pet3 beroende på vilket pet
                            if (secondAttack == null)
                            {
                                Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                            }

                            bool isCrit = secondAttack.Action == "crit";
                            int damage = secondAttack.Value;

                            bool enemyStunned = CalculateStun(3);
                            if (enemyStunned)
                            {
                                enemyPetMovement.Stun();
                            }

                            enemyHealthManager.ReduceHealth(damage, "Normal", pet, isCrit);
                            CalcLifesteal(damage);
                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    if (IsCounterAttackThisTurn())
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ReplayCounterStrike(pet, () =>
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
                    if (IsCounterAttackThisTurn())
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ReplayCounterStrike(pet, () =>
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
                if (IsCounterAttackThisTurn())
                {
                    SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                    // Call CounterStrike and pass MoveBackToRandomStart as the callback
                    playerSkills.ReplayCounterStrike(pet, () =>
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
            if (IsCounterAttackThisTurn())
            {
                SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                // Call CounterStrike and pass MoveBackToRandomStart as the callback
                playerSkills.ReplayCounterStrike(pet, () =>
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

    public bool RollForEnemy()
    {
        int currentTurn = gameManager.RoundsCount;

        var actionsThisTurn = ReplayManager.Instance.selectedReplay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

        // Hitta första attack från detta husdjur denna rundan
        var petAttack = actionsThisTurn
            .FirstOrDefault(a => a.Actor == GetCharacterType(pet.tag) && a.Action == "attack" || a.Action == "crit");

        CharacterType targetType;

        if (petAttack != null)
        {
            targetType = petAttack.Target;
        }
        else
        {
            // Om ingen attack eller crit hittas, leta efter dodge där Player är target
            var dodge = actionsThisTurn
                .FirstOrDefault(a => a.Target == GetCharacterType(pet.tag) && a.Action == "dodge");

            if (dodge == null)
            {
                Debug.LogWarning("❌ Kunde inte hitta varken attack eller dodge där Player var involverad.");
                return false;
            }

            // Dodge.actor = den som dodgar → den spelaren försökte attackera
            targetType = dodge.Actor;
        }

        GameObject selectedEnemy = availableEnemies.FirstOrDefault(e =>
        e.CompareTag(targetType.ToString()));

        if (selectedEnemy == null)
        {
            Debug.LogWarning("❌ Kunde inte hitta fiende med tagg: " + targetType);
            return false;
        }

        enemy = selectedEnemy;
        enemyFeet = enemy.transform.Find("Feet");
        enemyHealthManager = enemy.GetComponent<ReplayHealthManager>();

        if (enemy.tag == "EnemyGlad")
        {
            enemyGladMovement = enemy.GetComponent<ReplayEnemyPlayerMovement>();
        }
        else if (enemy.tag.StartsWith("EnemyPet"))
        {
            enemyPetMovement = enemy.GetComponent<ReplayEnemyPetMovement>();
        }

        targetEnemySet = true;
        return true;
    }


    private bool EnemyDodges(int index)
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = gameManager.RoundsCount;

        var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn &&
                        (a.Action == "attack" || a.Action == "crit" || a.Action == "dodge"))
            .ToList();

        if (index < 0 || index >= actionsThisTurn.Count)
        {
            return false;
        }

        return actionsThisTurn[index].Action == "dodge";
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

    private bool CalculateStun(int attackIndex)
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = ReplayGameManager.Instance.RoundsCount;

        // Alla actions i ordning för denna turn
        var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

        // Hämta attacker från rätt attackerare
        var attackerActions = actionsThisTurn
            .Where(a => (a.Action == "attack" || a.Action == "crit") && a.Actor == GetCharacterType(pet.tag))
            .ToList();

        if (attackIndex >= attackerActions.Count)
            return false;

        var targetAttack = attackerActions[attackIndex];
        int targetIndex = actionsThisTurn.IndexOf(targetAttack);

        // Leta upp stunnen precis före denna attack
        if (targetIndex > 0)
        {
            var previous = actionsThisTurn[targetIndex - 1];

            // Stun måste ske precis före attacken, och Actor i stun = den som blir stunnad
            if (previous.Action == "stunned")
            {
                // Bekräfta att stun hör till denna attack
                return true;
            }
        }

        return false;
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

    private MatchEventDTO GetNthAttackThisTurn(CharacterType actorType, int attackIndex)
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = ReplayGameManager.Instance.RoundsCount;

        var attacks = replay.actions
            .Where(a => a.Turn == currentTurn &&
                        a.Actor == actorType &&
                        (a.Action == "attack" || a.Action == "crit"))
            .ToList();

        if (attacks.Count >= attackIndex)
        {
            return attacks[attackIndex - 1]; // attackIndex = 1 ger [0], 2 ger [1] osv.
        }

        return null;
    }

    private bool IsCounterAttackThisTurn()
    {
        if (GetCharacterType(enemy.tag) == CharacterType.EnemyGlad)
        {
            var replay = ReplayManager.Instance.selectedReplay;
            int currentTurn = ReplayGameManager.Instance.RoundsCount;

            var actionsThisTurn = replay.actions
                .Where(a => a.Turn == currentTurn)
                .ToList();

            if (actionsThisTurn.Count < 2)
                return false;

            var firstActor = actionsThisTurn.First().Actor;
            var lastAction = actionsThisTurn.Last();

            // Counterattack sker om sista actionen är en attack av någon annan än den som började rundan
            return lastAction.Action == "attack" && lastAction.Actor != firstActor;
        }
        else { return false; }
    }
}
