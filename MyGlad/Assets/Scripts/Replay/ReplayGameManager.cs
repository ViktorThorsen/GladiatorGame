using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class ReplayGameManager : MonoBehaviour
{
    public static ReplayGameManager Instance { get; private set; }
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

    [SerializeField] private PetDataBase petDB;
    [SerializeField] public ItemDataBase itemDataBase;

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

    ReplayPlayerMovement playerMovement;
    ReplayEnemyPlayerMovement enemyGladMovement;
    ReplayInventoryBattleHandler inventoryBattleHandler;
    ReplayEnemyInventoryBattleHandler enemyGladInventoryBattleHandler;
    SkillBattleHandler skillBattleHandler;
    SkillBattleHandler enemyGladSkillBattleHandler;
    private List<ReplayEnemyPetMovement> enemyPetMovements = new List<ReplayEnemyPetMovement>();
    private List<ReplayPetMovement> petMovements = new List<ReplayPetMovement>();
    private bool initialDelayCompleted;
    private bool rollTime;
    private bool isGameOver;
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
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        BattleBackground battlebackgroundImage = backgroundImageDataBase.GetBattleBackgroundByName(ReplayManager.Instance.selectedReplay.mapName);
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
        ReplayCharacterData.Instance.LoadStatsFromReplayDTO();
        ReplayEnemyGladData.Instance.LoadStatsFromReplayDTO();
        // Randomly spawn player and enemy at available positions
        playerObject = SpawnPlayerAtRandomPosition();
        enemyGladiatorObject = SpawnEnemyGladiatorAtRandomPosition();
        foreach (string petName in ReplayManager.Instance.selectedReplay.enemy.pets.petNames)
        {
            GameObject enemyPet = petDB.GetPetByName(petName);
            if (enemyPet != null)
                enemyPetObjects.Add(SpawnEnemyPetAtRandomPosition(enemyPet));
        }

        foreach (string petName in ReplayManager.Instance.selectedReplay.player.pets.petNames)
        {
            GameObject pet = petDB.GetPetByName(petName);
            if (pet != null)
                petObjects.Add(SpawnPetAtRandomPosition(pet));
        }

        // Store their initial scales
        if (playerObject != null) playerInitialScale = playerObject.transform.localScale;
        if (enemyGladiatorObject != null) enemyGladiatorInitialScale = enemyGladiatorObject.transform.localScale;

        if (ReplayManager.Instance.selectedReplay.enemy.pets.petNames.Count > 0)
        {
            for (int i = 0; i < enemyPetObjects.Count; i++)
            {
                if (enemyPetObjects[i] != null)
                    enemyPetInitialScale = enemyPetObjects[i].transform.localScale;
            }
        }

        if (ReplayManager.Instance.selectedReplay.player.pets.petNames.Count > 0)
        {
            for (int i = 0; i < petObjects.Count; i++)
            {
                if (petObjects[i] != null)
                    petInitialScale = petObjects[i].transform.localScale;
            }
        }
        AdjustAllSortingLayersAndScale();

        GameObject SpawnPlayerAtRandomPosition()
        {
            int randomIndex = GetRandomAvailablePosition(leftPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = playerStartPositions[randomIndex];

                GameObject characterObject = CharacterManager.InstantiateReplayCharacter(
                    ReplayManager.Instance.selectedReplay.player,
                    characterPrefab,
                    parentObj,
                    spawnPosition,
                    new Vector3(0.5f, 0.5f, 1f)
                );

                characterObject.tag = "Player";
                characterObject.AddComponent<ReplayHealthManager>();
                characterObject.AddComponent<ReplayPlayerMovement>();
                characterObject.AddComponent<ReplayInventoryBattleHandler>();
                characterObject.AddComponent<SkillBattleHandler>();

                skillBattleHandler = characterObject.GetComponent<SkillBattleHandler>();
                inventoryBattleHandler = characterObject.GetComponent<ReplayInventoryBattleHandler>();
                playerMovement = characterObject.GetComponent<ReplayPlayerMovement>();

                leftPositionsAvailable[randomIndex] = false;
                playerMovement.positionIndex = randomIndex;

                return characterObject;
            }
            return null;
        }

        GameObject SpawnEnemyGladiatorAtRandomPosition()
        {
            int randomIndex = GetRandomAvailablePosition(rightPositionsAvailable);
            if (randomIndex != -1)
            {
                Transform spawnPosition = enemyStartPositions[randomIndex];
                GameObject characterObject = CharacterManager.InstantiateReplayCharacter(
                    ReplayManager.Instance.selectedReplay.enemy,
                    characterPrefab,
                    parentObj,
                    spawnPosition,
                    new Vector3(-0.5f, 0.5f, 1f)
                );

                characterObject.tag = "EnemyGlad";
                characterObject.AddComponent<ReplayHealthManager>();
                characterObject.AddComponent<ReplayEnemyPlayerMovement>();
                characterObject.AddComponent<ReplayEnemyInventoryBattleHandler>();
                characterObject.AddComponent<SkillBattleHandler>();

                enemyGladSkillBattleHandler = characterObject.GetComponent<SkillBattleHandler>();
                enemyGladInventoryBattleHandler = characterObject.GetComponent<ReplayEnemyInventoryBattleHandler>();
                enemyGladMovement = characterObject.GetComponent<ReplayEnemyPlayerMovement>();

                rightPositionsAvailable[randomIndex] = false;
                enemyGladMovement.positionIndex = randomIndex;

                return characterObject;
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
                petObject.AddComponent<ReplayHealthManager>();
                petObject.AddComponent<ReplayPetMovement>();
                petObject.AddComponent<SkillBattleHandler>();
                petMovements.Add(petObject.GetComponent<ReplayPetMovement>());
                // Mark this position as occupied
                leftPositionsAvailable[randomIndex] = false;
                ReplayPetMovement petMov = petObject.GetComponent<ReplayPetMovement>();
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
                enemyPetObject.AddComponent<ReplayHealthManager>();
                enemyPetObject.AddComponent<ReplayEnemyPetMovement>();
                enemyPetObject.AddComponent<SkillBattleHandler>();
                enemyPetMovements.Add(enemyPetObject.GetComponent<ReplayEnemyPetMovement>());
                // Mark this position as occupied
                rightPositionsAvailable[randomIndex] = false;
                ReplayEnemyPetMovement petMov = enemyPetObject.GetComponent<ReplayEnemyPetMovement>();
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
                    ReplayHealthManager healthManager = character.GetComponent<ReplayHealthManager>();
                    healthManager.RemoveVemon();
                    ReplayPlayerMovement movement = character.GetComponent<ReplayPlayerMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "EnemyGlad")
                {
                    ReplayHealthManager healthManager = character.GetComponent<ReplayHealthManager>();
                    healthManager.RemoveVemon();
                    ReplayEnemyPlayerMovement movement = character.GetComponent<ReplayEnemyPlayerMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "Pet1" || character.tag == "Pet2" || character.tag == "Pet3")
                {
                    ReplayHealthManager healthManager = character.GetComponent<ReplayHealthManager>();
                    healthManager.RemoveVemon();
                    ReplayPetMovement movement = character.GetComponent<ReplayPetMovement>();
                    movement.RemoveStun();
                }
                else if (character.tag == "EnemyPet1" || character.tag == "EnemyPet2" || character.tag == "EnemyPet3")
                {
                    ReplayHealthManager healthManager = character.GetComponent<ReplayHealthManager>();
                    healthManager.RemoveVemon();
                    ReplayEnemyPetMovement movement = character.GetComponent<ReplayEnemyPetMovement>();
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

    public CharacterType DetermineHighestRoll()
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = roundsCount + 1;

        var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

        if (actionsThisTurn.Count == 0)
        {
            Debug.LogWarning($" No actions found for turn {currentTurn} in replay.");
            return CharacterType.None;
        }

        // Försök hitta första attack eller crit
        var firstAttack = actionsThisTurn
            .FirstOrDefault(a => a.Action == "attack" || a.Action == "crit" || a.Action == "ConsumableUsed" || a.Action == "WeaponEquipped");

        if (firstAttack != null)
        {
            roundsCount++;
            rollTime = false;
            return firstAttack.Actor;
        }

        // Om ingen attack eller crit finns – leta efter dodge
        var firstDodge = actionsThisTurn
            .FirstOrDefault(a => a.Action == "dodge");

        if (firstDodge != null)
        {
            roundsCount++;
            rollTime = false;
            // Dodge.target är den som slog → alltså startaren av rundan
            return firstDodge.Target;
        }

        Debug.LogWarning($" No attack/crit/dodge actions found in turn {currentTurn}");
        return CharacterType.None;
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
        var replay = ReplayManager.Instance.selectedReplay;

        // Kolla om vi är klara (dvs. om roundsCount överskrider det sista turn-numret)
        int maxTurn = replay.actions.Max(a => a.Turn);
        if (roundsCount > maxTurn)
        {
            Debug.Log("▶️ Replay avslutad.");
            GameOver("Player");
            return;
        }
        else
        {
            if (replay.player.skills.skillNames.Contains("Berserk") && !skillBattleHandler.berserkUsed)
            {
                ReplayHealthManager playerHealthManager = playerObject.GetComponent<ReplayHealthManager>();
                int currentHealth = playerHealthManager.CurrentHealth;

                // Använd snapshot-värdet från replayn
                int maxHealth = replay.player.character.health;

                if (currentHealth < maxHealth / 3)
                {
                    playerMovement.berserkDamage = skillBattleHandler.Berserk(playerObject);
                }
            }
            var currentTurn = roundsCount;
            var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

            // Hitta spelarens första action denna runda
            var playerAction = actionsThisTurn
                .FirstOrDefault(a => a.Actor == CharacterType.Player);

            if (playerAction == null)
            {
                Debug.LogWarning("❌ Inga actions för spelaren denna runda.");
                return;
            }

            Debug.Log($"▶ Replay Player Action: {playerAction.Action}");

            switch (playerAction.Action)
            {
                case "ConsumableUsed":
                    StartCoroutine(inventoryBattleHandler.UseConsumable(0.6f));
                    break;

                case "WeaponEquipped":
                    StartCoroutine(inventoryBattleHandler.EquipWeaponAndStartMoving(0.2f));
                    break;

                case "attack":
                    StartCoroutine(PlayerAttack());
                    break;
                case "crit":
                    StartCoroutine(PlayerAttack());
                    break;
                case "dodge":
                    StartCoroutine(PlayerAttack());
                    break;
            }

        }
    }
    private void HandleEnemyGladTurn()
    {
        var replay = ReplayManager.Instance.selectedReplay;

        // Kolla om vi är klara (dvs. om roundsCount överskrider det sista turn-numret)
        int maxTurn = replay.actions.Max(a => a.Turn);
        if (roundsCount > maxTurn)
        {
            Debug.Log("▶️ Replay avslutad.");
            GameOver("EnemyGlad");
            return;
        }
        else
        {
            if (replay.enemy.skills.skillNames.Contains("Berserk") && !enemyGladSkillBattleHandler.berserkUsed)
            {
                ReplayHealthManager enemyPlayerHealthManager = enemyGladiatorObject.GetComponent<ReplayHealthManager>();
                int currentHealth = enemyPlayerHealthManager.CurrentHealth;

                // Använd snapshot-värdet från replayn
                int maxHealth = replay.enemy.character.health;

                if (currentHealth < maxHealth / 3)
                {
                    enemyGladMovement.berserkDamage = enemyGladSkillBattleHandler.Berserk(enemyGladiatorObject);
                }
            }
            var currentTurn = roundsCount;
            var actionsThisTurn = replay.actions
            .Where(a => a.Turn == currentTurn)
            .ToList();

            // Hitta spelarens första action denna runda
            var playerAction = actionsThisTurn
                .FirstOrDefault(a => a.Actor == CharacterType.EnemyGlad);

            if (playerAction == null)
            {
                Debug.LogWarning("❌ Inga actions för spelaren denna runda.");
                return;
            }

            Debug.Log($"▶ Replay Player Action: {playerAction.Action}");

            switch (playerAction.Action)
            {
                case "ConsumableUsed":
                    StartCoroutine(enemyGladInventoryBattleHandler.UseConsumable(0.6f));
                    break;

                case "WeaponEquipped":
                    StartCoroutine(enemyGladInventoryBattleHandler.EquipWeaponAndStartMoving(0.2f));
                    break;

                case "attack":
                    StartCoroutine(EnemyGladAttack());
                    break;
                case "crit":
                    StartCoroutine(EnemyGladAttack());
                    break;
                case "dodge":
                    StartCoroutine(PlayerAttack());
                    break;
            }

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
        foreach (Item item in enemyGladInventoryBattleHandler.GetCombatWeaponInventory())
        {
            // Instantiate a new inventory slot from the prefab
            GameObject newSlot = Instantiate(inventorySlotPrefab, enemyWeaponInventorySlotParent);

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

