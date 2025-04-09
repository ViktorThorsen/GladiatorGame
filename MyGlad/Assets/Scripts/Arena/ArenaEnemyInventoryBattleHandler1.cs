using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (weaponInventory.Count == 0)
        {
            playerMovement.IsMoving = true;
            yield break;
        }

        // Get a random item from the combat inventory
        int randomIndex = Random.Range(0, weaponInventory.Count);
        Item itemToEquip = weaponInventory[randomIndex];



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
                    currentWeapon.attackDamage,
                    currentWeapon.dodgeRate,
                    currentWeapon.critRate,
                    currentWeapon.stunRate);
                IsWeaponEquipped = true;

                // Remove the item from the combat inventory to mark it as used
                weaponInventory.RemoveAt(randomIndex);
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
                    currentWeapon.attackDamage,
                    currentWeapon.dodgeRate,
                    currentWeapon.critRate,
                    currentWeapon.stunRate);
            currentWeapon = null;
            IsWeaponEquipped = false;
        }
        else
        {

        }
    }
}