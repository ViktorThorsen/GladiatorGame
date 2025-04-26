using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetMovement : MonoBehaviour
{
    private GameManager gameManager;
    private Animator anim;
    private GameObject pet;
    [SerializeField] private Transform petFeet;
    private HealthManager petHealthManager;

    [SerializeField] private int petSpeed;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private GameObject enemy;
    private Transform enemyFeet;
    private HealthManager enemyHealthManager;
    private EnemyMovement enemyMovement;
    private MonsterStats enemyStats;
    private MonsterStats monsterStats;
    private bool isMoving;
    private bool isStunned;
    private int stunnedAtRound;
    private Coroutine moveCoroutine;
    Vector3 screenLeft;
    Vector3 screenRight;
    private Transform visualTransform;
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
        gameManager = FindObjectOfType<GameManager>();
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
        petSpeed = 30;
        petHealthManager = pet.GetComponent<HealthManager>();
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
        FlipTowards(targetPosition);

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
            enemyMovement.Dodge();
            yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
            anim.SetTrigger("stophit");
            MoveBackToRandomStart(); // Stop combo and move back to start
            yield break; // End the coroutine here
        }
        else
        {
            enemyMovement.MoveEnemyToRight(0.5f);
            if (!gameManager.IsGameOver)
            {
                bool enemyStunned = CalculateStun();
                if (enemyStunned) { enemyMovement.Stun(); }
                enemyHealthManager.ReduceHealth(CalculateRandomDamage(monsterStats.AttackDamage), "Normal", pet);

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
                enemyMovement.Dodge();
                yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                anim.SetTrigger("stophit");
                MoveBackToRandomStart(); // Stop combo and move back to start
                yield break; // End the coroutine here
            }
            else
            {
                enemyMovement.MoveEnemyToRight(0.5f);
                if (!gameManager.IsGameOver)
                {
                    enemyHealthManager.ReduceHealth(CalculateRandomDamage(monsterStats.AttackDamage), "Normal", pet);
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
                    enemyMovement.Dodge();
                    yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                    anim.SetTrigger("stophit");
                    MoveBackToRandomStart(); // Stop combo and move back to start
                    yield break; // End the coroutine here
                }
                else
                {
                    enemyMovement.MoveEnemyToRight(0.5f);
                    enemyHealthManager.ReduceHealth(CalculateRandomDamage(monsterStats.AttackDamage), "Normal", pet);

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
                        enemyMovement.Dodge();
                        yield return new WaitForSeconds(0.5f); // Wait for dodge animation to complete
                        anim.SetTrigger("stophit");
                        MoveBackToRandomStart(); // Stop combo and move back to start
                        yield break; // End the coroutine here
                    }
                    else
                    {
                        enemyMovement.MoveEnemyToRight(0.5f);
                        enemyHealthManager.ReduceHealth(CalculateRandomDamage(monsterStats.AttackDamage), "Normal", pet);

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

    private void FlipTowards(Vector2 targetPosition)
    {
        float direction = targetPosition.x - transform.position.x;

        if (direction > 0.01f)
        {
            visualTransform.localRotation = Quaternion.Euler(0, 0, 0); // höger
        }
        else if (direction < -0.01f)
        {
            visualTransform.localRotation = Quaternion.Euler(0, 180, 0); // vänster
        }
    }

    bool RollForEnemy()
    {
        // Kontrollera om det finns några fiender i listan
        if (availableEnemies.Count > 0)
        {
            // Kontrollera om alla fiender är döda
            bool allEnemiesDead = true;

            foreach (GameObject enemy in availableEnemies)
            {
                HealthManager HM = enemy.GetComponent<HealthManager>();
                if (!HM.IsDead)
                {

                    allEnemiesDead = false;
                    break; // Så snart vi hittar en levande fiende, sluta leta
                }
            }

            if (allEnemiesDead)
            {
                // Om alla fiender är döda, returnera false
                IsMoving = false;
                gameManager.GameOver("Player");
                return false;
            }

            // Rolla en slumpmässig index för att välja fiende
            int randomIndex = Random.Range(0, availableEnemies.Count);
            HealthManager selectedHM = availableEnemies[randomIndex].GetComponent<HealthManager>();

            if (!selectedHM.IsDead)
            {
                // Fienden är inte död, fortsätt
                enemy = availableEnemies[randomIndex];
                enemyFeet = enemy.transform.Find("Feet");
                enemyHealthManager = enemy.GetComponent<HealthManager>();
                enemyMovement = enemy.GetComponent<EnemyMovement>();
                enemyStats = enemy.GetComponent<MonsterStats>();
                targetEnemySet = true;
                return true;
            }
            else
            {
                // Om fienden är död, rulla om för att hitta en ny fiende
                return RollForEnemy(); // Viktigt att returnera resultatet här
            }
        }
        else
        {
            // Om inga fiender finns kvar
            return false;
        }
    }

    private bool EnemyDodges()
    {
        int dodgeChance = monsterStats.DodgeRate;
        int randomValue = Random.Range(0, 100);
        return randomValue < dodgeChance;
    }

    void MovePetToRight(float distance)
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
        petHealthManager.ShowCombatText(0, "Stunned");
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
        if (randomValue < monsterStats.StunRate && !gameManager.IsGameOver)
        {
            return true;
        }
        else { return false; }
    }

    private int CalculateRandomDamage(int baseDamage)
    {
        int outDmg;
        // Calculate a random damage between baseDamage - 2 and baseDamage + 2
        int minDamage = baseDamage - 2;
        int maxDamage = baseDamage + 2;

        // Ensure the minimum damage is at least 1
        if (minDamage < 1) { minDamage = 1; }
        int randomDmg = Random.Range(minDamage, maxDamage + 1);
        int randomValue = Random.Range(0, 100);
        if (randomValue < monsterStats.CritRate)
        {
            outDmg = randomDmg * 2;
            petHealthManager.ShowCombatText(0, "Critical Strike");
        }
        else { outDmg = randomDmg; }
        return outDmg; // Random.Range is inclusive for integers
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
            // Flip mot mål
            FlipTowards(targetPosition);

            anim.SetBool("run", true);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, petSpeed * Time.deltaTime);
            yield return null;
        }

        // Efter att ha nått målet: återställ till vänster
        visualTransform.localRotation = Quaternion.Euler(0, 0, 0);

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
        petHealthManager.ShowCombatText(0, "Dodge!");
        transform.position = targetPosition;

        yield return null; // Yield to ensure any other logic can complete if necessary
    }
}
