using UnityEngine;

namespace NeuroEscape
{
    /// One kind of item (a key, a letter...). Create with: right-click in Project > Create > Neuro Escape > Item.
    [CreateAssetMenu(fileName = "NewItem", menuName = "Neuro Escape/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName = "New Item";

        [Tooltip("Picture shown in the hotbar. Import the PNG with Texture Type = Sprite (2D and UI).")]
        public Sprite icon;

        [Tooltip("Tick for letters/notes: using it from the hotbar opens it to read.")]
        public bool isReadable;

        [TextArea(4, 15)]
        public string text;
    }
}
