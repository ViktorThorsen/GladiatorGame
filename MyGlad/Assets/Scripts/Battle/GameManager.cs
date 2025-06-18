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
using System.Linq;
using TMPro;

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

    [SerializeField] private float fixedOffsetFromLeft = 1f;
    [SerializeField] private float fixedOffsetFromRight = 1f;
    private List<GameObject> enemyObjects = new List<GameObject>();
    private List<GameObject> petObjects = new List<GameObject>();
    private GameObject playerObject;

    private Vector3 playerInitialScale;
    private Vector3 enemyInitialScale;
    private List<Vector3> petInitialScales = new List<Vector3>();

    PlayerMovement playerMovement;
    InventoryBattleHandler inventoryBattleHandler;
    SkillBattleHandler skillBattleHandler;
    private List<EnemyMovement> enemyMovements = new List<EnemyMovement>();
    private List<PetMovement> petMovements = new List<PetMovement>();
    private bool initialDelayCompleted;
    private bool rollTime;
    private bool isGameOver;
    [SerializeField] public GameObject healthSliderPrefab;
    [SerializeField] public GameObject pethealthSliderPrefab;

    [SerializeField] public TMP_Text NameText;
    [SerializeField] public TMP_Text EnemyNameText;


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
        AdjustStartPositionsToCameraEdges();
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
                if (petObjects[i] != null) petInitialScales.Add(petObjects[i].transform.localScale);
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
                    new Vector3(0.5f, 0.5f, 2f) // Set the desired scale
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
                NameText.text = CharacterData.Instance.CharName;
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
                EnemyNameText.text = stats.MonsterName;
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
        if (Inventory.Instance.HasSkill("BlessingOfDavid") && CheckStrength())
        {
            skillBattleHandler.AddDavidStats();
        }

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
            return playerInitialScale;

        else if (enemyObjects.Contains(character))
            return enemyInitialScale;

        else if (petObjects.Contains(character))
        {
            int index = petObjects.IndexOf(character);
            if (index >= 0 && index < petInitialScales.Count)
                return petInitialScales[index];
        }

        return Vector3.one;
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
    private void AdjustStartPositionsToCameraEdges()
    {
        float camHeight = 2f * Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        float leftEdge = Camera.main.transform.position.x - camWidth / 2f;
        float rightEdge = Camera.main.transform.position.x + camWidth / 2f;

        // Justera vänstra positioner (spelare/pets)
        for (int i = 0; i < playerStartPositions.Length; i++)
        {
            Vector3 pos = playerStartPositions[i].position;

            // Ju längre upp i listan (i=0), desto större offset
            float dynamicOffset = fixedOffsetFromLeft + (playerStartPositions.Length - i) * 0.5f;
            pos.x = leftEdge + dynamicOffset;

            playerStartPositions[i].position = pos;
        }

        // Justera högra positioner (fiender)
        for (int i = 0; i < enemyStartPositions.Length; i++)
        {
            Vector3 pos = enemyStartPositions[i].position;

            // Ju längre upp i listan (i=0), desto större offset
            float dynamicOffset = fixedOffsetFromRight + (enemyStartPositions.Length - i) * 0.5f;
            pos.x = rightEdge - dynamicOffset;

            enemyStartPositions[i].position = pos;
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
        HealthManager playerhealth = playerObject.GetComponent<HealthManager>();
        PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
        if (playerMovement != null && !playerMovement.IsStunned && !playerhealth.IsDead)
        {
            rolls.Add(CharacterType.Player, RollNumberPlayer() + CharacterData.Instance.initiative);
        }

        // Hantera fiender och ignorera de som är döda eller stunned
        for (int i = 0; i < enemyObjects.Count; i++)
        {
            HealthManager enemyHealth = enemyObjects[i].GetComponent<HealthManager>();
            EnemyMovement enemyMovement = enemyObjects[i].GetComponent<EnemyMovement>();
            MonsterStats enemyStats = enemyObjects[i].GetComponent<MonsterStats>();
            if (enemyHealth != null && !enemyHealth.IsDead && enemyMovement != null && !enemyMovement.IsStunned)
            {
                rolls.Add((CharacterType)(2 + i), RollNumberEnemy() + enemyStats.Initiative);
            }
        }

        // Hantera husdjur och ignorera de som är döda eller stunned
        for (int i = 0; i < petObjects.Count; i++)
        {
            HealthManager petHealth = petObjects[i].GetComponent<HealthManager>();
            PetMovement petMovement = petObjects[i].GetComponent<PetMovement>();
            MonsterStats petStats = petObjects[i].GetComponent<MonsterStats>();
            if (petHealth != null && !petHealth.IsDead && petMovement != null && !petMovement.IsStunned)
            {
                rolls.Add((CharacterType)(6 + i), RollNumberPet() + petStats.Initiative);
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
            if (Inventory.Instance.HasSkill("Berserk"))
            {
                HealthManager playerHealthManager = playerObject.GetComponent<HealthManager>();
                int currentHealth = playerHealthManager.CurrentHealth;
                if (currentHealth < CharacterData.Instance.Health / 3)
                {
                    playerMovement.berserkDamage = skillBattleHandler.Berserk(playerObject);
                }
                else { playerMovement.berserkDamage = 0; skillBattleHandler.EndBerserk(playerObject); }
            }

            int randomNumber = RollNumberPlayer();

            PlayerAction action = DeterminePlayerAction(randomNumber + CharacterData.Instance.Intellect);
            Debug.Log(randomNumber + CharacterData.Instance.Intellect);


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
        if (roll >= 50)
        {
            int randomNumber = Random.Range(1, 3); // 1 eller 2
            bool hasConsumable = inventoryBattleHandler.GetCombatConsumableInventory().Count > 0;
            bool hasWeaponToEquip = inventoryBattleHandler.GetCombatWeaponInventory().Any(w => w != null) &&
                                    !inventoryBattleHandler.IsWeaponEquipped;

            if (randomNumber == 1)
            {
                if (hasConsumable)
                    return PlayerAction.UseConsumable;
                else if (hasWeaponToEquip)
                    return PlayerAction.EquipWeapon;
            }
            else if (randomNumber == 2)
            {
                if (hasWeaponToEquip)
                    return PlayerAction.EquipWeapon;
                else if (hasConsumable)
                    return PlayerAction.UseConsumable;
            }

            // Om inget kan göras, så blir det attack
            return PlayerAction.Attack;
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
            if (Inventory.Instance.HasSkill("BlessingOfDavid") && CheckStrength())
            {
                skillBattleHandler.RemoveDavidStats();
            }

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
                FightData.Instance.AddFightResultLand(ChooseLandsManager.Instance.ChoosedLand);
                FightData.Instance.AddFightResultStage(MonsterHuntManager.Instance.selectedStage);
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

            if (item != null)
            {
                // Set the sprite of the image to the item's sprite
                slotImage.sprite = item.itemSprite;
            }
            else
            {
                // Inget vapen i denna slot, gör den t.ex. grå eller transparent
                slotImage.sprite = null;
                slotImage.color = new Color(1f, 1f, 1f, 0.3f); // halvtransparent grå
            }
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

    public bool CheckStrength()
    {
        int petcheckstrength = 0;
        foreach (GameObject pet in petObjects)
        {
            petcheckstrength += pet.GetComponent<MonsterStats>().AttackDamage;
        }

        int checkstrength = 0;
        foreach (GameObject enemy in enemyObjects)
        {
            checkstrength += enemy.GetComponent<MonsterStats>().AttackDamage;
        }
        return checkstrength > CharacterData.Instance.Strength + petcheckstrength;
    }
}

