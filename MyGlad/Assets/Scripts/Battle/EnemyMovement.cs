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
        enemySpeed = 40;
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

            Vector2 new2DPos = Vector2.MoveTowards(enemyFeet.position, targetPosition, enemySpeed * Time.deltaTime);
            Vector3 delta = new Vector3(new2DPos.x - enemyFeet.position.x, new2DPos.y - enemyFeet.position.y, 0);
            transform.position += delta;
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
        // Check if player dodges the first hit
        if (PlayerDodges())
        {
            if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
            if (enemyHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
            else
            {
                anim.SetBool("run", false);
                moveCoroutine = null;
                IsMoving = false;
                gameManager.RollTime = true;
            }
            yield break; // End the coroutine here
        }
        else
        {
            if (!gameManager.IsGameOver)
            {
                if (player.tag == "Player")
                {
                    playerMovement.MovePlayerToLeft(0.5f);
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                    bool playerStunned = CalculateStun();
                    if (playerStunned && !Inventory.Instance.HasSkill("IronWill")) { playerMovement.Stun(); }
                    CalcLifesteal(damage);
                }


                else if (player.tag == "Pet1" || player.tag == "Pet2" || player.tag == "Pet3")
                {
                    petMovement.MovePetToLeft(0.5f);
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                    bool playerStunned = CalculateStun();
                    if (playerStunned) { petMovement.Stun(); }
                    CalcLifesteal(damage);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        int randomNumber = Random.Range(0, 100) + monsterstats.combo;
        if (enemyHealthManager.CurrentHealth <= 0) { randomNumber = 0; }
        if (randomNumber > 60)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MoveEnemyToLeft(0.5f);

            // Check if player dodges the second hit
            if (PlayerDodges())
            {
                if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                anim.SetTrigger("stophit");
                if (enemyHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
                else
                {
                    anim.SetBool("run", false);
                    moveCoroutine = null;
                    IsMoving = false;
                    gameManager.RollTime = true;
                }
                yield break; // End the coroutine here
            }
            else
            {
                if (player.tag == "Player")
                {
                    playerMovement.MovePlayerToLeft(0.5f);
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                    bool playerStunned = CalculateStun();
                    if (playerStunned && !Inventory.Instance.HasSkill("IronWill")) { playerMovement.Stun(); }
                    CalcLifesteal(damage);
                }
                else if (player.tag == "Pet1" || player.tag == "Pet2" || player.tag == "Pet3")
                {
                    petMovement.MovePetToLeft(0.5f);
                    int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                    int randomValue = Random.Range(0, 100);
                    bool isCrit = false;
                    if (randomValue < monsterstats.CritRate)
                    {
                        damage = damage * 2;
                        isCrit = true;
                    }
                    playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                    bool playerStunned = CalculateStun();
                    if (playerStunned) { petMovement.Stun(); }
                    CalcLifesteal(damage);
                }
            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100) + monsterstats.combo;
            if (enemyHealthManager.CurrentHealth <= 0) { randomNumber1 = 0; }
            if (randomNumber1 > 60)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hit2");
                MoveEnemyToLeft(0.5f);

                // Check if player dodges the third hit
                if (PlayerDodges())
                {
                    if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    if (enemyHealthManager.CurrentHealth > 0) { MoveBackToRandomStart(); }
                    else
                    {
                        anim.SetBool("run", false);
                        moveCoroutine = null;
                        IsMoving = false;
                        gameManager.RollTime = true;
                    }
                    yield break; // End the coroutine here
                }
                else
                {
                    if (player.tag == "Player")
                    {
                        playerMovement.MovePlayerToLeft(0.5f);
                        int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                        int randomValue = Random.Range(0, 100);
                        bool isCrit = false;
                        if (randomValue < monsterstats.CritRate)
                        {
                            damage = damage * 2;
                            isCrit = true;
                        }
                        playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                        bool playerStunned = CalculateStun();
                        if (playerStunned && !Inventory.Instance.HasSkill("IronWill")) { playerMovement.Stun(); }
                        CalcLifesteal(damage);
                    }
                    else if (player.tag == "Pet1" || player.tag == "Pet2" || player.tag == "Pet3")
                    {
                        petMovement.MovePetToLeft(0.5f);
                        int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                        int randomValue = Random.Range(0, 100);
                        bool isCrit = false;
                        if (randomValue < monsterstats.CritRate)
                        {
                            damage = damage * 2;
                            isCrit = true;
                        }
                        playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                        bool playerStunned = CalculateStun();
                        if (playerStunned) { petMovement.Stun(); }
                        CalcLifesteal(damage);
                    }
                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100) + monsterstats.combo;
                if (enemyHealthManager.CurrentHealth <= 0) { randomNumber2 = 0; }
                if (randomNumber2 > 60)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("hit3");
                    MoveEnemyToLeft(0.5f);

                    // Check if player dodges the fourth hit
                    if (PlayerDodges())
                    {
                        if (player.tag == "Player") { playerMovement.Dodge(); } else { petMovement.Dodge(); }
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
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
                        } // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        if (player.tag == "Player")
                        {
                            playerMovement.MovePlayerToLeft(0.5f);
                            int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                            int randomValue = Random.Range(0, 100);
                            bool isCrit = false;
                            if (randomValue < monsterstats.CritRate)
                            {
                                damage = damage * 2;
                                isCrit = true;
                            }
                            playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                            bool playerStunned = CalculateStun();
                            if (playerStunned && !Inventory.Instance.HasSkill("IronWill")) { playerMovement.Stun(); }
                            CalcLifesteal(damage);
                        }
                        else if (player.tag == "Pet1" || player.tag == "Pet2" || player.tag == "Pet3")
                        {
                            petMovement.MovePetToLeft(0.5f);
                            int damage = CalculateRandomDamage(monsterstats.AttackDamage);
                            int randomValue = Random.Range(0, 100);
                            bool isCrit = false;
                            if (randomValue < monsterstats.CritRate)
                            {
                                damage = damage * 2;
                                isCrit = true;
                            }
                            playerHealthManager.ReduceHealth(damage, "Normal", enemy, isCrit);
                            bool playerStunned = CalculateStun();
                            if (playerStunned) { petMovement.Stun(); }
                            CalcLifesteal(damage);
                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !playerMovement.IsStunned)
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
                    else if (enemyHealthManager.CurrentHealth > 0)
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
                    if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !playerMovement.IsStunned)
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
                    else if (enemyHealthManager.CurrentHealth > 0)
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
                }
            }
            else
            {
                anim.SetTrigger("stophit");
                yield return new WaitForSeconds(0.05f);
                int randomNumberCounter = Random.Range(0, 100);
                if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !playerMovement.IsStunned)
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
                else if (enemyHealthManager.CurrentHealth > 0)
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
            }
        }
        else
        {
            anim.SetTrigger("stophit");
            yield return new WaitForSeconds(0.05f);
            int randomNumberCounter = Random.Range(0, 100);
            if (randomNumberCounter > 50 && player.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver && !playerMovement.IsStunned)
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
            else if (enemyHealthManager.CurrentHealth > 0)
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
    private void CalcLifesteal(int damage)
    {
        int baseLifeSteal = monsterstats.LifeSteal;

        if (baseLifeSteal > 0)
        {
            float lifeStealMultiplier = baseLifeSteal / 100f;

            int vampBonus = Mathf.RoundToInt(damage * lifeStealMultiplier);
            if (vampBonus < 1)
            {
                vampBonus = 1;
            }

            enemyHealthManager.IncreaseHealth(vampBonus);
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
        if (player.tag == "Player")
        {
            randomDmg = randomDmg - CharacterData.Instance.Defense;
            if (randomDmg < 1)
            {
                randomDmg = 1;
            }
        }
        else
        {
            MonsterStats petstats = player.GetComponent<MonsterStats>();
            randomDmg = randomDmg - petstats.defense;
            if (randomDmg < 1)
            {
                randomDmg = 1;
            }
        }
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

    IEnumerator MoveBackToStart(Vector3 targetPosition)
    {
        float tolerance = 0.1f;

        while (Vector2.Distance(enemy.transform.position, targetPosition) > tolerance)
        {
            // Flip based on direction
            FlipTowards(targetPosition);

            anim.SetBool("run", true);
            Vector2 new2DPos = Vector2.MoveTowards(enemy.transform.position, targetPosition, enemySpeed * Time.deltaTime);
            enemy.transform.position = new Vector3(new2DPos.x, new2DPos.y, enemy.transform.position.z);
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
        HealthManager arenaHealthManager = enemy.GetComponent<HealthManager>();
        if (!arenaHealthManager.IsDead)
        {
            if (player.tag == "Player")
            {
                if (playerMovement.IsStunned)
                {
                    return false;
                }
                else
                {
                    int dodgeChance = CharacterData.Instance.DodgeRate; // Assume dodge chance is 30%
                    int randomValue = Random.Range(0, 100);
                    dodgeChance = dodgeChance - monsterstats.hitRate;
                    return randomValue < dodgeChance;
                }
            }
            else
            {
                if (petMovement.IsStunned)
                {
                    return false;
                }
                else
                {
                    MonsterStats petstats = player.GetComponent<MonsterStats>();
                    int dodgeChance = petstats.DodgeRate; // Assume dodge chance is 30%
                    int randomValue = Random.Range(0, 100);
                    dodgeChance = dodgeChance - monsterstats.hitRate;
                    return randomValue < dodgeChance;
                }
            }
        }
        else return false;
    }

    public void Dodge()
    {
        float screenBottomY = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).y;
        float screenTopY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, Camera.main.nearClipPlane)).y;
        float screenCenterY = (screenBottomY + screenTopY) / 2;

        float dodgeDistance = 1f;
        float dodgeDirection = transform.position.y < screenCenterY ? 1f : -1f;

        // ✅ Bevara Z-positionen!
        Vector3 dodgePosition = new Vector3(transform.position.x, transform.position.y + dodgeDirection * dodgeDistance, transform.position.z);

        StartCoroutine(DodgeMove(dodgePosition));
    }

    IEnumerator DodgeMove(Vector3 targetPosition)
    {

        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }
}