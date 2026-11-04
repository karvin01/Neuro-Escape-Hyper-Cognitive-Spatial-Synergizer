using UnityEngine;

namespace NeuroEscape
{
    public enum FootstepSurfaceType { Stone, Wood, Grass }

    /// Tells FootstepAudio what the floor is made of.
    /// Put it on a floor object, OR once on a parent object (e.g. "Floor_Library") so every tile under it counts.
    /// Floors without this component use the player's Default Surface (Stone).
    public class FootstepSurface : MonoBehaviour
    {
        public FootstepSurfaceType surface = FootstepSurfaceType.Stone;
    }
}
