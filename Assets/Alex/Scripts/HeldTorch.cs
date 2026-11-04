using UnityEngine;

namespace NeuroEscape
{
    /// The torch in the player's hand (spawned by the EquipManager when the Torch item is taken out with F).
    /// Sconces and torch holders use HeldTorch.Current to know the player is holding a flame.
    public class HeldTorch : MonoBehaviour
    {
        public static HeldTorch Current { get; private set; }

        [Tooltip("Small up-and-down sway while holding it.")]
        public float swayAmount = 0.01f;
        public float swaySpeed = 1.6f;

        Vector3 startPos;

        void Awake() => Current = this;

        void Start() => startPos = transform.localPosition;

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Update()
        {
            transform.localPosition = startPos + Vector3.up * (Mathf.Sin(Time.time * swaySpeed) * swayAmount);
        }
    }
}
