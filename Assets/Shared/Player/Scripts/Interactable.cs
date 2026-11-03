using UnityEngine;

namespace NeuroEscape
{
    /// Base for anything the player can look at and press E on (pickups, doors, puzzle buttons...).
    /// The object needs a (non-trigger) collider so the player's view can hit it.
    public abstract class Interactable : MonoBehaviour
    {
        /// Text shown on screen while looking at it. Return "" to show nothing.
        public abstract string Prompt { get; }

        /// Called when the player presses E while looking at it.
        public abstract void Interact();
    }
}
