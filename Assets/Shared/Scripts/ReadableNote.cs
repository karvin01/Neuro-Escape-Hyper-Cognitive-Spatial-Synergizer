using UnityEngine;

namespace NeuroEscape
{
    /// A note that is read where it lies (NOT picked up). Press E to read, E/Esc to close.
    /// Put on an object with a collider and assign an Item (with Is Readable ticked) for its title and text.
    public class ReadableNote : Interactable
    {
        public ItemData note;

        LetterReaderUI reader;

        public override string Prompt => note != null ? "Press E to read" : "";

        public override void Interact()
        {
            if (note == null) return;
            if (reader == null) reader = FindFirstObjectByType<LetterReaderUI>();
            if (reader != null) reader.Open(note);
            else Debug.LogWarning("ReadableNote: no LetterReaderUI in the scene (is the PlayerRig there?).");
        }
    }
}
