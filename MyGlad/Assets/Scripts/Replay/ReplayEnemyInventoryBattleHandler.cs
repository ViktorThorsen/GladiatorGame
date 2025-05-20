using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ReplayEnemyInventoryBattleHandler : MonoBehaviour
{
    private ReplayGameManager gameManager;
    private GameObject thisUnit;
    private Transform mainHandSocket;
    private Transform consumableSocket;
    public bool IsWeaponEquipped;
    private ReplayEnemyPlayerMovement playerMovement;
    public Item currentWeapon;
    private Animator anim;

    private ReplayHealthManager playerHealthManager;

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

        gameManager = FindObjectOfType<ReplayGameManager>();
        ResetCombatInventory();
        IsWeaponEquipped = false;
        thisUnit = gameObject;
        playerMovement = thisUnit.GetComponent<ReplayEnemyPlayerMovement>();
        playerHealthManager = thisUnit.GetComponent<ReplayHealthManager>();
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
        // Hämta vapennamn och konsumabelnamn från replaydatan
        List<string> weaponNames = ReplayManager.Instance.selectedReplay.enemy.weapons.weaponNames;
        List<string> consumableNames = ReplayManager.Instance.selectedReplay.enemy.consumables.consumableNames;

        // Bygg upp vapeninventariet
        weaponInventory = new List<Item>();
        foreach (string name in weaponNames)
        {
            Item weapon = gameManager.itemDataBase.GetWeaponByName(name);
            if (weapon != null)
                weaponInventory.Add(weapon);
        }

        // Bygg upp konsumabelinventariet
        consumableInventory = new List<Item>();
        foreach (string name in consumableNames)
        {
            Item consumable = gameManager.itemDataBase.GetConsumableByName(name);
            if (consumable != null)
                consumableInventory.Add(consumable);
        }
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


        // Kontrollera 25%
        if (!equipedFromShortcut2 && playerHealthManager.CurrentHealth <= playerHealthManager.maxHealth * 0.25f)
        {
            (itemToEquip, indexToRemove) = GetWeaponFromReplayShortcut(2);
            equipedFromShortcut2 = true;
        }
        // Kontrollera 50%
        else if (!equipedFromShortcut1 && playerHealthManager.CurrentHealth <= playerHealthManager.maxHealth * 0.5f)
        {
            (itemToEquip, indexToRemove) = GetWeaponFromReplayShortcut(1);
            equipedFromShortcut1 = true;
        }
        // Kontrollera 75%
        else if (!equipedFromShortcut0 && playerHealthManager.CurrentHealth <= playerHealthManager.maxHealth * 0.75f)
        {
            (itemToEquip, indexToRemove) = GetWeaponFromReplayShortcut(0);
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
                ReplayEnemyGladData.Instance.AddEquipStats(
                    currentWeapon.strength,
                    currentWeapon.agility,
                    currentWeapon.intellect,
                    currentWeapon.health,
                    currentWeapon.hit,
                    currentWeapon.defense,
                    0,
                    currentWeapon.stunRate,
                    currentWeapon.lifesteal);
                IsWeaponEquipped = true;
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
            ReplayCharacterData.Instance.RemoveEquipStats(
                    currentWeapon.strength,
                    currentWeapon.agility,
                    currentWeapon.intellect,
                    currentWeapon.health,
                    currentWeapon.hit,
                    currentWeapon.defense,
                    0,
                    currentWeapon.stunRate,
                    currentWeapon.lifesteal);
            currentWeapon = null;
            IsWeaponEquipped = false;
        }
        else
        {

        }
    }

    private (Item item, int index) GetWeaponFromReplayShortcut(int shortcutSlot)
    {
        var replay = ReplayManager.Instance.selectedReplay;
        int currentTurn = ReplayGameManager.Instance.RoundsCount;

        // Leta upp rätt action där vapnet utrustades och shortcut-slotten matchar
        var weaponEquipAction = replay.actions
            .Where(a => a.Turn == currentTurn &&
                        a.Action == "WeaponEquipped" &&
                        a.Actor == CharacterType.EnemyGlad) // eller Player beroende på vem det gäller
            .FirstOrDefault();

        if (weaponEquipAction == null)
        {
            Debug.LogWarning("❌ Ingen WeaponEquipped-action hittades i denna rundan.");
            return (null, -1);
        }

        int index = weaponEquipAction.Value;

        if (index < 0 || index >= weaponInventory.Count || weaponInventory[index] == null)
        {
            Debug.LogWarning($"❌ Ogiltigt vapenindex ({index}) i WeaponInventory.");
            return (null, -1);
        }

        return (weaponInventory[index], index);
    }
}