using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public enum SlotType { Weapon, Consumable, Pet, Shortcut }

public class DropSlotHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public int slotIndex;
    public SlotType slotType;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        var dragHandler = droppedObject.GetComponent<ItemDragHandler>();
        if (dragHandler == null) return;

        Transform originalSlot = dragHandler.OriginalParent;
        var originalSlotHandler = originalSlot.GetComponent<DropSlotHandler>();

        // ✅ Hantera SHORTCUT separat
        if (slotType == SlotType.Shortcut)
        {
            var itemUI = droppedObject.GetComponent<ItemUI>();
            if (itemUI == null)
            {
                Debug.LogWarning("❌ Saknar ItemUI på objektet.");
                return;
            }

            // 🔍 Hitta index i weapon-listan
            int weaponIndex = Inventory.Instance.inventoryWeapons.FindIndex(w => w == itemUI.Item);
            if (weaponIndex == -1)
            {
                Debug.LogWarning("⚠️ Vapnet finns inte i inventory.");
                return;
            }

            // 🧹 Rensa tidigare innehåll i denna shortcutslot
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 🧱 Skapa visuell kopia
            GameObject clone = Instantiate(droppedObject, transform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localScale = Vector3.one;
            clone.transform.localRotation = Quaternion.identity;

            Destroy(clone.GetComponent<ItemDragHandler>());
            var cg = clone.GetComponent<CanvasGroup>();
            if (cg != null) Destroy(cg);

            // ✅ Uppdatera shortcut-indexlistan
            Inventory.Instance.AssignWeaponToShortcut(weaponIndex, slotIndex);

            Debug.Log($"✅ {itemUI.Item.itemName} tilldelad shortcut-slot {slotIndex} (index i weaponlist: {weaponIndex})");
            return;
        }

        // 🔁 SWAP för vanliga slots (Weapon/Consumable/Pet)
        if (originalSlotHandler == null) return;

        if (transform.childCount > 0)
        {
            Transform itemInTargetSlot = transform.GetChild(0);
            itemInTargetSlot.SetParent(originalSlot);
            itemInTargetSlot.localPosition = Vector3.zero;
        }

        droppedObject.transform.SetParent(transform);
        droppedObject.transform.localPosition = Vector3.zero;

        switch (slotType)
        {
            case SlotType.Weapon:
                Inventory.Instance.SwapWeapons(originalSlotHandler.slotIndex, slotIndex);
                break;
            case SlotType.Consumable:
                Inventory.Instance.SwapConsumables(originalSlotHandler.slotIndex, slotIndex);
                break;
            case SlotType.Pet:
                Inventory.Instance.SwapPets(originalSlotHandler.slotIndex, slotIndex);
                break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotType != SlotType.Shortcut) return;

        // Rensa visuellt
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Hitta vilket vapen som ligger i denna shortcut-slot
        int weaponIndex = Inventory.Instance.shortcutWeaponIndexes.FindIndex(s => s == slotIndex);

        if (weaponIndex != -1)
        {
            Inventory.Instance.shortcutWeaponIndexes[weaponIndex] = -1;
            Debug.Log($"⛔ Shortcut-slot {slotIndex} cleared, weapon index {weaponIndex} unassigned.");
        }
    }
}
