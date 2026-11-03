using System;
using UnityEngine;

namespace NeuroEscape
{
    /// Holds the 4 hotbar slots. Creates itself when first used and survives scene changes,
    /// so items stay in the hotbar when the player moves to the next map. No setup needed.
    public class Inventory : MonoBehaviour
    {
        public const int SlotCount = 4;

        static Inventory instance;
        static bool quitting;

        public static Inventory Instance
        {
            get
            {
                if (instance == null && !quitting)
                {
                    var go = new GameObject("Inventory");
                    instance = go.AddComponent<Inventory>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            quitting = false;
        }

        void OnApplicationQuit() => quitting = true;

        readonly ItemData[] slots = new ItemData[SlotCount];

        /// Fired whenever an item is added or removed (the hotbar listens to this).
        public event Action Changed;

        public bool IsFull => Array.IndexOf(slots, null) < 0;

        public ItemData GetSlot(int index) =>
            (index >= 0 && index < SlotCount) ? slots[index] : null;

        public bool Has(ItemData item) => item != null && Array.IndexOf(slots, item) >= 0;

        /// Puts the item in the first free slot. Returns false if the hotbar is full.
        public bool Add(ItemData item)
        {
            if (item == null) return false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = item;
                    Changed?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public void Remove(ItemData item)
        {
            int i = Array.IndexOf(slots, item);
            if (i >= 0)
            {
                slots[i] = null;
                Changed?.Invoke();
            }
        }
    }
}
