using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ArenaEnemyInventoryBattleHandler : MonoBehaviour
{
    private ArenaGameManager gameManager;
    private GameObject thisUnit;
    private Transform mainHandSocket;
    private Transform consumableSocket;
    public bool IsWeaponEquipped;
    private ArenaEnemyPlayerMovement playerMovement;
    public Item currentWeapon;
    private Animator anim;

    private ArenaHealthManager playerHealthManager;

    // Local list to keep track of available items during combat
    private List<Item> weaponInventory;
    private List<Item> consumableInventory;

    private bool equipedFromShortcut0 = false;
    private bool equipedFromShortcut1 = false;
    private bool equipedFromShortcut2 = false;
    private bool equipedFromShortcut3 = false;


    public List<Item> GetCombatWeaponInventory()
    {
        return weaponInventory;
    }

    public List<Item> GetCombatConsumableInventory()
    {
        return consumableInventory;
    }


    void Start()
    {
        ResetCombatInventory();
        gameManager = FindObjectOfType<ArenaGameManager>();
        IsWeaponEquipped = false;
        thisUnit = gameObject;
        playerMovement = thisUnit.GetComponent<ArenaEnemyPlayerMovement>();
        playerHealthManager = thisUnit.GetComponent<ArenaHealthManager>();
        anim = GetComponent<Animator>();

        // Find the MainHandSocket in your character's hierarchy
        mainHandSocket = FindChildByName(thisUnit.transform, "MainHandSocket");
        consumableSocket = FindChildByName(thisUnit.transform, "ConsumableSocket");
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void ResetCombatInventory()
    {
        // Create a copy of the inventory items
        weaponInventory = new List<Item>(EnemyInventory.Instance.GetWeapons());
        consumableInventory = new List<Item>(EnemyInventory.Instance.GetConsumables());
    }


    public IEnumerator UseConsumable(float delay)
    {
        if (consumableInventory.Count == 0)
        {
            playerMovement.IsMoving = true;
            yield break;
        }
        int randomIndex = Random.Range(0, consumableInventory.Count);
        Item itemToConsume = consumableInventory[randomIndex];
        if (itemToConsume != null && itemToConsume.itemSprite != null)
        {
            SpriteRenderer consumableSpriteRenderer = consumableSocket.GetComponent<SpriteRenderer>();
            if (consumableSpriteRenderer != null)
            {
                consumableSpriteRenderer.sprite = itemToConsume.itemSprite;
                consumableSocket.localPosition = itemToConsume.equippedPositionOffset;
                consumableSocket.localScale = itemToConsume.equippedScale;
                consumableSocket.localEulerAngles = itemToConsume.equippedRotation;
                consumableInventory.RemoveAt(randomIndex);
                ReplayData.Instance.AddAction(new MatchEventDTO
                {
                    Turn = gameManager.RoundsCount,
                    Actor = CharacterType.EnemyGlad,
                    Action = "ConsumableUsed",
                    Target = CharacterType.EnemyGlad,
                    Value = randomIndex
                });
                gameManager.UpdateBattleInventorySlots();
                if (itemToConsume.abilityType == abilityType.heal)
                {
                    playerHealthManager.IncreaseHealth(itemToConsume.healthRestorationAmount);
                }
            }
        }
        anim.SetTrigger("useconsumable");
        yield return new WaitForSeconds(delay);
        DestroyConsumable();
        playerMovement.IsMoving = true;

    }
    public IEnumerator EquipWeaponAndStartMoving(float delay)
    {
        // Trigger the equip weapon animation
        anim.SetTrigger("equipweapon");

        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Check if there are any available items in the combat inventory
        if (weaponInventory.TrueForAll(w => w == null))
        {
            playerMovement.IsMoving = true;
            yield break;
        }

        Item itemToEquip = null;
        int indexToRemove = -1;


        // Kontrollera lägst tröskel först (25%)
        if (!equipedFromShortcut2 && playerHealthManager.CurrentHealth <= playerHealthManager.maxHealth * 0.25f)
        {
            int shortcutWeaponIndex = EnemyInventory.Instance.shortcutWeaponIndexes.FindIndex(slot => slot == 2);
            if (shortcutWeaponIndex != -1 && shortcutWeaponIndex < weaponInventory.Count)
            {
                if (weaponInventory[shortcutWeaponIndex] != null)
                {
                    itemToEquip = weaponInventory[shortcutWeaponIndex];
                    indexToRemove = shortcutWeaponIndex;
                    ReplayData.Instance.AddAction(new MatchEventDTO
                    {
                        Turn = gameManager.RoundsCount,
                        Actor = CharacterType.EnemyGlad,
                        Action = "WeaponEquipped",
                        Target = CharacterType.EnemyGlad,
                        Value = indexToRemove
                    });
                }
            }
            equipedFromShortcut2 = true;
        }
        // Kontrollera 50%
        else if (!equipedFromShortcut1 && playerHealthManager.CurrentHealth <= playerHealthManager.maxHealth * 0.5f)
        {
            int shortcutWeaponIndex = EnemyInventory.Instance.shortcutWeaponIndexes.FindIndex(slot => slot == 1);
            if (shortcutWeaponIndex != -1 && shortcutWeaponIndex < weaponInventory.Count)
            {
                if (weaponInventory[shortcutWeaponIndex] != null)
                {
                    itemToEquip = weaponInventory[shortcutWeaponIndex];
                    indexToRemove = shortcutWeaponIndex;
                    ReplayData.Instance.AddAction(new MatchEventDTO
                    {
                        Turn = gameManager.RoundsCount,
                        Actor = CharacterType.EnemyGlad,
                        Action = "WeaponEquipped",
                        Target = CharacterType.EnemyGlad,
                        Value = indexToRemove
                    });
                }
            }
            equipedFromShortcut1 = true;
        }
        // Kontrollera 75%
        else if (!equipedFromShortcut0 && playerHealthManager.CurrentHealth <= playerHealthManager.maxHealth * 0.75f)
        {
            int shortcutWeaponIndex = EnemyInventory.Instance.shortcutWeaponIndexes.FindIndex(slot => slot == 0);
            if (shortcutWeaponIndex != -1 && shortcutWeaponIndex < weaponInventory.Count)
            {
                if (weaponInventory[shortcutWeaponIndex] != null)
                {
                    itemToEquip = weaponInventory[shortcutWeaponIndex];
                    indexToRemove = shortcutWeaponIndex;
                    ReplayData.Instance.AddAction(new MatchEventDTO
                    {
                        Turn = gameManager.RoundsCount,
                        Actor = CharacterType.EnemyGlad,
                        Action = "WeaponEquipped",
                        Target = CharacterType.EnemyGlad,
                        Value = indexToRemove
                    });
                }
            }
            equipedFromShortcut0 = true;
        }

        if (itemToEquip == null)
        {
            for (int i = 0; i < weaponInventory.Count; i++)
            {
                if (weaponInventory[i] != null)
                {
                    itemToEquip = weaponInventory[i];
                    indexToRemove = i;
                    ReplayData.Instance.AddAction(new MatchEventDTO
                    {
                        Turn = gameManager.RoundsCount,
                        Actor = CharacterType.EnemyGlad,
                        Action = "WeaponEquipped",
                        Target = CharacterType.EnemyGlad,
                        Value = i
                    });
                    break;
                }
            }
        }


        // Check if the item has a sprite
        if (itemToEquip != null && itemToEquip.itemSprite != null)
        {
            // Get the SpriteRenderer on the MainHandSocket
            SpriteRenderer handSpriteRenderer = mainHandSocket.GetComponent<SpriteRenderer>();

            // Set the sprite to the item's sprite
            if (handSpriteRenderer != null)
            {
                handSpriteRenderer.sprite = itemToEquip.itemSprite;

                mainHandSocket.localPosition = itemToEquip.equippedPositionOffset;
                mainHandSocket.localScale = itemToEquip.equippedScale;
                mainHandSocket.localEulerAngles = itemToEquip.equippedRotation;
                currentWeapon = itemToEquip;
                EnemyGladiatorData.Instance.AddEquipStats(
                    currentWeapon.strength,
                    currentWeapon.agility,
                    currentWeapon.intellect,
                    currentWeapon.health,
                    currentWeapon.hit,
                    currentWeapon.defense,
                    0,
                    currentWeapon.stunRate,
                    currentWeapon.lifesteal,
                    currentWeapon.initiative, currentWeapon.combo);
                IsWeaponEquipped = true;

                // Remove the item from the combat inventory to mark it as used
                weaponInventory[indexToRemove] = null;
                gameManager.UpdateBattleInventorySlots();
            }
            else
            {
            }
        }
        else
        {
        }

        playerMovement.IsMoving = true;
    }
    public void DestroyConsumable()
    {
        // Get the SpriteRenderer on the MainHandSocket
        SpriteRenderer consumableSpriteRenderer = consumableSocket.GetComponent<SpriteRenderer>();

        // If the SpriteRenderer exists and has a sprite equipped
        if (consumableSpriteRenderer != null && consumableSpriteRenderer.sprite != null)
        {
            // Clear the sprite to "destroy" the weapon visually
            consumableSpriteRenderer.sprite = null;


        }
        else
        {

        }
    }

    public void DestroyWeapon()
    {
        // Get the SpriteRenderer on the MainHandSocket
        SpriteRenderer handSpriteRenderer = mainHandSocket.GetComponent<SpriteRenderer>();

        // If the SpriteRenderer exists and has a sprite equipped
        if (handSpriteRenderer != null && handSpriteRenderer.sprite != null)
        {
            // Clear the sprite to "destroy" the weapon visually
            handSpriteRenderer.sprite = null;


            EnemyGladiatorData.Instance.RemoveEquipStats(currentWeapon.strength,
                    currentWeapon.agility,
                    currentWeapon.intellect,
                    currentWeapon.health,
                    currentWeapon.hit,
                    currentWeapon.defense,
                    0,
                    currentWeapon.stunRate,
                    currentWeapon.lifesteal,
                    currentWeapon.initiative, currentWeapon.combo);
            currentWeapon = null;
            IsWeaponEquipped = false;
        }
        else
        {

        }
    }

    public void DisarmWeaponsBySkill()
    {
        var disarmSkill = Inventory.Instance.GetSkills()
            .FirstOrDefault(s => s.skillName == "Disarm");

        if (disarmSkill == null)
        {
            Debug.Log(" Disarm-skill saknas.");
            return;
        }

        Skill disarmSkillData = disarmSkill.GetSkillData();
        int level = disarmSkill.level;

        int weaponsToRemove = level switch
        {
            1 => disarmSkillData.effectPercentIncreaseLevel1,
            2 => disarmSkillData.effectPercentIncreaseLevel2,
            3 => disarmSkillData.effectPercentIncreaseLevel3,
            _ => 1
        };

        int removed = 0;
        for (int i = 0; i < weaponInventory.Count && removed < weaponsToRemove; i++)
        {
            if (weaponInventory[i] != null)
            {
                weaponInventory[i] = null;
                removed++;
            }
        }

        Debug.Log($" Disarmed {removed} weapon(s) via skill level {level}");
        gameManager.UpdateBattleInventorySlots();
    }
}