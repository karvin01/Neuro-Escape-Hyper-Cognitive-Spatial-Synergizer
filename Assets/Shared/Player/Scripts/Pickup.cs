using UnityEngine;

namespace NeuroEscape
{
    /// Put on an object in the world (with a collider). Press E to put its item in the hotbar.
    public class Pickup : Interactable
    {
        public ItemData item;

        public override string Prompt
        {
            get
            {
                if (item == null || Inventory.Instance == null) return "";
                return Inventory.Instance.IsFull
                    ? "Hotbar full"
                    : "Press E to pick up " + item.itemName;
            }
        }

        public override void Interact()
        {
            if (item == null || Inventory.Instance == null) return;
            if (Inventory.Instance.Add(item))
                Destroy(gameObject);
        }
    }
}
