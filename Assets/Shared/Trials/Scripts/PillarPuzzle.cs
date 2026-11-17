using UnityEngine;
using UnityEngine.Events;

namespace NeuroEscape
{
    /// Watches the element pillars. When ALL are right, the pillars lock and the portal wakes up.
    public class PillarPuzzle : MonoBehaviour
    {
        [Tooltip("Empty = every Rotating Pillar in the scene.")]
        public RotatingPillar[] pillars;
        [Tooltip("The portal that wakes up when all pillars are right.")]
        public PortalTeleport portal;
        public AudioClip solvedSound;
        [Range(0f, 1f)] public float volume = 0.7f;
        public UnityEvent onSolved;

        public bool Solved { get; private set; }

        void Start()
        {
            if (pillars == null || pillars.Length == 0)
                pillars = FindObjectsByType<RotatingPillar>(FindObjectsSortMode.None);
        }

        /// Called by a pillar after it turned.
        public void Check()
        {
            if (Solved || pillars == null || pillars.Length == 0) return;
            foreach (RotatingPillar p in pillars)
                if (p != null && !p.IsRight) return;
            Solved = true;
            foreach (RotatingPillar p in pillars) if (p != null) p.Locked = true;
            Pickup.PlaySound(solvedSound, volume);
            if (portal != null) portal.Activate();
            onSolved?.Invoke();
        }
    }
}
