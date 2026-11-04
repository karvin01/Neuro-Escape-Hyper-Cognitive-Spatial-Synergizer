using UnityEngine;

namespace NeuroEscape
{
    /// A sconce that can be kindled with the torch you carry (you keep your torch),
    /// or snuffed with E. When part of a SconcePuzzle, it also flips its two neighbours.
    public class Sconce : Interactable
    {
        [Tooltip("The puzzle this sconce belongs to (empty = a normal sconce).")]
        public SconcePuzzle puzzle;

        [Tooltip("The fire part (flame + light) shown when lit.")]
        public GameObject fire;

        public bool IsLit { get; private set; }

        void Awake()
        {
            if (fire != null) IsLit = fire.activeSelf;
        }

        public void SetLit(bool lit)
        {
            IsLit = lit;
            if (fire != null) fire.SetActive(lit);
        }

        public override string Prompt
        {
            get
            {
                if (puzzle != null && puzzle.Solved) return "";
                if (IsLit) return "Press E to snuff the flame";
                return HeldTorch.Current != null ? "Press E to kindle the sconce" : "It needs a flame";
            }
        }

        public override void Interact()
        {
            if (puzzle != null && puzzle.Solved) return;
            if (!IsLit && HeldTorch.Current == null) return;

            if (puzzle != null) puzzle.Use(this);
            else SetLit(!IsLit);
        }
    }
}
