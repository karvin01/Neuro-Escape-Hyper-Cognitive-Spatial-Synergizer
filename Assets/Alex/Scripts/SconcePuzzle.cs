using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeuroEscape
{
    /// "The Five Sconces": using one sconce flips it AND its two neighbours (in ring order).
    /// When every sconce burns, the puzzle is solved: gates rattle/open and On Solved fires
    /// (hook the wizard dialogue there later).
    public class SconcePuzzle : MonoBehaviour
    {
        [Tooltip("The sconces in ring order (each one's neighbours are the ones before and after it).")]
        public Sconce[] sconces;

        [Tooltip("Which sconces burn at the start (same order as Sconces).")]
        public bool[] startLit;

        [Header("When solved")]
        [Tooltip("Gates that only shake (the big gate that 'should' open).")]
        public Portcullis[] rattleOnSolve;
        [Tooltip("Gates that rise (e.g. the tower).")]
        public Portcullis[] openOnSolve;
        [Tooltip("Doors (with 'Opened By Puzzle Only') that open.")]
        public Door[] doorsToOpen;
        public UnityEvent onSolved;

        public bool Solved { get; private set; }

        void Start()
        {
            for (int i = 0; i < sconces.Length; i++)
            {
                if (sconces[i] == null) continue;
                sconces[i].puzzle = this;
                bool lit = startLit != null && i < startLit.Length && startLit[i];
                sconces[i].SetLit(lit);
            }
        }

        public void Use(Sconce s)
        {
            if (Solved) return;
            int i = Array.IndexOf(sconces, s);
            if (i < 0) { s.SetLit(!s.IsLit); return; }

            int n = sconces.Length;
            Flip(i);
            if (n > 1) Flip((i + 1) % n);
            if (n > 2) Flip((i - 1 + n) % n);
            CheckSolved();
        }

        void Flip(int i)
        {
            if (sconces[i] != null) sconces[i].SetLit(!sconces[i].IsLit);
        }

        void CheckSolved()
        {
            foreach (Sconce s in sconces)
                if (s != null && !s.IsLit) return;

            Solved = true;
            foreach (Portcullis p in rattleOnSolve) if (p != null) p.Rattle();
            foreach (Portcullis p in openOnSolve) if (p != null) p.Open();
            foreach (Door d in doorsToOpen) if (d != null) d.Open();
            onSolved?.Invoke();
        }
    }
}
