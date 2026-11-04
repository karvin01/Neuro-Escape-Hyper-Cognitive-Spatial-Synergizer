using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeuroEscape
{
    /// Starts a dialogue. Choose WHEN with 'Start When':
    /// - Press E: look at this object (needs a normal collider) and press E.
    /// - Walk Into Zone: give this object a collider with 'Is Trigger' ticked; plays when the player walks in.
    /// - At Start: plays when the scene starts.
    /// - Only When Called: another script or event calls Play() (e.g. a puzzle's On Solved).
    public class DialogueTrigger : Interactable
    {
        public enum StartMode { PressE, WalkIntoZone, AtStart, OnlyWhenCalled }

        [Serializable] public class ChoiceEvent : UnityEvent<int> { }

        public StartMode startWhen = StartMode.PressE;

        [Tooltip("What is said.")]
        public DialogueData dialogue;

        [Tooltip("Optional. Said on the 2nd time and after (empty = the same dialogue again).")]
        public DialogueData repeatDialogue;

        [Tooltip("Tick = can only happen once.")]
        public bool onlyOnce;

        [Tooltip("Text shown while looking at it (Press E mode).")]
        public string prompt = "Press E to talk";

        [Header("When it ends")]
        public UnityEvent onFinished;

        [Tooltip("If the dialogue has answers: runs with the picked answer (1 = first).")]
        public ChoiceEvent onChoice;

        [Tooltip("If the dialogue has a right answer: runs when it was picked.")]
        public UnityEvent onCorrectAnswer;

        [Tooltip("If the dialogue has a right answer: runs when a wrong one was picked.")]
        public UnityEvent onWrongAnswer;

        public int TimesPlayed { get; private set; }

        bool CanPlay => !(onlyOnce && TimesPlayed > 0) && !DialogueBox.IsOpen;

        public override string Prompt => startWhen == StartMode.PressE && CanPlay ? prompt : "";

        public override void Interact()
        {
            if (startWhen == StartMode.PressE) Play();
        }

        void Start()
        {
            if (startWhen == StartMode.AtStart) Play();
        }

        void OnTriggerEnter(Collider other)
        {
            if (startWhen == StartMode.WalkIntoZone && other.GetComponentInParent<PlayerFreeze>() != null)
                Play();
        }

        /// Starts the dialogue (also usable from events: drag this object in and pick DialogueTrigger > Play).
        public void Play()
        {
            if (!CanPlay) return;
            DialogueData d = TimesPlayed > 0 && repeatDialogue != null ? repeatDialogue : dialogue;
            if (d == null) return;
            TimesPlayed++;
            DialogueBox.Play(d, choice =>
            {
                onFinished?.Invoke();
                if (choice <= 0) return;
                onChoice?.Invoke(choice);
                if (d.correctChoice > 0)
                {
                    if (d.IsCorrect(choice)) onCorrectAnswer?.Invoke();
                    else onWrongAnswer?.Invoke();
                }
            });
        }
    }
}
