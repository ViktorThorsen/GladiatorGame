using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class ReplayPlayerMovement : MonoBehaviour
{
    private ReplayGameManager gameManager;
    private Animator anim;
    private GameObject player;
    private ReplayInventoryBattleHandler playerInventoryBattleHandler;
    private SkillBattleHandler skillBattleHandler;
    [SerializeField] private Transform playerFeet;
    private ReplayHealthManager playerHealthManager;

    [SerializeField] private int playerSpeed;
    private GameObject enemy;
    private List<GameObject> availableEnemies = new List<GameObject>();
    private Transform enemyFeet;
    private ReplayHealthManager enemyHealthManager;
    private ReplayEnemyPlayerMovement enemyGladMovement;
    private ReplayEnemyPetMovement enemyPetMovement;
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

        gameManager = FindObjectOfType<ReplayGameManager>();

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
        playerInventoryBattleHandler = player.GetComponent<ReplayInventoryBattleHandler>();
        skillBattleHandler = player.GetComponent<SkillBattleHandler>();
        playerHealthManager = player.GetComponent<ReplayHealthManager>();

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
            Vector2 newPosition = Vector2.MoveTowards(playerFeet.position, targetPosition, playerSpeed * Time.deltaTime);
            // Update the main transform's position based on feet's new position difference
            transform.position += (Vector3)(newPosition - (Vector2)playerFeet.position);
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
                    if (!gameManager.IsGameOver)
                    {
                        bool enemyStunned = CalculateStun(0);
                        if (enemyStunned) { enemyGladMovement.Stun(); }
                        if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "VenomousTouch") == true && ReplayManager.Instance.selectedReplay.enemy.skills.skills?.Any(s => s.skillName == "CleanseBorn") == true)
                        {
                            ApplyVenom(player);
                        }

                        // Hitta första attacken eller crit från detta pet
                        var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 1); // eller Pet2/Pet3 beroende på vilket pet
                        if (attack == null)
                        {
                            Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                        }

                        bool isCrit = attack.Action == "crit";
                        int damage = attack.Value;
                        enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                        CalcLifesteal(damage);
                        if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                        {
                            skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                        }
                        RollForDestroyWeapon(1);
                    }
                }
                else
                {
                    enemyPetMovement.MovePetToRight(0.5f);

                    bool enemyStunned = CalculateStun(0);
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "VenomousTouch") == true)
                    {
                        ApplyVenom(player);
                    }
                    // Hitta första attacken eller crit från detta pet
                    var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 1); // eller Pet2/Pet3 beroende på vilket pet
                    if (attack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = attack.Action == "crit";
                    int damage = attack.Value;
                    enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                    CalcLifesteal(damage);
                    if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                    {
                        skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon(1);
                }
            }


        }
        yield return new WaitForSeconds(0.5f);

        var secondAttackControll2 = GetNthAttackThisTurn(GetCharacterType(player.tag), 2);
        if (secondAttackControll2 != null)
        {
            // Always trigger the second hit animation and movement
            anim.SetTrigger("hit1");
            MovePlayerToRight(0.5f);
            // Check if the enemy dodges the second hit
            if (EnemyDodges(1))
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

                    bool enemyStunned = CalculateStun(1);
                    if (enemyStunned) { enemyGladMovement.Stun(); }
                    var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 2); // eller Pet2/Pet3 beroende på vilket pet
                    if (attack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = attack.Action == "crit";
                    int damage = attack.Value;
                    enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                    CalcLifesteal(damage);
                    if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                    {
                        skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon(2);

                }
                else
                {
                    enemyPetMovement.MovePetToRight(0.5f);

                    bool enemyStunned = CalculateStun(1);
                    if (enemyStunned) { enemyPetMovement.Stun(); }
                    var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 2); // eller Pet2/Pet3 beroende på vilket pet
                    if (attack == null)
                    {
                        Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                    }

                    bool isCrit = attack.Action == "crit";
                    int damage = attack.Value;
                    enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                    CalcLifesteal(damage);
                    if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                    {
                        skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                    }
                    RollForDestroyWeapon(2);


                }
            }

            yield return new WaitForSeconds(0.5f);
            var secondAttackControll3 = GetNthAttackThisTurn(GetCharacterType(player.tag), 3);
            if (secondAttackControll3 != null)
            {
                // Always trigger the third hit animation and movement
                anim.SetTrigger("hook");
                MovePlayerToRight(0.5f);

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

                        bool enemyStunned = CalculateStun(2);
                        if (enemyStunned) { enemyGladMovement.Stun(); }

                        var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 3); // eller Pet2/Pet3 beroende på vilket pet
                        if (attack == null)
                        {
                            Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                        }

                        bool isCrit = attack.Action == "crit";
                        int damage = attack.Value;
                        enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                        CalcLifesteal(damage);
                        if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                        {
                            skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                        }
                        RollForDestroyWeapon(3);

                    }
                    else
                    {
                        enemyPetMovement.MovePetToRight(0.5f);

                        bool enemyStunned = CalculateStun(2);
                        if (enemyStunned) { enemyPetMovement.Stun(); }

                        var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 3); // eller Pet2/Pet3 beroende på vilket pet
                        if (attack == null)
                        {
                            Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                        }

                        bool isCrit = attack.Action == "crit";
                        int damage = attack.Value;
                        enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                        CalcLifesteal(damage);
                        if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                        {
                            skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                        }
                        RollForDestroyWeapon(3);

                    }

                }

                yield return new WaitForSeconds(0.5f);
                var secondAttackControll4 = GetNthAttackThisTurn(GetCharacterType(player.tag), 4);
                if (secondAttackControll4 != null)
                {
                    // Always trigger the fourth hit animation and movement
                    anim.SetTrigger("uppercut");
                    MovePlayerToRight(0.5f);

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

                            bool enemyStunned = CalculateStun(3);
                            if (enemyStunned) { enemyGladMovement.Stun(); }
                            var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 4); // eller Pet2/Pet3 beroende på vilket pet
                            if (attack == null)
                            {
                                Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                            }

                            bool isCrit = attack.Action == "crit";
                            int damage = attack.Value;
                            enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                            CalcLifesteal(damage);
                            if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                            {
                                skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                            }
                            RollForDestroyWeapon(4);

                        }
                        else
                        {
                            enemyPetMovement.MovePetToRight(0.5f);

                            bool enemyStunned = CalculateStun(3);
                            if (enemyStunned) { enemyPetMovement.Stun(); }
                            var attack = GetNthAttackThisTurn(GetCharacterType(player.tag), 4); // eller Pet2/Pet3 beroende på vilket pet
                            if (attack == null)
                            {
                                Debug.LogWarning("❌ Ingen andra attack hittades i denna rundan.");
                            }

                            bool isCrit = attack.Action == "crit";
                            int damage = attack.Value;
                            enemyHealthManager.ReduceHealth(damage, "Normal", player, isCrit);
                            CalcLifesteal(damage);
                            if (ReplayManager.Instance.selectedReplay.player.skills.skills?.Any(s => s.skillName == "Cleave") == true)
                            {
                                skillBattleHandler.ReplayCleaveDamage(GetAllAliveEnemies(), enemy, damage, player);
                            }
                            RollForDestroyWeapon(4);


                        }
                    }

                    yield return new WaitForSeconds(0.5f);
                    anim.SetTrigger("stophit");
                    yield return new WaitForSeconds(0.05f);
                    if (IsCounterAttackThisTurn())
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ReplayCounterStrike(player, () =>
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
                    if (IsCounterAttackThisTurn())
                    {
                        SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                        // Call CounterStrike and pass MoveBackToRandomStart as the callback
                        playerSkills.ReplayCounterStrike(player, () =>
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
                if (IsCounterAttackThisTurn())
                {
                    SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                    // Call CounterStrike and pass MoveBackToRandomStart as the callback
                    playerSkills.ReplayCounterStrike(player, () =>
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
            if (IsCounterAttackThisTurn())
            {
                SkillBattleHandler playerSkills = enemy.GetComponent<SkillBattleHandler>();

                // Call CounterStrike and pass MoveBackToRandomStart as the callback
                playerSkills.ReplayCounterStrike(player, () =>
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
        // Hämta rätt turn-nummer
        int currentTurn = gameManager.RoundsCount;

        // Hämta alla actions från denna rundan
        var actionsThisTurn = ReplayManager.Instance.selectedReplay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

        // Hitta första attack från Player denna rundan
        var playerAttack = actionsThisTurn
            .FirstOrDefault(a => a.Actor == CharacterType.Player && a.Action == "attack" || a.Action == "crit");

        CharacterType targetType;

        if (playerAttack != null)
        {
            targetType = playerAttack.Target;
        }
        else
        {
            // Om ingen attack eller crit hittas, leta efter dodge där Player är target
            var dodge = actionsThisTurn
                .FirstOrDefault(a => a.Target == CharacterType.Player && a.Action == "dodge");

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
        CombatTextManager.Instance.SpawnText("Stunned", player.transform.position + Vector3.up * 1.5f, "#FFFFFF");
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
        int baseLifeSteal = ReplayCharacterData.Instance.LifeSteal;

        if (baseLifeSteal > 0)
        {
            float lifeStealMultiplier = baseLifeSteal / 100f;

            var vampyreSkillInstance = ReplayManager.Instance.selectedReplay.player.skills.skills
                ?.FirstOrDefault(s => s.skillName == "Vampyre");

            if (vampyreSkillInstance != null)
            {
                var vampyreData = SkillDataBase.Instance.GetSkillByName("Vampyre");
                if (vampyreData != null)
                {
                    int bonusPercent = 0;

                    switch (vampyreSkillInstance.level)
                    {
                        case 1:
                            bonusPercent = vampyreData.effectPercentIncreaseLevel1;
                            break;
                        case 2:
                            bonusPercent = vampyreData.effectPercentIncreaseLevel2;
                            break;
                        case 3:
                            bonusPercent = vampyreData.effectPercentIncreaseLevel3;
                            break;
                    }

                    lifeStealMultiplier += bonusPercent / 100f;
                }
            }

            int vampBonus = Mathf.RoundToInt(damage * lifeStealMultiplier);
            if (vampBonus < 1)
            {
                vampBonus = 1;
            }

            playerHealthManager.IncreaseHealth(vampBonus);
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
            .Where(a => (a.Action == "attack" || a.Action == "crit") && a.Actor == CharacterType.Player)
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

    private void ApplyVenom(GameObject dealer)
    {
        enemyHealthManager.ApplyVenom(dealer);
    }

    public int CalculateRandomDamage(int baseDamage)
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
            // Ensure the player is always facing left (flip if necessary) during the movement
            if (transform.position.x > targetPosition.x)
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            anim.SetBool("run", true);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, playerSpeed * Time.deltaTime);
            yield return null;
        }

        anim.SetBool("run", false);
        moveCoroutine = null;
        IsMoving = false;
        gameManager.RollTime = true;
    }

    private void RollForDestroyWeapon(int attackIndex)
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = ReplayGameManager.Instance.RoundsCount;

        // Hämta alla actions från denna runda där Actor är samma som attackeraren
        var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn && a.Actor == CharacterType.Player)
            .ToList();

        // Hitta alla attacker/crit i turordning
        var attackActions = actionsThisTurn
            .Where(a => a.Action == "attack" || a.Action == "crit")
            .ToList();

        if (attackIndex - 1 >= attackActions.Count)
        {
            Debug.LogWarning($"❌ Det finns inte {attackIndex} attacker för denna rundan.");
            return;
        }

        // Ta ut matchande action i combo
        var selectedAttack = attackActions[attackIndex - 1];

        // Kolla om nästa action direkt efter är "weapondestroyed"
        int attackPos = actionsThisTurn.IndexOf(selectedAttack);

        if (attackPos + 1 < actionsThisTurn.Count &&
            actionsThisTurn[attackPos + 1].Action == "weapondestroyed")
        {
            Debug.Log($"🧨 Vapnet gick sönder efter attack {attackIndex} (av )");
            if (playerInventoryBattleHandler.currentWeapon != null)
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
        CombatTextManager.Instance.SpawnText("Dodge", player.transform.position + Vector3.up * 1.5f, "#FFFFFF");
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
    private List<GameObject> GetAllAliveEnemies()
    {
        List<GameObject> aliveEnemies = new List<GameObject>();

        foreach (GameObject enemy in availableEnemies)
        {
            ReplayHealthManager HM = enemy.GetComponent<ReplayHealthManager>();
            if (!HM.IsDead)
            {
                aliveEnemies.Add(enemy); // Only add alive enemies to the list
            }
        }
        return aliveEnemies;
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
        else return false;
    }
}

