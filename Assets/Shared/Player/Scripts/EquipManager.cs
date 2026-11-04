using UnityEngine;
using UnityEngine.InputSystem;

namespace NeuroEscape
{
    /// Takes "held" items out of the hotbar into the player's hand (items with a Held Prefab, e.g. a torch).
    /// F = take out / put away. Creates itself when the game starts; no setup needed.
    /// The held object is placed in front of the camera using its prefab's own position and rotation.
    public class EquipManager : MonoBehaviour
    {
        public static EquipManager Instance { get; private set; }

        /// The item currently in the hand (null = empty hand).
        public ItemData Equipped { get; private set; }

        GameObject held;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("EquipManager");
            Instance = go.AddComponent<EquipManager>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            if (Equipped != null)
            {
                Inventory inv = Inventory.Instance;
                if (inv == null || !inv.Has(Equipped)) PutAway();      // item was used up
                else if (held == null) TakeOut(Equipped);            // e.g. after a scene change
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame && !PlayerFreeze.IsFrozen)
                Toggle();
        }

        /// F: put away what is in the hand, or take out the first held item from the hotbar.
        public void Toggle()
        {
            if (Equipped != null) { PutAway(); return; }
            ItemData item = FirstHoldable();
            if (item != null) TakeOut(item);
        }

        /// Hotbar key: take this item out, or put it away if it is already in the hand.
        public void Toggle(ItemData item)
        {
            if (item == null) return;
            if (Equipped == item) PutAway();
            else TakeOut(item);
        }

        public void TakeOut(ItemData item)
        {
            if (item == null || item.heldPrefab == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            if (held != null) Destroy(held);
            held = Instantiate(item.heldPrefab, cam.transform, false);
            Equipped = item;
        }

        public void PutAway()
        {
            if (held != null) Destroy(held);
            held = null;
            Equipped = null;
        }

        static ItemData FirstHoldable()
        {
            Inventory inv = Inventory.Instance;
            if (inv == null) return null;
            for (int i = 0; i < Inventory.SlotCount; i++)
            {
                ItemData it = inv.GetSlot(i);
                if (it != null && it.heldPrefab != null) return it;
            }
            return null;
        }
    }
}
