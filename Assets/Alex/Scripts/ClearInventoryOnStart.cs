using UnityEngine;

namespace NeuroEscape
{
    /// Empties the hotbar when this scene starts, so items carried over from earlier levels
    /// don't take up slots here. Also puts away anything held in the hand.
    /// Put it on any object in the scene (e.g. an empty "LevelStart").
    public class ClearInventoryOnStart : MonoBehaviour
    {
        void Awake()
        {
            if (EquipManager.Instance != null) EquipManager.Instance.PutAway();

            Inventory inv = Inventory.Instance;
            if (inv == null) return;

            for (int i = 0; i < Inventory.SlotCount; i++)
            {
                ItemData item = inv.GetSlot(i);
                while (item != null)
                {
                    inv.Remove(item);
                    item = inv.GetSlot(i);
                }
            }
        }
    }
}
