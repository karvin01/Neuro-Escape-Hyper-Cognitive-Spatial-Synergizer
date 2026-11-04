using UnityEngine;

namespace NeuroEscape
{
    /// The text of a book (or note) kept in its own file, so the same text can be put on any object.
    /// Create with: right-click in the Project window > Create > Neuro Escape > Book Text.
    /// Then drag it into an Inspectable's 'Book' slot.
    [CreateAssetMenu(fileName = "NewBook", menuName = "Neuro Escape/Book Text")]
    public class BookData : ScriptableObject
    {
        [Tooltip("Shown in the prompt and as the heading.")]
        public string title = "Old Book";

        [Tooltip("One entry per page. An open book shows two pages at a time.")]
        [TextArea(3, 12)] public string[] pages;
    }
}
