using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.TextCore.Text;
using UnityEditor.Timeline.Actions;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Transform parentObj;
    [SerializeField] private Transform charPos;
    [SerializeField] private MonsterDataBase monsterDataBase;

    [SerializeField] private Transform enemyPos;
    [SerializeField] private SceneController sceneController;
    [SerializeField] private Transform weaponInventorySlotParent; // The parent object that holds all the inventory slots
    [SerializeField] private Transform consumableInventorySlotParent;
    [SerializeField] private Transform petInventorySlotParent;
    [SerializeField] private BackgroundImageDataBase backgroundImageDataBase;
    [SerializeField] private SpriteRenderer backgroundImage;


    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] public Transform[] playerStartPositions;
    public bool[] leftPositionsAvailable;

    [SerializeField] public Transform[] enemyStartPositions;
    public bool[] rightPositionsAvailable;
    private List<GameObject> enemyObjects = new List<GameObject>();
    private List<GameObject> petObjects = new List<GameObject>();
    private GameObject playerObject;

    private Vector3 playerInitialScale;
    private Vector3 enemyInitialScale;
    private Vector3 petInitialScale;

    PlayerMovement playerMovement;
    InventoryBattleHandler inventoryBattleHandler;
    SkillBattleHandler skillBattleHandler;
    private List<EnemyMovement> enemyMovements = new List<EnemyMovement>();
    private List<PetMovement> petMovements = new List<PetMovement>();
    private bool initialDelayCompleted;
    private bool rollTime;
    private bool isGameOver;
    [SerializeField] public GameObject damageTextPrefab;
    [SerializeField] public GameObject healthSliderPrefab;

    private int roundsCount;


    public bool IsGameOver
    {
        get { return isGameOver; }
        set { isGameOver = value; }
    }

    public bool RollTime
    {
        get { return rollTime; }
        set { rollTime = value; }
    }

    public int RoundsCount
    {
        get { return roundsCount; }
        set { roundsCount = value; }
    }

    public enum CharacterType
    {
        Player = 1,
        Enemy1 = 2,
        Enemy2 = 3,
        Enemy3 = 4,
        Enemy4 = 5,
        Pet1 = 6,
        Pet2 = 7,
        Pet3 = 8,
        None
    }

    // Define PlayerAction enum for different player actions
    public enum PlayerAction
    {
        Attack,
        UseConsumable,
        EquipWeapon
    }
    public List<GameObject> GetEnemies()
    {
        return enemyObjects;
    }
    public List<GameObject> GetPlayerAndPets()
    {
        // Create a new list to store player and pets
        List<GameObject> playerAndPets = new List<GameObject>();

        // Add the player object to the list if it exists
        if (playerObject != null)
        {
            playerAndPets.Add(playerObject);
        }

        // Add all pet objects to the list if any exist
        if (petObjects != null && petObjects.Count > 0)
        {
            playerAndPets.AddRange(petObjects);
        }

        // Return the combined list
        return playerAndPets;
    }

    void Start()
    {
        BattleBackground battlebackgroundImage = backgroundImageDataBase.GetBattleBackgroundByName(MonsterHuntManager.Instance.sceneState);
        if (battlebackgroundImage != null)
        {
            backgroundImage.sprite = battlebackgroundImage.backgroundImage;
        }
        else
        {
            Debug.LogWarning("Background image not found for state: ");
        }
        // Initialize availability arrays
        leftPositionsAvailable = new bool[playerStartPositions.Length];
        rightPositionsAvailable = new bool[enemyStartPositions.Length];

        for (int i = 0; i < leftPositionsAvailable.Length; i++)
        {
            leftPositionsAvailable[i] = true;
        }

        for (int i = 0; i < rightPositionsAvailable.Length; i++)
        {
            rightPositionsAvailable[i] = true;
        }

        // Randomly spawn player and enemy at available positions
        playerObject = SpawnPlayerAtRandomPosition();
        foreach (string monsterName in MonsterHuntManager.Instance.SelectedMonsterNames)
        {
            enemyObjects.Add(SpawnEnemyAtRandomPosition(monsterName));
        }

        foreach (GameObject pet in Inventory.Instance.GetPets())
        {
            petObjects.Add(SpawnPetAtRandomPosition(pet));
        }

        // Store their initial scales
        if (playerObject != null) playerInitialScale = playerObject.transform.localScale;

        for (int i = 0; i < enemyObjects.Count; i++)
        {
            if (enemyObjects[i] != null) enemyInitialScale = enemyObjects[i].transform.localScale;

        }


        if (Inventory.Instance.GetPets().Count > 0)
        {
            for (int i = 0; i < petObjects.Count; i++)
            {
                if (petObjects[i] != null) petInitialScale = petObjects[i].transform.localScale;
            }
        }
        AdjustAllSortingLayersAndScale();

        GameObject SpawnPlayerAtRandomPosition()
        {
            int randomIndex = GetRandomAvailablePosition(leftPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = playerStartPositions[randomIndex];
                GameObject characterObject = CharacterManager.InstantiateCharacter(
                    CharacterData.Instance,
                    characterPrefab,
                    parentObj,
                    spawnPosition,
                    new Vector3(0.5f, 0.5f, 1f) // Set the desired scale
                );
                characterObject.tag = "Player";
                characterObject.AddComponent<HealthManager>();
                characterObject.AddComponent<PlayerMovement>();
                characterObject.AddComponent<InventoryBattleHandler>();
                characterObject.AddComponent<SkillBattleHandler>();
                skillBattleHandler = characterObject.GetComponent<SkillBattleHandler>();
                inventoryBattleHandler = characterObject.GetComponent<InventoryBattleHandler>();
                playerMovement = characterObject.GetComponent<PlayerMovement>();
                // Mark this position as occupied
                leftPositionsAvailable[randomIndex] = false;
                playerMovement.positionIndex = randomIndex;
                return characterObject; // Return the instantiated player object
            }
            return null;
        }

        GameObject SpawnPetAtRandomPosition(GameObject pet)
        {
            int randomIndex = GetRandomAvailablePosition(leftPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = playerStartPositions[randomIndex];
                MonsterStats stats = pet.GetComponent<MonsterStats>();
                GameObject petObject = PetManager.InstantiatePet(
                    pet,
                    parentObj,
                    spawnPosition,
                    stats.Scale // Set the desired scale
                );
                string petTag = "Pet" + (petObjects.Count + 1);
                petObject.tag = petTag;
                petObject.AddComponent<HealthManager>();
                petObject.AddComponent<PetMovement>();
                petObject.AddComponent<SkillBattleHandler>();
                petMovements.Add(petObject.GetComponent<PetMovement>());
                // Mark this position as occupied
                leftPositionsAvailable[randomIndex] = false;
                PetMovement petMov = petObject.GetComponent<PetMovement>();
                petMov.positionIndex = randomIndex;
                return petObject; // Return the instantiated player object
            }
            return null;
        }



        GameObject SpawnEnemyAtRandomPosition(string monsterName)
        {
            int randomIndex = GetRandomAvailablePosition(rightPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = enemyStartPositions[randomIndex];
                GameObject monsterPrefab = monsterDataBase.GetMonsterByName(monsterName);
                MonsterStats stats = monsterPrefab.GetComponent<MonsterStats>();

                GameObject monsterObject = MonsterManager.InstantiateMonster(
                    monsterPrefab,
                    parentObj,
                    spawnPosition,
                    stats.Scale // Set the initial scale based on stats
                );
                string enemyTag = "Enemy" + (enemyObjects.Count + 1); // Exempel: Enemy1, Enemy2, etc.
                monsterObject.tag = enemyTag;
                monsterObject.AddComponent<HealthManager>();
                monsterObject.AddComponent<EnemyMovement>();
                monsterObject.AddComponent<SkillBattleHandler>();
                enemyMovements.Add(monsterObject.GetComponent<EnemyMovement>());
                // Mark this position as occupied
                rightPositionsAvailable[randomIndex] = false;
                EnemyMovement enemyMov = monsterObject.GetComponent<EnemyMovement>();
                enemyMov.positionIndex = randomIndex;
                return monsterObject; // Return the instantiated enemy object
            }
            return null;
        }
        StartCoroutine(InitialDelay());
    }

    // Method to get a random available position index
    public int GetRandomAvailablePosition(bool[] positionsAvailable)
    {
        List<int> availableIndices = new List<int>();

        // Collect all available positions
        for (int i = 0; i < positionsAvailable.Length; i++)
        {
            if (positionsAvailable[i])
            {
                availableIndices.Add(i);
            }
        }

        // If no available positions, return -1
        if (availableIndices.Count == 0)
        {
            return -1;
        }

        // Pick a random index from available positions
        int randomIndex = Random.Range(0, availableIndices.Count);
        return availableIndices[randomIndex];
    }

    IEnumerator InitialDelay()
    {
        // Wait for 2 seconds
        yield return new WaitForSeconds(0.1f);
        UpdateBattleInventorySlots();
        yield return new WaitForSeconds(0.9f);
        rollTime = true;
        initialDelayCompleted = true;

    }
    private void FixedUpdate()
    {
        AdjustAllSortingLayersAndScale();

        if (initialDelayCompleted && rollTime && !IsGameOver)
        {
            CharacterType startingCharacter;
            List<GameObject> allCharacters = new List<GameObject>
    {
        playerObject
    };
            allCharacters.AddRange(enemyObjects);
            allCharacters.AddRange(petObjects);
            foreach (GameObject character in allCharacters)
            {
                if (character.tag == "Player")
                {
                    HealthManager healthManager = character.GetComponent<HealthManager>();
                    healthManager.RemoveVemon();
                    PlayerMovement movement = character.GetComponent<PlayerMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "Pet1" || character.tag == "Pet2" || character.tag == "Pet3")
                {
                    HealthManager healthManager = character.GetComponent<HealthManager>();
                    healthManager.RemoveVemon();
                    PetMovement movement = character.GetComponent<PetMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "Enemy1" || character.tag == "Enemy2" || character.tag == "Enemy3" || character.tag == "Enemy4")
                {
                    HealthManager healthManager = character.GetComponent<HealthManager>();
                    healthManager.RemoveVemon();
                    EnemyMovement movement = character.GetComponent<EnemyMovement>();
                    movement.RemoveStun();
                }
            }
            if (roundsCount > 0)
            {
                if (Inventory.Instance.HasSkill("LifeBlood"))
                {
                    skillBattleHandler.LifeBlood(playerObject);
                }
            }

            startingCharacter = DetermineHighestRoll();

            // Switch-sats för att hantera attackerande karaktärer
            switch (startingCharacter)
            {
                case CharacterType.Player:

                    HandlePlayerTurn();
                    break;

                case CharacterType.Enemy1:
                case CharacterType.Enemy2:
                case CharacterType.Enemy3:
                case CharacterType.Enemy4:
                    int enemyIndex = (int)startingCharacter - 2;
                    if (enemyIndex < enemyObjects.Count)
                    {

                        HandleEnemyTurn(enemyIndex);
                    }
                    break;

                case CharacterType.Pet1:
                case CharacterType.Pet2:
                case CharacterType.Pet3:
                    int petIndex = (int)startingCharacter - 6;
                    if (petIndex < petObjects.Count)
                    {

                        HandlePetTurn(petIndex);
                    }
                    break;

                default:
                    break;
            }
        }
    }
    private void AdjustAllSortingLayersAndScale()
    {
        // Lista över alla karaktärer: spelare, fiender och husdjur
        List<GameObject> allCharacters = new List<GameObject>
    {
        playerObject
    };
        allCharacters.AddRange(enemyObjects);
        allCharacters.AddRange(petObjects);

        // Sortera karaktärerna baserat på deras feet position (y) istället för deras position
        allCharacters = allCharacters.OrderByDescending(c => c.transform.Find("Feet").position.y).ToList();

        // Loop för att justera sorteringslager och skalning
        for (int i = 0; i < allCharacters.Count; i++)
        {
            GameObject character = allCharacters[i];
            if (character != null)
            {
                // Hämta ursprungliga skalan för varje karaktär
                Vector3 initialScale = GetInitialScale(character);

                // Justera skalan baserat på feet position (Y-position)
                AdjustScaleBasedOnPosition(character, initialScale);

                // Justera sorteringslagret baserat på dess index
                SetSortingLayer(character, i);
            }
        }
    }

    private Vector3 GetInitialScale(GameObject character)
    {
        if (character == playerObject)
        {
            return playerInitialScale; // Använd den ursprungliga skalan för spelaren
        }
        else if (enemyObjects.Contains(character))
        {
            return enemyInitialScale; // Använd den ursprungliga skalan för fiender
        }
        else if (petObjects.Contains(character))
        {
            return petInitialScale; // Använd den ursprungliga skalan för husdjur
        }
        return Vector3.one; // Standardvärde om något skulle gå fel
    }

    private void AdjustScaleBasedOnPosition(GameObject character, Vector3 initialScale)
    {
        // Hämta skärmpositionen för karaktären baserat på deras feet position
        Vector3 screenPosition = Camera.main.WorldToViewportPoint(character.transform.Find("Feet").position);

        // Beräkna skalan baserat på den vertikala (y) skärmpositionen
        float scaleMultiplier = Mathf.Lerp(1f, 0.5f, screenPosition.y); // 1 är större nära botten, 0.7 är mindre nära toppen

        // Applicera den nya skalan baserat på den ursprungliga skalan
        character.transform.localScale = initialScale * scaleMultiplier;
    }

    private void SetSortingLayer(GameObject character, int index)
    {
        // Adjust sorting layers for SpriteRenderers based on the index
        string sortingLayer = AdjustSortingLayers(index);

        // Set sorting layers for all SpriteRenderers
        SpriteRenderer[] spriteRenderers = character.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingLayerName = sortingLayer;
        }

        // Set the sorting layer for Canvases (if necessary)
        Canvas[] canvases = character.GetComponentsInChildren<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            canvas.sortingLayerName = "firstFront"; // Set canvas layer separately
            canvas.overrideSorting = true;
        }

        // Set the sorting layer for TrailRenderer
        TrailRenderer[] trailRenderers = character.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trail in trailRenderers)
        {
            trail.sortingLayerName = sortingLayer; // Use the same sorting layer as SpriteRenderers
            trail.sortingOrder = 0; // Adjust order if needed

            Renderer trailRenderer = trail.GetComponent<Renderer>();
            if (trailRenderer != null)
            {
                trailRenderer.sortingLayerName = sortingLayer;
                trailRenderer.sortingOrder = 0;
            }
        }

        // Set the sorting layer for ParticleSystemRenderer (for hit particles)
        ParticleSystemRenderer[] particleRenderers = character.GetComponentsInChildren<ParticleSystemRenderer>();
        foreach (ParticleSystemRenderer particleRenderer in particleRenderers)
        {
            particleRenderer.sortingLayerName = sortingLayer; // Use the same sorting layer as SpriteRenderers
            particleRenderer.sortingOrder = 1; // Adjust the sorting order if necessary
        }
    }

    private string AdjustSortingLayers(int index)
    {
        // Definiera lagernamnen baserat på deras index
        switch (index)
        {
            case 0:
                return "firstBack";
            case 1:
                return "secondBack";
            case 2:
                return "midToBack";
            case 3:
                return "middleUp";
            case 4:
                return "middleDown";
            case 5:
                return "midToFront";
            case 6:
                return "secondFront";
            case 7:
                return "firstFront";
            default:
                return "Default"; // Standardlager om indexet överstiger max antal karaktärer
        }
    }

    // Method to assign sorting layer to a character

    public int RollNumberPlayer()
    {
        int randomNumber = Random.Range(0, 100); //Rolls higher ddepending on lvl and initiative

        return randomNumber;
    }

    public int RollNumberEnemy()
    {
        int randomNumber = Random.Range(0, 100);

        return randomNumber;
    }
    public int RollNumberPet()
    {
        int randomNumber = Random.Range(0, 100);
        return randomNumber;
    }

    public CharacterType DetermineHighestRoll()
    {
        Dictionary<CharacterType, int> rolls = new Dictionary<CharacterType, int>();

        // Hantera spelaren och ignorera om denne är stunned
        PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
        if (playerMovement != null && !playerMovement.IsStunned)
        {
            rolls.Add(CharacterType.Player, RollNumberPlayer());
        }

        // Hantera fiender och ignorera de som är döda eller stunned
        for (int i = 0; i < enemyObjects.Count; i++)
        {
            HealthManager enemyHealth = enemyObjects[i].GetComponent<HealthManager>();
            EnemyMovement enemyMovement = enemyObjects[i].GetComponent<EnemyMovement>();
            if (enemyHealth != null && !enemyHealth.IsDead && enemyMovement != null && !enemyMovement.IsStunned)
            {
                rolls.Add((CharacterType)(2 + i), RollNumberEnemy());
            }
        }

        // Hantera husdjur och ignorera de som är döda eller stunned
        for (int i = 0; i < petObjects.Count; i++)
        {
            HealthManager petHealth = petObjects[i].GetComponent<HealthManager>();
            PetMovement petMovement = petObjects[i].GetComponent<PetMovement>();
            if (petHealth != null && !petHealth.IsDead && petMovement != null && !petMovement.IsStunned)
            {
                rolls.Add((CharacterType)(6 + i), RollNumberPet());
            }
        }

        // Välj den karaktär med högst rullning
        if (rolls.Count > 0)
        {
            var highest = rolls.OrderByDescending(r => r.Value).First();
            rollTime = false;
            roundsCount++;
            return highest.Key;
        }

        // Hantera fallet där ingen rullning görs
        return CharacterType.None; // Eller någon annan lämplig default.
    }
    private void HandlePetTurn(int petIndex)
    {
        petMovements[petIndex].IsMoving = true;
        StartCoroutine(PetAttack(petIndex));
    }
    private IEnumerator PetAttack(int petIndex)
    {
        yield return new WaitForSeconds(0.5f);
        petMovements[petIndex].IsMoving = false;
    }

    private void HandleEnemyTurn(int enemyIndex)
    {
        enemyMovements[enemyIndex].IsMoving = true;
        StartCoroutine(EnemyAttack(enemyIndex));
    }
    private IEnumerator EnemyAttack(int enemyIndex)
    {
        yield return new WaitForSeconds(0.5f);
        enemyMovements[enemyIndex].IsMoving = false;
    }


    private void HandlePlayerTurn()
    {
        if (!playerMovement.RollForEnemy())
        {
            GameOver("Player");
        }
        else
        {
            if (Inventory.Instance.HasSkill("Berserk") && !skillBattleHandler.berserkUsed)
            {
                HealthManager playerHealthManager = playerObject.GetComponent<HealthManager>();
                int currentHealth = playerHealthManager.CurrentHealth;
                if (currentHealth < CharacterData.Instance.Health / 3)
                {
                    playerMovement.berserkDamage = skillBattleHandler.Berserk(playerObject);
                }
            }

            int randomNumber = RollNumberPlayer();

            PlayerAction action = DeterminePlayerAction(randomNumber);


            switch (action)
            {
                case PlayerAction.UseConsumable:
                    if (inventoryBattleHandler.GetCombatConsumableInventory().Count > 0)
                    {
                        StartCoroutine(inventoryBattleHandler.UseConsumable(0.5f));
                    }
                    else
                    {
                        StartCoroutine(PlayerAttack());
                    }
                    break;

                case PlayerAction.EquipWeapon:
                    if (inventoryBattleHandler.GetCombatWeaponInventory().Count > 0 && !inventoryBattleHandler.IsWeaponEquipped)
                    {
                        StartCoroutine(inventoryBattleHandler.EquipWeaponAndStartMoving(0.2f));
                    }
                    else
                    {
                        StartCoroutine(PlayerAttack());
                    }
                    break;

                case PlayerAction.Attack:
                    StartCoroutine(PlayerAttack());
                    break;

                default:

                    break;
            }

        }
    }

    private PlayerAction DeterminePlayerAction(int roll)
    {
        if (roll >= 50 && inventoryBattleHandler.GetCombatConsumableInventory().Count > 0)
        {
            return PlayerAction.UseConsumable;
        }
        else if (roll < 50 && inventoryBattleHandler.GetCombatWeaponInventory().Count > 0 && !inventoryBattleHandler.IsWeaponEquipped)
        {
            return PlayerAction.EquipWeapon;
        }
        else
        {
            return PlayerAction.Attack;
        }
    }

    private IEnumerator PlayerAttack()
    {
        playerMovement.IsMoving = true;
        yield return new WaitForSeconds(0.5f);
        playerMovement.IsMoving = false;
    }

    public void GameOver(string winnerTag)
    {

        if (!isGameOver)
        {
            isGameOver = true;
            inventoryBattleHandler.DestroyWeapon();
            List<string> listOfNames = new List<string>(); // Skapa en lista för att lagra monster-namnen

            // Lägg till namnen på besegrade monster i listan
            foreach (GameObject enemy in enemyObjects)
            {
                MonsterStats statsFromDeadEnemy = enemy.GetComponent<MonsterStats>();

                listOfNames.Add(statsFromDeadEnemy.MonsterName); // Lägg till namnet i listan
            }

            // Om du vill omvandla listan till en array
            string[] arrayOfNames = listOfNames.ToArray();
            FightData.Instance.AddFightResultNames(arrayOfNames);
            // Ange om striden var en vinst eller förlust baserat på winnerTag
            if (winnerTag == "Player")
            {
                FightData.Instance.AddFightResultWinOrLoss(true);
            }
            else { FightData.Instance.AddFightResultWinOrLoss(false); }


            // Starta fördröjd scenövergång
            MonsterHuntManager.Instance.Cleanup();
            StartCoroutine(DelayedSceneLoad());
        }
    }
    public void Died(string tag)
    {
        if (tag == "Player")
        {
            playerMovement.IsMoving = false;
            GameOver("Enemy");
        }
    }

    public void UpdateBattleInventorySlots()
    {
        // Clear any existing slots if necessary
        foreach (Transform child in weaponInventorySlotParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in consumableInventorySlotParent)
        {
            Destroy(child.gameObject);
        }
        // Iterate over each item in the combat inventory
        foreach (Item item in inventoryBattleHandler.GetCombatWeaponInventory())
        {
            // Instantiate a new inventory slot from the prefab
            GameObject newSlot = Instantiate(inventorySlotPrefab, weaponInventorySlotParent);

            // Get the Image component from the slot
            Image slotImage = newSlot.GetComponent<Image>();

            // Set the sprite of the image to the item's sprite
            slotImage.sprite = item.itemSprite;
        }
        foreach (Item item in inventoryBattleHandler.GetCombatConsumableInventory())
        {
            // Instantiate a new inventory slot from the prefab
            GameObject newSlot = Instantiate(inventorySlotPrefab, consumableInventorySlotParent);

            // Get the Image component from the slot
            Image slotImage = newSlot.GetComponent<Image>();

            // Set the sprite of the image to the item's sprite
            slotImage.sprite = item.itemSprite;
        }
    }
    private IEnumerator DelayedSceneLoad()
    {
        BackgroundMusicManager.Instance.FadeOutMusic(3f);
        yield return new WaitForSeconds(3f); // Wait for 2 seconds
        sceneController.LoadScene("RewardScene");
    }
}

