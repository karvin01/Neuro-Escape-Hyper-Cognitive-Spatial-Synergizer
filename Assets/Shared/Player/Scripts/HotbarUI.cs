using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// Shows the hotbar slots. Keys 1-6 use a slot: readable items open in the LetterReaderUI,
    /// held items (e.g. a torch) are taken out / put away. Tab frees the mouse so slots can be clicked.
    /// Put this on the HUD canvas.
    public class HotbarUI : MonoBehaviour
    {
        [Tooltip("The Icon images inside Slot1..Slot6, in order.")]
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
            if (kb == null || DialogueBox.IsOpen || InspectViewer.IsOpen || CodeInputBox.IsOpen) return;   // number keys pick answers / type codes

            if (kb.digit1Key.wasPressedThisFrame) UseSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) UseSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) UseSlot(2);
            if (kb.digit4Key.wasPressedThisFrame) UseSlot(3);
            if (kb.digit5Key.wasPressedThisFrame) UseSlot(4);
            if (kb.digit6Key.wasPressedThisFrame) UseSlot(5);

            bool reading = letterReader != null && letterReader.IsOpen;
            if (kb.tabKey.wasPressedThisFrame && !reading && PlayerFreeze.Instance != null)
                PlayerFreeze.Instance.SetFrozen(!PlayerFreeze.IsFrozen);
        }

        /// Also called by each slot's Button (On Click) with 0..5.
        public void UseSlot(int index)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            Inventory inv = Inventory.Instance;
            if (inv == null) return;

            ItemData item = inv.GetSlot(index);
            if (item == null) return;

            if (item.heldPrefab != null)
            {
                if (EquipManager.Instance != null) EquipManager.Instance.Toggle(item);
            }
            else if (item.isReadable && letterReader != null)
            {
                letterReader.Open(item);
            }
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
