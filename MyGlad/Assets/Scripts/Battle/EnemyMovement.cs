using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private GameManager gameManager;
    private Animator anim;
    private GameObject enemy;
    [SerializeField] private Transform enemyFeet;
    private HealthManager enemyHealthManager;
    private MonsterStats monsterstats;

    [SerializeField] private int enemySpeed;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private GameObject player;
    private Transform playerFeet;
    private HealthManager playerHealthManager;
    private PlayerMovement playerMovement;
    private PetMovement petMovement;
    private Transform visualTransform;

    public int positionIndex;
    private bool isMoving;
    private bool isStunned;
    private int stunnedAtRound;
    private Coroutine moveCoroutine;

    Vector3 screenLeft;
    Vector3 screenRight;

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
        gameManager = FindObjectOfType<GameManager>();
        enemy = gameObject;

        foreach (GameObject player in gameManager.GetPlayerAndPets())
        {
            availableEnemies.Add(player);
        }

        enemyFeet = enemy.transform.Find("Feet");

        // Hämta visualTransform (för flip och animation)
        visualTransform = transform.Find("Visual");
        if (visualTransform == null)
        {
            visualTransform = transform; // fallback om "Visual" inte finns
        }

        // Hämta Animator från rätt plats
        anim = visualTransform.GetComponent<Animator>();

        monsterstats = GetComponent<MonsterStats>();
        enemySpeed = 30;
        enemyHealthManager = GetComponent<HealthManager>();

        screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane));
    }

    void FixedUpdate()
    {
        if (IsMoving && enemy.transform != null && enemyFeet != null)
        {
            if (!valid)
            {
                valid = RollForEnemy();
            }
            if (targetEnemySet)
            {
                float distanceToPlayer = Vector2.Distance(enemyFeet.position, playerFeet.position);
                float stoppingDistance = 0.5f; // Adjust this value as needed to stop before collision

                if (distanceToPlayer > stoppingDistance)
                {
                    // Start moving towards the player's feet (enemy attacks by moving left)
                    if (moveCoroutine == null)
                    {
                        moveCoroutine = StartCoroutine(MoveTowards(playerFeet.position, stoppingDistance));
                    }
                }
            }
        }
    }

    IEnumerator MoveTowards(Vector2 targetPosition, float stoppingDistance)
    {
        while (Vector2.Distance(enemyFeet.position, targetPosition) > stoppingDistance)
        {
            anim.SetBool("run", true);

            // Flip visual based on movement direction
            FlipTowards(targetPosition);

            Vector2 newPosition = Vector2.MoveTowards(enemyFeet.position, targetPosition, enemySpeed * Time.deltaTime);
            transform.position += (Vector3)(newPosition - (Vector2)enemyFeet.position);
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
        MoveEnemyToLeft(0.5f);
        if (monsterstats.LifeSteal > 0)
        {
            enemyHealthManager.IncreaseHealth(monsterstats.LifeSteal);
        }
        // Check if player dodges the first hit
        if (PlayerDodges())
        {
            if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
            MoveBackToRandomStart(); // Stop combo and move back to start
            yield break; // End the coroutine here
        }
        else
        {
            if (!gameManager.IsGameOver)
            {
                if (player.tag == "Player")
                {
                    playerMovement.MovePlayerToLeft(0.5f);
                    bool playerStunned = CalculateStun();
                    if (playerStunned) { playerMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                }


                else if (player.tag == "Pet1" || player.tag == "Pet2" || player.tag == "Pet3")
                {
                    petMovement.MovePetToLeft(0.5f);
                    bool playerStunned = CalculateStun();
                    if (playerStunned) { petMovement.Stun(); }
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        int randomNumber = Random.Range(0, 100);
        if (randomNumber > 50)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MoveEnemyToLeft(0.5f);
            if (monsterstats.LifeSteal > 0)
            {
                enemyHealthManager.IncreaseHealth(monsterstats.LifeSteal);
            }
            // Check if player dodges the second hit
            if (PlayerDodges())
            {
                if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                anim.SetTrigger("stophit");
                MoveBackToRandomStart(); // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                if (player.tag == "Player") { playerMovement.MovePlayerToLeft(0.5f); } else { petMovement.MovePetToLeft(0.5f); }
                int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                int randomValue = Random.Range(0, 100);
                bool isCrit = false;
                if (randomValue < monsterstats.CritRate)
                {
                    damage = damage * 2;
                    isCrit = true;
                }
                playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100);
            if (randomNumber1 > 50)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hit2");
                MoveEnemyToLeft(0.5f);
                if (monsterstats.LifeSteal > 0)
                {
                    enemyHealthManager.IncreaseHealth(monsterstats.LifeSteal);
                }
                // Check if player dodges the third hit
                if (PlayerDodges())
                {
                    if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart(); // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    if (player.tag == "Player") { playerMovement.MovePlayerToLeft(0.5f); } else { petMovement.MovePetToLeft(0.5f); }
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100);
                if (randomNumber2 > 50)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("hit3");
                    MoveEnemyToLeft(0.5f);
                    if (monsterstats.LifeSteal > 0)
                    {
                        enemyHealthManager.IncreaseHealth(monsterstats.LifeSteal);
                    }
                    // Check if player dodges the fourth hit
                    if (PlayerDodges())
                    {
                        if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
                        MoveBackToRandomStart(); // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        if (player.tag == "Player") { playerMovement.MovePlayerToLeft(0.5f); } else { petMovement.MovePetToLeft(0.5f); }
                        int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                        int randomValue = Random.Range(0, 100);
                        bool isCrit = false;
                        if (randomValue < monsterstats.CritRate)
                        {
                            damage = damage * 2;
                            isCrit = true;
                        }
                        playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
                    {
                        SkillBattleHandler playerSkills = player.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.CounterStrike(enemy, () =>
                        {
                            if (enemyHealthManager.CurrentHealth > 0)
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
                    else { MoveBackToRandomStart(); }

                }
                else
                {
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
                    {
                        SkillBattleHandler playerSkills = player.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.CounterStrike(enemy, () =>
                        {
                            if (enemyHealthManager.CurrentHealth > 0)
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
                if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
                {
                    SkillBattleHandler playerSkills = player.GetComponent<SkillBattleHandler>();

                    // Call CounterStrike and pass MoveBackToRandomStart as the callback
                    playerSkills.CounterStrike(enemy, () =>
                    {
                        if (enemyHealthManager.CurrentHealth > 0)
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
            if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
            {
                SkillBattleHandler playerSkills = player.GetComponent<SkillBattleHandler>();

                // Call CounterStrike and pass MoveBackToRandomStart as the callback
                playerSkills.CounterStrike(enemy, () =>
                {
                    if (enemyHealthManager.CurrentHealth > 0)
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
            gameManager.GameOver("Enemy1");
            return false;
        }

        // Roll a random index for selecting a target from alive enemies
        int randomIndex = Random.Range(0, aliveEnemies.Count);
        GameObject selectedEnemy = aliveEnemies[randomIndex];

        // Now proceed with setting the target as player or pet
        if (selectedEnemy.tag == "Player")
        {
            player = selectedEnemy;
            playerFeet = player.transform.Find("Feet");
            playerHealthManager = player.GetComponent<HealthManager>();
            playerMovement = player.GetComponent<PlayerMovement>();
            targetEnemySet = true;
        }
        else if (selectedEnemy.tag == "Pet1" || selectedEnemy.tag == "Pet2" || selectedEnemy.tag == "Pet3")
        {
            player = selectedEnemy;
            playerFeet = player.transform.Find("Feet");
            playerHealthManager = player.GetComponent<HealthManager>();
            petMovement = player.GetComponent<PetMovement>();
            targetEnemySet = true;
        }

        return true;
    }

    void MoveEnemyToLeft(float distance)
    {
        if (player.transform.position.x > screenLeft.x + 2f)
        {
            Vector3 newPosition = transform.position;
            newPosition.x -= distance; // Move to the right by 'distance' units
            transform.position = newPosition;
        }
    }

    public void MoveEnemyToRight(float distance)
    {
        anim.SetTrigger("takedamage");

        if (transform.position.x < screenRight.x - 2f)
        {
            Vector3 newPosition = transform.position;
            newPosition.x += distance; // Move to the right by 'distance' units
            transform.position = newPosition;
        }
    }
    private bool CalculateStun()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < monsterstats.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }
    public void Stun()
    {
        CombatTextManager.Instance.SpawnText("Stunned", enemy.transform.position + Vector3.up * 1.5f, "#FFFFFF");
        IsStunned = true;
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

    private int CalculateRandomDamage(int baseDamage)
    {
        // Calculate a random damage between baseDamage - 2 and baseDamage + 2
        int minDamage = Mathf.RoundToInt(baseDamage * 0.9f);
        int maxDamage = Mathf.RoundToInt(baseDamage * 1.1f);

        // Ensure the minimum damage is at least 1
        if (minDamage < 1) { minDamage = 1; }
        int randomDmg = Random.Range(minDamage, maxDamage);
        return randomDmg; // Random.Range is inclusive for integers
    }
    private void FlipTowards(Vector2 targetPosition)
    {
        float direction = targetPosition.x - transform.position.x;

        if (direction > 0.01f)
        {
            visualTransform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        else if (direction < -0.01f)
        {
            visualTransform.localRotation = Quaternion.Euler(0, 0, 0);
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

        while (Vector2.Distance(enemy.transform.position, targetPosition) > tolerance)
        {
            // Flip based on direction
            FlipTowards(targetPosition);

            anim.SetBool("run", true);
            enemy.transform.position = Vector2.MoveTowards(enemy.transform.position, targetPosition, enemySpeed * Time.deltaTime);
            yield return null;
        }
        visualTransform.localRotation = Quaternion.Euler(0, 0, 0);
        anim.SetBool("run", false);
        moveCoroutine = null;
        isMoving = false;
        gameManager.RollTime = true;
    }

    // Method to make player dodge
    private bool PlayerDodges()
    {
        if (player.tag == "Player")
        {
            int dodgeChance = CharacterData.Instance.DodgeRate; // Assume dodge chance is 30%
            int randomValue = Random.Range(0, 100);
            return randomValue < dodgeChance;
        }
        else
        {
            MonsterStats petstats = player.GetComponent<MonsterStats>();
            int dodgeChance = petstats.DodgeRate; // Assume dodge chance is 30%
            int randomValue = Random.Range(0, 100);
            return randomValue < dodgeChance;
        }
    }

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

        // Move the enemy to the dodge position over time
        StartCoroutine(DodgeMove(dodgePosition, dodgeDirection));
    }

    IEnumerator DodgeMove(Vector3 targetPosition, float dodgeDirection)
    {
        CombatTextManager.Instance.SpawnText("Dodge", enemy.transform.position + Vector3.up * 1.5f, "#FFFFFF");
        // Instantly set the player's position to the dodge target position
        transform.position = targetPosition;

        yield return null; // Yield to ensure any other logic can complete if necessary
    }
}