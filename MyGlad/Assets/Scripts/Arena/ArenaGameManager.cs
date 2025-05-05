using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class ArenaGameManager : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Transform parentObj;
    [SerializeField] private Transform charPos;
    [SerializeField] private Transform enemyPos;
    [SerializeField] private SceneController sceneController;
    [SerializeField] private Transform weaponInventorySlotParent; // The parent object that holds all the inventory slots
    [SerializeField] private Transform consumableInventorySlotParent;
    [SerializeField] private Transform petInventorySlotParent;
    [SerializeField] private Transform enemyWeaponInventorySlotParent; // The parent object that holds all the inventory slots
    [SerializeField] private Transform enemyConsumableInventorySlotParent;
    [SerializeField] private Transform enemyPetInventorySlotParent;

    [SerializeField] private BackgroundImageDataBase backgroundImageDataBase;
    [SerializeField] private SpriteRenderer backgroundImage;

    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] public Transform[] playerStartPositions;
    public bool[] leftPositionsAvailable;

    [SerializeField] public Transform[] enemyStartPositions;
    public bool[] rightPositionsAvailable;
    private List<GameObject> enemyPetObjects = new List<GameObject>();
    private List<GameObject> petObjects = new List<GameObject>();
    private GameObject enemyGladiatorObject;
    private GameObject playerObject;

    private Vector3 playerInitialScale;
    private Vector3 enemyGladiatorInitialScale;
    private Vector3 petInitialScale;
    private Vector3 enemyPetInitialScale;

    ArenaPlayerMovement playerMovement;
    ArenaEnemyPlayerMovement enemyGladMovement;
    ArenaInventoryBattleHandler inventoryBattleHandler;
    ArenaEnemyInventoryBattleHandler enemyGladInventoryBattleHandler;
    SkillBattleHandler skillBattleHandler;
    SkillBattleHandler enemyGladSkillBattleHandler;
    private List<ArenaEnemyPetMovement> enemyPetMovements = new List<ArenaEnemyPetMovement>();
    private List<ArenaPetMovement> petMovements = new List<ArenaPetMovement>();
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
        EnemyGlad = 2,
        EnemyPet1 = 3,
        EnemyPet2 = 4,
        EnemyPet3 = 5,
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
        List<GameObject> enemyAndPets = new List<GameObject>();
        if (enemyGladiatorObject != null)
        {
            enemyAndPets.Add(enemyGladiatorObject);
        }

        // Add all pet objects to the list if any exist
        if (enemyPetObjects != null && enemyPetObjects.Count > 0)
        {
            enemyAndPets.AddRange(enemyPetObjects);
        }
        return enemyAndPets;
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

    public string GetEnemyBackground()
    {
        int level = EnemyGladiatorData.Instance.Level;

        string backgroundName = "";

        switch (level)
        {
            case 1:
                backgroundName = "Farm";
                break;
            case 2:
                backgroundName = "Forest";
                break;
            case 3:
                backgroundName = "Alley";
                break;
            case 4:
                backgroundName = "SnowMountain";
                break;
            case 5:
                backgroundName = "Colosseum";
                break;
            default:
                backgroundName = "Farm";
                break;
        }

        return backgroundName;
    }

    void Start()
    {
        BattleBackground battlebackgroundImage = backgroundImageDataBase.GetBattleBackgroundByName(GetEnemyBackground());
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
        enemyGladiatorObject = SpawnEnemyGladiatorAtRandomPosition();
        foreach (GameObject enemyPet in EnemyInventory.Instance.GetPets())
        {
            enemyPetObjects.Add(SpawnEnemyPetAtRandomPosition(enemyPet));
        }

        foreach (GameObject pet in Inventory.Instance.GetPets())
        {
            petObjects.Add(SpawnPetAtRandomPosition(pet));
        }

        // Store their initial scales
        if (playerObject != null) playerInitialScale = playerObject.transform.localScale;
        if (enemyGladiatorObject != null) enemyGladiatorInitialScale = enemyGladiatorObject.transform.localScale;

        if (EnemyInventory.Instance.GetPets().Count > 0)
        {
            for (int i = 0; i < enemyPetObjects.Count; i++)
            {
                if (enemyPetObjects[i] != null) enemyPetInitialScale = enemyPetObjects[i].transform.localScale;
            }
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
                characterObject.AddComponent<ArenaHealthManager>();
                characterObject.AddComponent<ArenaPlayerMovement>();
                characterObject.AddComponent<ArenaInventoryBattleHandler>();
                characterObject.AddComponent<SkillBattleHandler>();
                skillBattleHandler = characterObject.GetComponent<SkillBattleHandler>();
                inventoryBattleHandler = characterObject.GetComponent<ArenaInventoryBattleHandler>();
                playerMovement = characterObject.GetComponent<ArenaPlayerMovement>();
                // Mark this position as occupied
                leftPositionsAvailable[randomIndex] = false;
                playerMovement.positionIndex = randomIndex;
                return characterObject; // Return the instantiated player object
            }
            return null;
        }

        GameObject SpawnEnemyGladiatorAtRandomPosition()
        {
            int randomIndex = GetRandomAvailablePosition(rightPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = enemyStartPositions[randomIndex];
                GameObject characterObject = CharacterManager.InstantiateEnemyGladiator(
                    EnemyGladiatorData.Instance,
                    characterPrefab,
                    parentObj,
                    spawnPosition,
                    new Vector3(-0.5f, 0.5f, 1f) // Set the desired scale
                );
                characterObject.tag = "EnemyGlad";
                characterObject.AddComponent<ArenaHealthManager>();
                characterObject.AddComponent<ArenaEnemyPlayerMovement>();
                characterObject.AddComponent<ArenaEnemyInventoryBattleHandler>();
                characterObject.AddComponent<SkillBattleHandler>();
                enemyGladSkillBattleHandler = characterObject.GetComponent<SkillBattleHandler>();
                enemyGladInventoryBattleHandler = characterObject.GetComponent<ArenaEnemyInventoryBattleHandler>();
                enemyGladMovement = characterObject.GetComponent<ArenaEnemyPlayerMovement>();
                // Mark this position as occupied
                rightPositionsAvailable[randomIndex] = false;
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
                petObject.AddComponent<ArenaHealthManager>();
                petObject.AddComponent<ArenaPetMovement>();
                petObject.AddComponent<SkillBattleHandler>();
                petMovements.Add(petObject.GetComponent<ArenaPetMovement>());
                // Mark this position as occupied
                leftPositionsAvailable[randomIndex] = false;
                ArenaPetMovement petMov = petObject.GetComponent<ArenaPetMovement>();
                petMov.positionIndex = randomIndex;
                return petObject; // Return the instantiated player object
            }
            return null;
        }



        GameObject SpawnEnemyPetAtRandomPosition(GameObject enemyPet)
        {
            int randomIndex = GetRandomAvailablePosition(rightPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = enemyStartPositions[randomIndex];
                MonsterStats stats = enemyPet.GetComponent<MonsterStats>();
                GameObject enemyPetObject = PetManager.InstantiatePet(
                    enemyPet,
                    parentObj,
                    spawnPosition,
                    stats.Scale // Set the desired scale
                );
                Vector3 petScale = enemyPetObject.transform.localScale;
                petScale.x = Mathf.Abs(petScale.x) * 1; // Ensure the X scale is negative
                enemyPetObject.transform.localScale = petScale;
                string petTag = "EnemyPet" + (enemyPetObjects.Count + 1);
                enemyPetObject.tag = petTag;
                enemyPetObject.AddComponent<ArenaHealthManager>();
                enemyPetObject.AddComponent<ArenaEnemyPetMovement>();
                enemyPetObject.AddComponent<SkillBattleHandler>();
                enemyPetMovements.Add(enemyPetObject.GetComponent<ArenaEnemyPetMovement>());
                // Mark this position as occupied
                rightPositionsAvailable[randomIndex] = false;
                ArenaEnemyPetMovement petMov = enemyPetObject.GetComponent<ArenaEnemyPetMovement>();
                petMov.positionIndex = randomIndex;
                return enemyPetObject; // Return the instantiated player object
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
        playerObject,enemyGladiatorObject
    };
            allCharacters.AddRange(enemyPetObjects);
            allCharacters.AddRange(petObjects);
            foreach (GameObject character in allCharacters)
            {
                if (character.tag == "Player")
                {
                    ArenaHealthManager healthManager = character.GetComponent<ArenaHealthManager>();
                    healthManager.RemoveVemon();
                    ArenaPlayerMovement movement = character.GetComponent<ArenaPlayerMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "EnemyGlad")
                {
                    ArenaHealthManager healthManager = character.GetComponent<ArenaHealthManager>();
                    healthManager.RemoveVemon();
                    ArenaEnemyPlayerMovement movement = character.GetComponent<ArenaEnemyPlayerMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "Pet1" || character.tag == "Pet2" || character.tag == "Pet3")
                {
                    ArenaHealthManager healthManager = character.GetComponent<ArenaHealthManager>();
                    healthManager.RemoveVemon();
                    ArenaPetMovement movement = character.GetComponent<ArenaPetMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "EnemyPet1" || character.tag == "EnemyPet2" || character.tag == "EnemyPet3")
                {
                    ArenaHealthManager healthManager = character.GetComponent<ArenaHealthManager>();
                    healthManager.RemoveVemon();
                    ArenaEnemyPetMovement movement = character.GetComponent<ArenaEnemyPetMovement>();
                    movement.RemoveStun();
                }
            }
            if (roundsCount > 0)
            {
                if (Inventory.Instance.HasSkill("LifeBlood"))
                {
                    skillBattleHandler.LifeBlood(playerObject);
                }
                if (EnemyInventory.Instance.HasSkill("LifeBlood"))
                {
                    enemyGladSkillBattleHandler.LifeBlood(enemyGladiatorObject);
                }
            }

            startingCharacter = DetermineHighestRoll();

            // Switch-sats för att hantera attackerande karaktärer
            Debug.Log(startingCharacter);
            switch (startingCharacter)
            {
                case CharacterType.Player:

                    HandlePlayerTurn();
                    break;

                case CharacterType.EnemyGlad:

                    HandleEnemyGladTurn();
                    break;
                case CharacterType.EnemyPet1:
                case CharacterType.EnemyPet2:
                case CharacterType.EnemyPet3:
                    int enemyPetIndex = (int)startingCharacter - 3;
                    if (enemyPetIndex < enemyPetObjects.Count)
                    {

                        HandleEnemyPetTurn(enemyPetIndex);
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
        playerObject, enemyGladiatorObject
    };
        allCharacters.AddRange(enemyPetObjects);
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
        if (character == enemyGladiatorObject)
        {
            return enemyGladiatorInitialScale; // Använd den ursprungliga skalan för spelaren
        }
        else if (enemyPetObjects.Contains(character))
        {
            return enemyPetInitialScale; // Använd den ursprungliga skalan för fiender
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
    public int RollNumberEnemyGlad()
    {
        int randomNumber = Random.Range(0, 100); //Rolls higher ddepending on lvl and initiative

        return randomNumber;
    }

    public int RollNumberEnemyPet()
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
        ArenaPlayerMovement playerMovement = playerObject.GetComponent<ArenaPlayerMovement>();
        if (playerMovement != null && !playerMovement.IsStunned)
        {
            rolls.Add(CharacterType.Player, RollNumberPlayer());
        }
        ArenaEnemyPlayerMovement enemyGladMovement = enemyGladiatorObject.GetComponent<ArenaEnemyPlayerMovement>();
        if (enemyGladMovement != null && !enemyGladMovement.IsStunned)
        {
            rolls.Add(CharacterType.EnemyGlad, RollNumberPlayer());
        }

        // Hantera fiender och ignorera de som är döda eller stunned
        for (int i = 0; i < enemyPetObjects.Count; i++)
        {
            ArenaHealthManager enemyPetHealth = enemyPetObjects[i].GetComponent<ArenaHealthManager>();
            ArenaEnemyPetMovement enemyPetMovement = enemyPetObjects[i].GetComponent<ArenaEnemyPetMovement>();
            if (enemyPetHealth != null && !enemyPetHealth.IsDead && enemyPetMovement != null && !enemyPetMovement.IsStunned)
            {
                rolls.Add((CharacterType)(3 + i), RollNumberEnemyPet());
            }
        }

        // Hantera husdjur och ignorera de som är döda eller stunned
        for (int i = 0; i < petObjects.Count; i++)
        {
            ArenaHealthManager petHealth = petObjects[i].GetComponent<ArenaHealthManager>();
            ArenaPetMovement petMovement = petObjects[i].GetComponent<ArenaPetMovement>();
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
    private void HandleEnemyPetTurn(int petIndex)
    {
        enemyPetMovements[petIndex].IsMoving = true;
        StartCoroutine(EnemyPetAttack(petIndex));
    }
    private IEnumerator EnemyPetAttack(int petIndex)
    {
        yield return new WaitForSeconds(0.5f);
        enemyPetMovements[petIndex].IsMoving = false;
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
                ArenaHealthManager playerHealthManager = playerObject.GetComponent<ArenaHealthManager>();
                int currentHealth = playerHealthManager.CurrentHealth;
                if (currentHealth < CharacterData.Instance.Health / 3)
                {
                    playerMovement.berserkDamage = skillBattleHandler.Berserk(playerObject);
                }
            }

            int randomNumber = RollNumberPlayer();

            PlayerAction action = DeterminePlayerAction(randomNumber);

            Debug.Log(action);
            switch (action)
            {
                case PlayerAction.UseConsumable:
                    if (inventoryBattleHandler.GetCombatConsumableInventory().Count > 0)
                    {
                        StartCoroutine(inventoryBattleHandler.UseConsumable(0.6f));
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
    private void HandleEnemyGladTurn()
    {
        if (!enemyGladMovement.RollForEnemy())
        {
            GameOver("Enemy");
        }
        else
        {
            if (EnemyInventory.Instance.HasSkill("Berserk") && !enemyGladSkillBattleHandler.berserkUsed)
            {
                ArenaHealthManager enemyGladHealthManager = enemyGladiatorObject.GetComponent<ArenaHealthManager>();
                int currentHealth = enemyGladHealthManager.CurrentHealth;
                if (currentHealth < EnemyGladiatorData.Instance.Health / 3)
                {
                    enemyGladMovement.berserkDamage = enemyGladSkillBattleHandler.Berserk(enemyGladiatorObject);
                }
            }

            int randomNumber = RollNumberPlayer();

            PlayerAction action = DeterminePlayerAction(randomNumber);


            switch (action)
            {
                case PlayerAction.UseConsumable:
                    if (enemyGladInventoryBattleHandler.GetCombatConsumableInventory().Count > 0)
                    {
                        StartCoroutine(enemyGladInventoryBattleHandler.UseConsumable(0.6f));
                    }
                    else
                    {
                        StartCoroutine(EnemyGladAttack());
                    }
                    break;

                case PlayerAction.EquipWeapon:
                    if (enemyGladInventoryBattleHandler.GetCombatWeaponInventory().Count > 0 && !enemyGladInventoryBattleHandler.IsWeaponEquipped)
                    {
                        StartCoroutine(enemyGladInventoryBattleHandler.EquipWeaponAndStartMoving(0.2f));
                    }
                    else
                    {
                        StartCoroutine(EnemyGladAttack());
                    }
                    break;

                case PlayerAction.Attack:
                    StartCoroutine(EnemyGladAttack());
                    break;

                default:

                    break;
            }

        }
    }

    private PlayerAction DeterminePlayerAction(int roll)
    {
        if (roll >= 70 && inventoryBattleHandler.GetCombatConsumableInventory().Count > 0)
        {
            return PlayerAction.UseConsumable;
        }
        else if (roll < 70 && inventoryBattleHandler.GetCombatWeaponInventory().Count > 0 && !inventoryBattleHandler.IsWeaponEquipped)
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
    private IEnumerator EnemyGladAttack()
    {
        enemyGladMovement.IsMoving = true;
        yield return new WaitForSeconds(0.5f);
        enemyGladMovement.IsMoving = false;
    }

    public void GameOver(string winnerTag)
    {

        if (!isGameOver)
        {
            isGameOver = true;
            inventoryBattleHandler.DestroyWeapon();
            List<string> listOfNames = new List<string>(); // Skapa en lista för att lagra monster-namnen

            // Lägg till namnen på besegrade monster i listan
            foreach (GameObject enemy in GetEnemies())
            {
                if (enemy.tag == "EnemyPet1" || enemy.tag == "EnemyPet2" || enemy.tag == "EnemyPet3")
                {
                    MonsterStats statsFromDeadEnemy = enemy.GetComponent<MonsterStats>();
                    listOfNames.Add(statsFromDeadEnemy.MonsterName);
                }
                else if (enemy.tag == "EnemyGlad")
                {
                    listOfNames.Add(EnemyGladiatorData.Instance.CharName);
                }


                // Lägg till namnet i listan
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
            ArenaManager.Instance.Cleanup();
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
        if (tag == "EnemyGlad")
        {
            enemyGladMovement.IsMoving = false;
            GameOver("Player");
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
        foreach (Transform child in enemyWeaponInventorySlotParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in enemyConsumableInventorySlotParent)
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
        foreach (Item item in enemyGladInventoryBattleHandler.GetCombatWeaponInventory())
        {
            // Instantiate a new inventory slot from the prefab
            GameObject newSlot = Instantiate(inventorySlotPrefab, enemyWeaponInventorySlotParent);

            // Get the Image component from the slot
            Image slotImage = newSlot.GetComponent<Image>();

            // Set the sprite of the image to the item's sprite
            slotImage.sprite = item.itemSprite;
        }
        foreach (Item item in enemyGladInventoryBattleHandler.GetCombatConsumableInventory())
        {
            // Instantiate a new inventory slot from the prefab
            GameObject newSlot = Instantiate(inventorySlotPrefab, enemyConsumableInventorySlotParent);

            // Get the Image component from the slot
            Image slotImage = newSlot.GetComponent<Image>();

            // Set the sprite of the image to the item's sprite
            slotImage.sprite = item.itemSprite;
        }
    }

    private IEnumerator DelayedSceneLoad()
    {
        BackgroundMusicManager.Instance.FadeOutMusic(3f);
        yield return new WaitForSeconds(4f); // Wait for 2 seconds
        sceneController.LoadScene("Base");
    }
}

