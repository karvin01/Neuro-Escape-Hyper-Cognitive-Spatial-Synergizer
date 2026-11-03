using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// Shows the 4 hotbar slots. Keys 1-4 use a slot (readable items open in the LetterReaderUI).
    /// Tab frees the mouse so slots can also be clicked. Put this on the HUD canvas.
    public class HotbarUI : MonoBehaviour
    {
        [Tooltip("The Icon images inside Slot1..Slot4, in order.")]
        public Image[] slotIcons = new Image[Inventory.SlotCount];

        public LetterReaderUI letterReader;

        void OnEnable()
        {
            Inventory inv = Inventory.Instance;
            if (inv != null) inv.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Inventory inv = Inventory.Instance;
            if (inv != null) inv.Changed -= Refresh;
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) UseSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) UseSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) UseSlot(2);
            if (kb.digit4Key.wasPressedThisFrame) UseSlot(3);

            bool reading = letterReader != null && letterReader.IsOpen;
            if (kb.tabKey.wasPressedThisFrame && !reading && PlayerFreeze.Instance != null)
                PlayerFreeze.Instance.SetFrozen(!PlayerFreeze.IsFrozen);
        }

        /// Also called by each slot's Button (On Click) with 0, 1, 2 or 3.
        public void UseSlot(int index)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            Inventory inv = Inventory.Instance;
            if (inv == null || letterReader == null) return;

            ItemData item = inv.GetSlot(index);
            if (item != null && item.isReadable)
                letterReader.Open(item);
        }

        void Refresh()
        {
            Inventory inv = Inventory.Instance;
            if (inv == null) return;

            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] == null) continue;
                ItemData item = inv.GetSlot(i);
                slotIcons[i].sprite = item != null ? item.icon : null;
                slotIcons[i].enabled = item != null;
            }
        }
    }
}
