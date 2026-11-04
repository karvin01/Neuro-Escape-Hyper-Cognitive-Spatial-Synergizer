using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace NeuroEscape
{
    /// The bonfire: press E to summon the wizard.
    /// First it checks a few funny cases (top to bottom, the first that fits wins):
    ///  1. Everything finished         -> After Done
    ///  2. Summoned again too quickly   -> Annoyed (gets worse each time; the last one makes the fire sulk)
    ///  3. Holding a torch out          -> Torch Out
    /// Otherwise he appears (the very first time with the Intro) and asks "How can I help?" (the Menu).
    /// The Menu's answers must be in this order:
    ///  1 = more story (Story lines, a different one each time)   2 = a hint (he refuses: No Hints)
    ///  3 = "I know the order" -> only after the puzzles: the Final Questions, else Puzzles/Items Missing
    ///  4 = goodbye (Farewell Lines, a joke, then he vanishes)
    /// When the sconce puzzle gets solved, he calls out once (Flames Awake) to come back to the bonfire.
    public class BonfireWizard : Interactable
    {
        [Header("Talking")]
        public DialogueData intro;
        [Tooltip("'How can I help?' with 4 answers: story / hint / I know the order / goodbye.")]
        public DialogueData menu;
        [Tooltip("Answer 1: more of the story, a different one each time (in order, then repeats).")]
        public DialogueData[] story;
        [Tooltip("Answer 2: he refuses to give hints (in order, then repeats).")]
        public DialogueData[] noHints;
        [Tooltip("Answer 4: a joke before he vanishes (in order, then repeats).")]
        [FormerlySerializedAs("idleLines")] public DialogueData[] farewellLines;

        [Header("Funny cases")]
        [Tooltip("Summoned again within 'Spam Seconds' after he left. Each next one is angrier; after the last one the fire sulks.")]
        public DialogueData[] annoyed;
        public DialogueData torchOut;

        [Header("The final test")]
        [Tooltip("Answer 3 while the puzzles are not solved yet.")]
        public DialogueData puzzlesMissing;
        [Tooltip("Answer 3 while the needed items are not in the hotbar yet.")]
        public DialogueData itemsMissing;
        [Tooltip("Said (from afar) right after the puzzles get solved.")]
        public DialogueData flamesAwake;
        [Tooltip("Seconds between solving the puzzle and his call.")]
        public float flamesAwakeDelay = 2f;
        [Tooltip("The questions, asked one after another. Each has its own Answers and Correct Choice.")]
        public DialogueData[] finalQuestions;
        [Tooltip("Ticked = he says 'wrong' right after a wrong answer. Unticked = he only judges after the last question (harder).")]
        public bool judgeEachAnswer;
        [Tooltip("Said after getting it wrong (in order, then repeats).")]
        public DialogueData[] wrongAnswers;
        public DialogueData correct;
        public DialogueData afterDone;

        [Header("What the final test needs")]
        [Tooltip("Puzzles that must be solved. Empty = every Sconce Puzzle in the scene.")]
        public SconcePuzzle[] puzzlesToSolve;
        [Tooltip("Optional. Items that must be in the hotbar (e.g. the tower page). Empty slots are ignored.")]
        public ItemData[] itemsNeeded;

        [Header("When the answers are right")]
        public UnityEvent onCorrect;
        [Tooltip("Optional. Scene to load after the right answers (must be in File > Build Profiles > Scene List).")]
        public string nextScene;

        [Header("Annoying him")]
        public float spamSeconds = 4f;
        [Tooltip("How long the fire sulks (and he refuses to come).")]
        public float sulkSeconds = 12f;

        [Header("The wizard and the fire")]
        [Tooltip("The standing wizard (hidden until summoned).")]
        public Transform wizard;
        public AudioClip poofSound;
        [Range(0f, 1f)] public float poofVolume = 0.6f;
        public TorchFlicker fire;
        [Tooltip("The flames, shrunk while sulking.")]
        public Transform flames;

        public bool Done { get; private set; }

        bool introDone, quizWrong, announced, leaving;
        int storyIndex, hintIndex, farewellIndex, annoyLevel, wrongIndex;
        float lastLeftTime = -999f, sulkUntil = -1f;
        Vector3 wizardScale = Vector3.one, flamesScale = Vector3.one;
        SconcePuzzle[] puzzles;
        Coroutine scaling;

        bool Sulking => Time.time < sulkUntil;
        bool HasQuiz => finalQuestions != null && finalQuestions.Length > 0;

        void Awake()
        {
            if (wizard != null) { wizardScale = wizard.localScale; wizard.gameObject.SetActive(false); }
            if (flames != null) flamesScale = flames.localScale;
        }

        void Start()
        {
            puzzles = puzzlesToSolve != null && puzzlesToSolve.Length > 0
                ? puzzlesToSolve
                : FindObjectsByType<SconcePuzzle>(FindObjectsSortMode.None);
        }

        public override string Prompt
        {
            get
            {
                if (DialogueBox.IsOpen) return "";
                return Sulking ? "The fire is sulking..." : "Press E to summon the wizard";
            }
        }

        public override void Interact()
        {
            if (DialogueBox.IsOpen || Sulking) return;

            if (Done) { Say(afterDone, Leave); return; }

            if (Time.time - lastLeftTime < spamSeconds && annoyed != null && annoyed.Length > 0)
            {
                DialogueData d = annoyed[Mathf.Min(annoyLevel, annoyed.Length - 1)];
                annoyLevel++;
                if (annoyLevel >= annoyed.Length) { annoyLevel = 0; sulkUntil = Time.time + sulkSeconds + 3f; }
                Say(d, Leave);
                return;
            }
            annoyLevel = 0;

            if (torchOut != null && HeldTorch.Current != null) { Say(torchOut, Leave); return; }

            if (!introDone && intro != null) { introDone = true; Say(intro, ShowMenu); }
            else ShowMenu();
        }

        // ---------- the menu ----------

        void ShowMenu()
        {
            Appear();
            if (menu == null || !DialogueBox.Play(menu, OnMenu)) Leave();
        }

        void OnMenu(int choice)
        {
            switch (choice)
            {
                case 1: Say(Next(story, ref storyIndex), ShowMenu); break;
                case 2: Say(Next(noHints, ref hintIndex), ShowMenu); break;
                case 3:
                    if (!PuzzlesSolved()) Say(puzzlesMissing, ShowMenu);
                    else if (!HasItems()) Say(itemsMissing, ShowMenu);
                    else if (HasQuiz) StartQuiz();
                    else ShowMenu();
                    break;
                default: Say(Next(farewellLines, ref farewellIndex), Leave); break;
            }
        }

        static DialogueData Next(DialogueData[] list, ref int index)
        {
            if (list == null || list.Length == 0) return null;
            return list[index++ % list.Length];
        }

        /// Shows the wizard, plays d, then runs 'then' (also when d is empty).
        void Say(DialogueData d, System.Action then)
        {
            Appear();
            if (d == null || !DialogueBox.Play(d, _ => then())) then();
        }

        // ---------- the final test ----------

        void StartQuiz()
        {
            Appear();
            quizWrong = false;
            Ask(0);
        }

        void Ask(int i)
        {
            DialogueData q = finalQuestions[i];
            System.Action<int> answered = choice =>
            {
                bool right = q == null || q.correctChoice <= 0 || q.IsCorrect(choice);
                if (!right) quizWrong = true;
                if (!right && judgeEachAnswer) Answered(false);
                else if (i + 1 < finalQuestions.Length) Ask(i + 1);
                else Answered(!quizWrong);
            };
            if (q == null || !DialogueBox.Play(q, answered)) answered(0);
        }

        void Answered(bool right)
        {
            if (right)
            {
                Done = true;
                Say(correct, () =>
                {
                    Leave();
                    onCorrect?.Invoke();
                    if (!string.IsNullOrEmpty(nextScene)) StartCoroutine(LoadNext());
                });
                return;
            }
            Say(Next(wrongAnswers, ref wrongIndex), () =>
            {
                Leave();
                lastLeftTime = -999f;   // trying again right away is not "annoying"
            });
        }

        IEnumerator LoadNext()
        {
            yield return new WaitForSeconds(1.2f);
            SceneManager.LoadScene(nextScene);
        }

        bool PuzzlesSolved()
        {
            if (puzzles == null) Start();
            foreach (SconcePuzzle p in puzzles)
                if (p != null && !p.Solved) return false;
            return true;
        }

        bool HasItems()
        {
            if (itemsNeeded == null) return true;
            Inventory inv = Inventory.Instance;
            foreach (ItemData it in itemsNeeded)
                if (it != null && (inv == null || !inv.Has(it))) return false;
            return true;
        }

        // ---------- "the flames are awake, come to the bonfire!" ----------

        IEnumerator CallPlayer()
        {
            yield return new WaitForSeconds(flamesAwakeDelay);
            while (DialogueBox.IsOpen) yield return null;
            if (!Done) DialogueBox.Play(flamesAwake);
        }

        // ---------- the wizard popping in and out ----------

        void Appear()
        {
            if (wizard == null) return;
            if (wizard.gameObject.activeSelf && !leaving) return;   // already here
            leaving = false;
            if (!wizard.gameObject.activeSelf) Poof();
            wizard.gameObject.SetActive(true);
            if (scaling != null) StopCoroutine(scaling);
            scaling = StartCoroutine(Scale(wizard, Vector3.zero, wizardScale, 0.25f, false));
        }

        void Leave()
        {
            lastLeftTime = Time.time;
            if (wizard == null || !wizard.gameObject.activeSelf || leaving) return;
            leaving = true;
            Poof();
            if (scaling != null) StopCoroutine(scaling);
            scaling = StartCoroutine(Scale(wizard, wizard.localScale, Vector3.zero, 0.2f, true));
        }

        void Poof()
        {
            if (poofSound != null && wizard != null)
                AudioSource.PlayClipAtPoint(poofSound, wizard.position + Vector3.up, poofVolume);
        }

        static IEnumerator Scale(Transform t, Vector3 from, Vector3 to, float time, bool hideAtEnd)
        {
            for (float e = 0f; e < time; e += Time.deltaTime)
            {
                float k = e / time;
                float pop = hideAtEnd ? 1f : 1f + Mathf.Sin(k * Mathf.PI) * 0.25f * (1f - k);   // little bounce when appearing
                t.localScale = Vector3.LerpUnclamped(from, to, k * pop);
                yield return null;
            }
            t.localScale = to;
            if (hideAtEnd) t.gameObject.SetActive(false);
        }

        // ---------- every frame: sulking fire + watching the puzzle ----------

        void Update()
        {
            float target = Sulking ? 0.25f : 1f;
            if (fire != null) fire.brightness = Mathf.MoveTowards(fire.brightness, target, Time.deltaTime * 1.5f);
            if (flames != null)
                flames.localScale = Vector3.MoveTowards(flames.localScale, flamesScale * (Sulking ? 0.45f : 1f), Time.deltaTime);

            if (!announced && flamesAwake != null && puzzles != null && puzzles.Length > 0 && PuzzlesSolved())
            {
                announced = true;
                StartCoroutine(CallPlayer());
            }
        }

        // ---------- testing (Play mode: right-click the "Bonfire Wizard" title in the Inspector) ----------

        [ContextMenu("Test: ask the final questions now")]
        void TestFinalQuestions()
        {
            if (!Application.isPlaying) { Debug.Log("Press Play first."); return; }
            if (!DialogueBox.IsOpen && HasQuiz) StartQuiz();
        }

        [ContextMenu("Test: forget everything (intro again)")]
        void TestReset()
        {
            introDone = false; Done = false; announced = false;
            storyIndex = hintIndex = farewellIndex = annoyLevel = wrongIndex = 0;
            lastLeftTime = -999f; sulkUntil = -1f;
        }
    }
}
