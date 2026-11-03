using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NeuroEscape
{
    /// Shows a readable item's text on screen. E or Esc (or the Close button) closes it.
    /// Put this on the HUD canvas (NOT on the panel itself), and drag the panel into 'Panel'.
    public class LetterReaderUI : MonoBehaviour
    {
        [Tooltip("The LetterPanel object that is shown/hidden.")]
        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text bodyText;

        public bool IsOpen => panel != null && panel.activeSelf;

        bool wasFrozenBefore;
        int openedFrame = -1;

        void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Open(ItemData item)
        {
            if (panel == null || item == null) return;

            if (!IsOpen) wasFrozenBefore = PlayerFreeze.IsFrozen;
            if (titleText != null) titleText.text = item.itemName;
            if (bodyText != null) bodyText.text = item.text;

            panel.SetActive(true);
            openedFrame = Time.frameCount;
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);
        }

        /// Also hook this to the Close button's On Click.
        public void Close()
        {
            if (!IsOpen) return;

            panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (PlayerFreeze.Instance != null && !wasFrozenBefore)
                PlayerFreeze.Instance.SetFrozen(false);
        }

        void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                Close();
        }
    }
}
