using UnityEngine;

namespace NeuroEscape
{
    /// A door that swings open. Can need a key from the hotbar, or be opened only by a puzzle (call Open()).
    /// Tip: if the door turns around its middle, put it under an empty "Hinge" object placed at its edge
    /// and drag that Hinge into the 'Hinge' slot.
    public class Door : Interactable
    {
        [Tooltip("Leave empty for a door that opens without a key.")]
        public ItemData requiredKey;
        public bool removeKeyWhenUsed = true;

        [Tooltip("Tick if only a puzzle script should open this door (pressing E does nothing).")]
        public bool openedByPuzzleOnly;

        [Header("Opening")]
        [Tooltip("The object that rotates. Empty = this object.")]
        public Transform hinge;
        public float openAngle = 90f;
        [Tooltip("Degrees per second.")]
        public float openSpeed = 120f;

        [Header("Sound")]
        [Tooltip("Optional. Played once when the door starts opening (e.g. a creak).")]
        public AudioClip openSound;
        [Range(0f, 1f)] public float soundVolume = 0.8f;
        [Tooltip("Optional. Played when E is pressed without the right key (still locked).")]
        public AudioClip lockedSound;

        float nextLockedSoundTime;

        public bool IsOpen { get; private set; }

        Quaternion openRotation;

        void Awake()
        {
            if (hinge == null) hinge = transform;
            openRotation = hinge.localRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        void Update()
        {
            if (IsOpen)
                hinge.localRotation = Quaternion.RotateTowards(
                    hinge.localRotation, openRotation, openSpeed * Time.deltaTime);
        }

        public override string Prompt
        {
            get
            {
                if (IsOpen) return "";
                if (openedByPuzzleOnly) return "Locked";
                if (requiredKey == null) return "Press E to open";
                if (Inventory.Instance == null) return "";
                return Inventory.Instance.Has(requiredKey)
                    ? "Press E to unlock"
                    : "Locked (needs " + requiredKey.itemName + ")";
            }
        }

        public override void Interact()
        {
            if (IsOpen || openedByPuzzleOnly) return;
            if (requiredKey != null)
            {
                if (Inventory.Instance == null || !Inventory.Instance.Has(requiredKey))
                {
                    PlayLockedSound();
                    return;
                }
                if (removeKeyWhenUsed) Inventory.Instance.Remove(requiredKey);
            }
            Open();
        }

        void PlayLockedSound()
        {
            if (lockedSound == null || Time.time < nextLockedSoundTime) return;   // no spam when E is mashed
            nextLockedSoundTime = Time.time + lockedSound.length;
            AudioSource.PlayClipAtPoint(lockedSound, hinge != null ? hinge.position : transform.position, soundVolume);
        }

        /// Opens the door. Puzzle scripts call this when the puzzle is solved.
        public void Open()
        {
            if (!IsOpen && openSound != null)
                AudioSource.PlayClipAtPoint(openSound, hinge != null ? hinge.position : transform.position, soundVolume);
            IsOpen = true;
        }
    }
}
