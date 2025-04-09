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
        playerSpeed = 30;
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

        if (EnemyGladiatorData.Instance.LifeSteal > 0)
        {
            enemyHealthManager.IncreaseHealth(EnemyGladiatorData.Instance.LifeSteal);
        }
        // Check if the enemy dodges the first hit
        if (EnemyDodges())
        {
            if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
            MoveBackToRandomStart(); // Stop combo and move back to start
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
                    if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                    {
                        ApplyVenom();
                    }
                    enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                    RollForDestroyWeapon();

                }
                else
                {
                    enemyPetMovement.MovePetToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                    {
                        ApplyVenom();
                    }
                    enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                    RollForDestroyWeapon();
                }
            }
        }
        yield return new WaitForSeconds(0.5f);

        int randomNumber = Random.Range(0, 100);
        if (randomNumber > 50)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePlayerToLeft(0.5f);
            if (EnemyGladiatorData.Instance.LifeSteal > 0)
            {
                enemyHealthManager.IncreaseHealth(EnemyGladiatorData.Instance.LifeSteal);
            }
            // Check if the enemy dodges the second hit
            if (EnemyDodges())
            {
                if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                anim.SetTrigger("stophit");
                MoveBackToRandomStart(); // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                if (enemy.tag == "Player")
                {
                    enemyPlayerMovement.MovePlayerToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyPlayerMovement.Stun(); }
                    if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                    {
                        ApplyVenom();
                    }
                    enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                    RollForDestroyWeapon();
                }
                else
                {
                    enemyPetMovement.MovePetToLeft(0.5f);

                    bool enemyStunned = CalculateStun();
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                    {
                        ApplyVenom();
                    }
                    enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                    RollForDestroyWeapon();

                }
            }

            yield return new WaitForSeconds(0.5f);
            int randomNumber1 = Random.Range(0, 100);
            if (randomNumber1 > 50)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hook");
                MovePlayerToLeft(0.5f);
                if (EnemyGladiatorData.Instance.LifeSteal > 0)
                {
                    enemyHealthManager.IncreaseHealth(EnemyGladiatorData.Instance.LifeSteal);
                }
                // Check if the enemy dodges the third hit
                if (EnemyDodges())
                {
                    if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart(); // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    if (enemy.tag == "Player")
                    {
                        enemyPlayerMovement.MovePlayerToLeft(0.5f);

                        bool enemyStunned = CalculateStun();
                        if (enemyStunned) { enemyPlayerMovement.Stun(); }
                        if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                        {
                            ApplyVenom();
                        }
                        enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                        RollForDestroyWeapon();

                    }
                    else
                    {
                        enemyPetMovement.MovePetToLeft(0.5f);

                        bool enemyStunned = CalculateStun();
                        if (enemyStunned) { enemyPetMovement.Stun(); }
                        if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                        {
                            ApplyVenom();
                        }
                        enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                        RollForDestroyWeapon();

                    }
                }

                yield return new WaitForSeconds(0.5f);
                int randomNumber2 = Random.Range(0, 100);
                if (randomNumber2 > 50)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("uppercut");
                    MovePlayerToLeft(0.5f);
                    if (EnemyGladiatorData.Instance.LifeSteal > 0)
                    {
                        enemyHealthManager.IncreaseHealth(EnemyGladiatorData.Instance.LifeSteal);
                    }
                    // Check if the enemy dodges the fourth hit
                    if (EnemyDodges())
                    {
                        if (enemy.tag == "Player") { enemyPlayerMovement.Dodge(); } else { enemyPetMovement.Dodge(); }
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
                        MoveBackToRandomStart(); // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        if (enemy.tag == "Player")
                        {
                            enemyPlayerMovement.MovePlayerToLeft(0.5f);

                            bool enemyStunned = CalculateStun();
                            if (enemyStunned) { enemyPlayerMovement.Stun(); }
                            if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                            {
                                ApplyVenom();
                            }
                            enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                            RollForDestroyWeapon();

                        }
                        else
                        {
                            enemyPetMovement.MovePetToLeft(0.5f);

                            bool enemyStunned = CalculateStun();
                            if (enemyStunned) { enemyPetMovement.Stun(); }
                            if (EnemyInventory.Instance.HasSkill("VenomousTouch"))
                            {
                                ApplyVenom();
                            }
                            enemyHealthManager.ReduceHealth(CalculateRandomDamage(EnemyGladiatorData.Instance.AttackDamage + berserkDamage), "Normal", player);
                            RollForDestroyWeapon();

                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    int randomNumberCounter = Random.Range(0, 100);
                    if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
                    if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
                if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
            if (randomNumberCounter > 50 && enemy.tag == "Player" && Inventory.Instance.HasSkill("CounterStrike") && !gameManager.IsGameOver)
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
            else
            {
                MoveBackToRandomStart();
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

    private bool EnemyDodges()
    {
        ArenaHealthManager arenaHealthManager = enemy.GetComponent<ArenaHealthManager>();
        if (!arenaHealthManager.IsDead)
        {
            if (enemy.tag == "Player")
            {
                int dodgeChance = EnemyGladiatorData.Instance.DodgeRate; // Assume dodge chance is 30%
                int randomValue = Random.Range(0, 100);
                return randomValue < dodgeChance;
            }
            else
            {
                MonsterStats enemyPetstats = enemy.GetComponent<MonsterStats>();
                int dodgeChance = enemyPetstats.DodgeRate; // Assume dodge chance is 30%
                int randomValue = Random.Range(0, 100);
                return randomValue < dodgeChance;
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
        playerHealthManager.ShowCombatText(0, "Stunned");
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

    private bool CalculateStun()
    {
        int randomValue = Random.Range(0, 100);
        if (randomValue < EnemyGladiatorData.Instance.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }

    private void ApplyVenom()
    {
        enemyHealthManager.ApplyVenom();
    }

    public int CalculateRandomDamage(int baseDamage)
    {
        int outDmg;
        // Calculate a random damage between baseDamage - 2 and baseDamage + 2
        int minDamage = baseDamage - 2;
        int maxDamage = baseDamage + 2;

        // Ensure the minimum damage is at least 1
        if (minDamage < 1) { minDamage = 1; }
        int randomDmg = Random.Range(minDamage, maxDamage + 1);
        int randomValue = Random.Range(0, 100);
        if (randomValue < EnemyGladiatorData.Instance.CritRate)
        {
            outDmg = randomDmg * 2;
            playerHealthManager.ShowCombatText(0, "Critical Strike");
        }
        else { outDmg = randomDmg; }
        return outDmg; // Random.Range is inclusive for integers
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
                if (berserkDamage > 0)
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
        Vector2 dodgePosition = new Vector2(transform.position.x, transform.position.y + dodgeDirection * dodgeDistance);

        // Move the player to the dodge position over time
        StartCoroutine(DodgeMove(dodgePosition, dodgeDirection));
    }

    IEnumerator DodgeMove(Vector3 targetPosition, float dodgeDirection)
    {

        // Instantly set the player's position to the dodge target position
        playerHealthManager.ShowCombatText(0, "Dodge!");
        transform.position = targetPosition;

        yield return null; // Yield to ensure any other logic can complete if necessary
    }
}

