using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// The talk box at the bottom of the screen (portrait, name, typing text, answer buttons).
    /// It creates itself in every scene from Shared/Dialogue/Resources/DialogueBox, so there is nothing to place.
    /// Scripts start a talk with:  DialogueBox.Play(someDialogue);
    /// or, to know when it ends / which answer was picked (1 = first, 0 = none):
    ///     DialogueBox.Play(someDialogue, choice => { ... });
    /// Controls: E / Space / Enter / click = skip typing, then next line.
    /// Answers: W/S or arrows + E, number keys 1-6 (top row or numpad), or click.
    public class DialogueBox : MonoBehaviour
    {
        public static DialogueBox Instance { get; private set; }

        /// True while a dialogue is on screen (other scripts can wait for this).
        public static bool IsOpen => Instance != null && Instance.open;

        [Header("Look")]
        public TMP_FontAsset textFont;
        public TMP_FontAsset nameFont;
        [Tooltip("Frame picture (9-sliced).")]
        public Sprite frame;
        public Color textColor = new Color(0.95f, 0.9f, 0.8f);
        public Color nameColor = new Color(1f, 0.82f, 0.3f);
        public Color answerColor = new Color(0.85f, 0.8f, 0.7f);
        public Color selectedAnswerColor = new Color(1f, 0.85f, 0.3f);
        [Tooltip("Size of the box on a 1920x1080 screen.")]
        public Vector2 boxSize = new Vector2(1500f, 300f);
        public float textSize = 34f;
        public float nameSize = 46f;

        [Header("Typing")]
        public float lettersPerSecond = 40f;
        [Tooltip("Mumble sounds played while the text types.")]
        public AudioClip[] voiceBlips;
        [Range(0f, 1f)] public float voiceVolume = 0.5f;
        [Tooltip("A mumble every this many letters.")]
        public int lettersPerBlip = 3;
        public Vector2 voicePitch = new Vector2(0.9f, 1.15f);

        // ---------- runtime ----------
        bool open, typing, answersShown, ending, wasFrozenBefore;
        DialogueData data;
        Action<int> onDone;
        int lineIndex, totalChars, shownChars, lettersSinceBlip, lastBlip = -1, selected, openedFrame = -1;
        float visibleChars;

        GameObject box, hint;
        RectTransform answersRoot;
        Image portraitImage;
        TextMeshProUGUI nameText, bodyText, hintText;
        RectTransform nameRect, bodyRect;
        readonly List<Image> answerFrames = new List<Image>();
        readonly List<TextMeshProUGUI> answerTexts = new List<TextMeshProUGUI>();
        AudioSource voice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateOnStart() => EnsureExists();

        static void EnsureExists()
        {
            if (Instance != null) return;
            DialogueBox prefab = Resources.Load<DialogueBox>("DialogueBox");
            DialogueBox b = prefab != null ? Instantiate(prefab) : new GameObject().AddComponent<DialogueBox>();
            b.name = "DialogueBox";
            DontDestroyOnLoad(b.gameObject);
        }

        /// Shows a dialogue. onDone gets the picked answer (1 = first) or 0 when there were no answers.
        /// Returns false if another dialogue is already on screen.
        public static bool Play(DialogueData dialogue, Action<int> onDone = null)
        {
            EnsureExists();
            return Instance.Begin(dialogue, onDone);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            BuildUI();
            box.SetActive(false);
            answersRoot.gameObject.SetActive(false);
        }

        bool Begin(DialogueData d, Action<int> done)
        {
            if (open || d == null || d.LineCount == 0) return false;

            if (!ending) wasFrozenBefore = PlayerFreeze.IsFrozen;   // a dialogue started from another's end keeps the old state
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);

            data = d;
            onDone = done;
            open = true;
            openedFrame = Time.frameCount;

            bool hasName = !string.IsNullOrEmpty(d.speakerName);
            nameText.text = hasName ? d.speakerName : "";
            nameText.gameObject.SetActive(hasName);
            portraitImage.sprite = d.portrait;
            portraitImage.gameObject.SetActive(d.portrait != null);

            float left = d.portrait != null ? boxSize.y + 10f : 44f;
            nameRect.offsetMin = new Vector2(left, nameRect.offsetMin.y);
            bodyRect.offsetMin = new Vector2(left, bodyRect.offsetMin.y);
            bodyRect.offsetMax = new Vector2(bodyRect.offsetMax.x, hasName ? -88f : -36f);

            box.SetActive(true);
            ShowLine(0);
            return true;
        }

        void ShowLine(int i)
        {
            lineIndex = i;
            HideAnswers();
            hint.SetActive(false);
            bodyText.text = data.GetLine(i);
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            totalChars = bodyText.textInfo.characterCount;
            visibleChars = 0f;
            shownChars = 0;
            lettersSinceBlip = lettersPerBlip - 1;   // first letter mumbles right away
            typing = true;
            if (totalChars == 0) FinishTyping();
        }

        void Update()
        {
            if (!open) return;
            bool firstFrame = Time.frameCount == openedFrame;

            // keep the mouse free while talking (the player controller likes to grab it back, e.g. when the window gets focus)
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;

            if (typing)
            {
                visibleChars += lettersPerSecond * Time.unscaledDeltaTime;
                int v = Mathf.Min(totalChars, (int)visibleChars);
                for (int c = shownChars; c < v; c++)
                {
                    if (char.IsWhiteSpace(bodyText.textInfo.characterInfo[c].character)) continue;
                    if (++lettersSinceBlip >= lettersPerBlip) { lettersSinceBlip = 0; Blip(); }
                }
                shownChars = v;
                bodyText.maxVisibleCharacters = v;
                if (v >= totalChars) FinishTyping();
            }

            if (firstFrame) return;   // the E that opened the box must not also skip the first line

            if (answersShown) { HandleAnswers(); return; }

            if (AdvancePressed())
            {
                if (typing) FinishTyping();
                else if (lineIndex + 1 < data.LineCount) ShowLine(lineIndex + 1);
                else End(0);
            }

            if (hint.activeSelf)   // gentle blink of the "E" hint
            {
                float a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
                hintText.alpha = a;
            }
        }

        void FinishTyping()
        {
            typing = false;
            shownChars = totalChars;
            bodyText.maxVisibleCharacters = 99999;
            bool last = lineIndex + 1 >= data.LineCount;
            if (last && data.HasChoices) ShowAnswers();
            else hint.SetActive(true);
        }

        static bool AdvancePressed()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                                   kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        void End(int choice)
        {
            open = false;
            typing = false;
            HideAnswers();
            box.SetActive(false);

            Action<int> done = onDone;
            onDone = null;
            data = null;

            ending = true;
            done?.Invoke(choice);   // may start the next dialogue right away
            ending = false;

            if (!open && !wasFrozenBefore && PlayerFreeze.Instance != null)
                PlayerFreeze.Instance.SetFrozen(false);
        }

        void Blip()
        {
            if (voiceBlips == null || voiceBlips.Length == 0) return;
            int i = UnityEngine.Random.Range(0, voiceBlips.Length);
            if (voiceBlips.Length > 1 && i == lastBlip) i = (i + 1) % voiceBlips.Length;
            lastBlip = i;
            if (voiceBlips[i] == null) return;
            voice.pitch = UnityEngine.Random.Range(voicePitch.x, voicePitch.y);
            voice.PlayOneShot(voiceBlips[i], voiceVolume);
        }

        // ---------- answers ----------

        void ShowAnswers()
        {
            string[] choices = data.choices;
            while (answerFrames.Count < choices.Length) AddAnswerRow();

            const float rowH = 66f, gap = 10f;
            answersRoot.sizeDelta = new Vector2(answersRoot.sizeDelta.x, choices.Length * (rowH + gap));
            for (int i = 0; i < answerFrames.Count; i++)
            {
                bool used = i < choices.Length;
                answerFrames[i].gameObject.SetActive(used);
                if (!used) continue;
                RectTransform rt = answerFrames[i].rectTransform;
                rt.anchoredPosition = new Vector2(0f, -i * (rowH + gap));
                rt.sizeDelta = new Vector2(0f, rowH);
                answerTexts[i].text = (i + 1) + ".  " + choices[i];
            }
            selected = 0;
            HookKeyboard();
            typedNumber = -1;
            answersShown = true;
            answersRoot.gameObject.SetActive(true);
            RefreshAnswers();
        }

        void HideAnswers()
        {
            answersShown = false;
            if (answersRoot != null) answersRoot.gameObject.SetActive(false);
        }

        static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
        static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6 };

        // Typed characters: makes 1-6 work on every keyboard layout (e.g. AZERTY with or without Shift).
        Keyboard hookedKeyboard;
        int typedNumber = -1;

        void HookKeyboard()
        {
            Keyboard kb = Keyboard.current;
            if (kb == hookedKeyboard) return;
            if (hookedKeyboard != null) hookedKeyboard.onTextInput -= OnTextInput;
            hookedKeyboard = kb;
            if (kb != null) kb.onTextInput += OnTextInput;
        }

        void OnTextInput(char c)
        {
            if (c >= '1' && c <= '9') typedNumber = c - '1';
        }

        void OnDestroy()
        {
            if (hookedKeyboard != null) hookedKeyboard.onTextInput -= OnTextInput;
        }

        void HandleAnswers()
        {
            int n = data.choices.Length;
            HookKeyboard();
            if (typedNumber >= 0)
            {
                int t = typedNumber;
                typedNumber = -1;
                if (t < n) { Pick(t); return; }
            }
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { selected = (selected - 1 + n) % n; RefreshAnswers(); }
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { selected = (selected + 1) % n; RefreshAnswers(); }

                for (int i = 0; i < n && i < DigitKeys.Length; i++)
                    if (kb[DigitKeys[i]].wasPressedThisFrame || kb[NumpadKeys[i]].wasPressedThisFrame) { Pick(i); return; }

                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                    kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Pick(selected); return; }
            }

            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                for (int i = 0; i < n; i++)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(answerFrames[i].rectTransform, pos, null)) continue;
                    if (selected != i) { selected = i; RefreshAnswers(); }
                    if (mouse.leftButton.wasPressedThisFrame) { Pick(i); return; }
                }
            }
        }

        void Pick(int index) => End(index + 1);

        void RefreshAnswers()
        {
            for (int i = 0; i < answerFrames.Count; i++)
            {
                bool sel = i == selected;
                answerFrames[i].color = sel ? Color.white : new Color(0.6f, 0.6f, 0.65f, 1f);
                answerTexts[i].color = sel ? selectedAnswerColor : answerColor;
            }
        }

        // ---------- building the UI (no prefab editing needed) ----------

        void BuildUI()
        {
            var canvasGO = new GameObject("DialogueCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // the box
            RectTransform boxRt = NewRect("Box", canvasGO.transform);
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.sizeDelta = boxSize;
            boxRt.anchoredPosition = new Vector2(0f, 30f);
            MakeFrame(boxRt.gameObject);
            box = boxRt.gameObject;

            // portrait (left, square)
            RectTransform pr = NewRect("Portrait", boxRt);
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 0.5f);
            pr.pivot = new Vector2(0f, 0.5f);
            float ps = boxSize.y - 44f;
            pr.sizeDelta = new Vector2(ps, ps);
            pr.anchoredPosition = new Vector2(22f, 0f);
            portraitImage = pr.gameObject.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            // name (top)
            nameRect = NewRect("Name", boxRt);
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.offsetMin = new Vector2(44f, -84f);
            nameRect.offsetMax = new Vector2(-40f, -26f);
            nameText = NewText(nameRect, nameFont != null ? nameFont : textFont, nameSize, nameColor, TextAlignmentOptions.BottomLeft);

            // text (fills the rest)
            bodyRect = NewRect("Text", boxRt);
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(44f, 40f);
            bodyRect.offsetMax = new Vector2(-48f, -88f);
            bodyText = NewText(bodyRect, textFont, textSize, textColor, TextAlignmentOptions.TopLeft);
            bodyText.overflowMode = TextOverflowModes.Overflow;

            // "E" hint (bottom right)
            RectTransform hr = NewRect("Hint", boxRt);
            hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 0f);
            hr.sizeDelta = new Vector2(120f, 40f);
            hr.anchoredPosition = new Vector2(-30f, 18f);
            hintText = NewText(hr, textFont, 28f, nameColor, TextAlignmentOptions.BottomRight);
            hintText.text = "[E]";
            hint = hr.gameObject;

            // answers (stacked above the box)
            answersRoot = NewRect("Answers", canvasGO.transform);
            answersRoot.anchorMin = answersRoot.anchorMax = new Vector2(0.5f, 0f);
            answersRoot.pivot = new Vector2(0.5f, 0f);
            answersRoot.sizeDelta = new Vector2(boxSize.x * 0.72f, 0f);
            answersRoot.anchoredPosition = new Vector2(0f, 30f + boxSize.y + 16f);
        }

        void AddAnswerRow()
        {
            RectTransform row = NewRect("Answer" + (answerFrames.Count + 1), answersRoot);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            answerFrames.Add(MakeFrame(row.gameObject));

            RectTransform tr = NewRect("Text", row);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(34f, 4f);
            tr.offsetMax = new Vector2(-34f, -4f);
            answerTexts.Add(NewText(tr, textFont, 32f, answerColor, TextAlignmentOptions.MidlineLeft));
        }

        Image MakeFrame(GameObject go)
        {
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            if (frame != null)
            {
                img.sprite = frame;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 0.25f;   // makes the pixel border 4x bigger (crisp pixels)
            }
            else img.color = new Color(0.06f, 0.06f, 0.14f, 0.93f);
            return img;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static TextMeshProUGUI NewText(RectTransform rt, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
    }
}
