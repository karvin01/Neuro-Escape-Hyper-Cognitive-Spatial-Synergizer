using UnityEngine;

namespace NeuroEscape
{
    /// One conversation (e.g. the wizard's intro). Create with:
    /// right-click in the Project window > Create > Neuro Escape > Dialogue.
    /// Fill in the lines; add Answers to turn the LAST line into a question.
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Neuro Escape/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        [Tooltip("Name shown above the text.")]
        public string speakerName = "Wizard";

        [Tooltip("Picture on the left of the box. Empty = no picture.")]
        public Sprite portrait;

        [Tooltip("One box of text per line. E, Space or a click goes to the next line.")]
        [TextArea(2, 6)]
        public string[] lines;

        [Header("Answers (optional)")]
        [Tooltip("Answer buttons shown with the LAST line. Leave empty for a normal talk.")]
        public string[] choices;

        [Tooltip("Which answer is right: 1 = first answer. 0 = there is no right answer.")]
        public int correctChoice;

        public bool HasChoices => choices != null && choices.Length > 0;

        public int LineCount => lines != null && lines.Length > 0 ? lines.Length : (HasChoices ? 1 : 0);

        public string GetLine(int i) => lines != null && i < lines.Length ? lines[i] : "";

        /// True if 'choice' (1 = first answer) is the right answer.
        public bool IsCorrect(int choice) => correctChoice > 0 && choice == correctChoice;
    }
}
