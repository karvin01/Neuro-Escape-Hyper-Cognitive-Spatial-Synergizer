using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// Shows a readable item's text on screen. E or Esc (or the Close button) closes it.
    /// Put this on the HUD canvas (NOT on the panel itself), and drag the panel into 'Panel'.
    /// If the item has a Paper sprite (e.g. a torn page), it is shown behind the text.
    public class LetterReaderUI : MonoBehaviour
    {
        [Tooltip("The LetterPanel object that is shown/hidden.")]
        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text bodyText;

        [Tooltip("Optional. The background Image. Empty = the Image on Panel.")]
        public Image paperImage;

        public bool IsOpen => panel != null && panel.activeSelf;

        bool wasFrozenBefore;
        int openedFrame = -1;

        bool defaultsCached;
        Sprite defaultSprite;
        Color defaultColor;
        Image.Type defaultType;

        void Start()
        {
            CacheDefaults();
            if (panel != null) panel.SetActive(false);
        }

        void CacheDefaults()
        {
            if (defaultsCached) return;
            defaultsCached = true;
            if (paperImage == null && panel != null) paperImage = panel.GetComponent<Image>();
            if (paperImage != null)
            {
                defaultSprite = paperImage.sprite;
                defaultColor = paperImage.color;
                defaultType = paperImage.type;
            }
        }

        public void Open(ItemData item)
        {
            if (panel == null || item == null) return;
            CacheDefaults();

            if (!IsOpen) wasFrozenBefore = PlayerFreeze.IsFrozen;
            if (titleText != null) titleText.text = item.itemName;
            if (bodyText != null) bodyText.text = item.text;

            if (paperImage != null)
            {
                if (item.paper != null)
                {
                    paperImage.sprite = item.paper;
                    paperImage.color = Color.white;
                    paperImage.type = Image.Type.Simple;
                }
                else
                {
                    paperImage.sprite = defaultSprite;
                    paperImage.color = defaultColor;
                    paperImage.type = defaultType;
                }
            }

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
