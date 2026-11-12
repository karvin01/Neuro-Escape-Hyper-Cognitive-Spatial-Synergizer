using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// The "type the missing number" window used by the Code Doors. Creates itself (same frame and
    /// font as the dialogue box), nothing to place. Scripts open it with:
    ///   CodeInputBox.Show("The Sequence", "4, 9, 7, 12, 10, 15, [ ? ]", answer => answer == "13");
    /// The check returns true = right (the box closes), false = wrong (it says so and stays open).
    /// Controls: type numbers, Backspace deletes, Enter confirms, Esc / right-click closes.
    public class CodeInputBox : MonoBehaviour
    {
        public static CodeInputBox Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        public int maxLength = 6;
        public string wrongText = "That is not the right number.";

        bool open, wasFrozenBefore;
        int openedFrame = -1, textFrame = -1;
        string typed = "";
        Func<string, bool> check;
        Action onClosed;

        GameObject root;
        TextMeshProUGUI titleText, sequenceText, answerText, messageText, hintText;
        RectTransform confirmRect;
        Image confirmFrame;
        Keyboard hooked;
        Coroutine messageRoutine;

        static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        /// Opens the box. 'isRight' gets what was typed. 'closed' runs when it closes (right answer or cancelled).
        public static bool Show(string title, string sequence, Func<string, bool> isRight, Action closed = null)
        {
            if (Instance == null)
            {
                var go = new GameObject("CodeInputBox");
                DontDestroyOnLoad(go);
                go.AddComponent<CodeInputBox>();
            }
            return Instance.Begin(title, sequence, isRight, closed);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
            root.SetActive(false);
        }

        bool Begin(string title, string sequence, Func<string, bool> isRight, Action closed)
        {
            if (open || DialogueBox.IsOpen || InspectViewer.IsOpen) return false;
            check = isRight;
            onClosed = closed;
            typed = "";
            titleText.text = title;
            sequenceText.text = sequence;
            messageText.text = "";
            wasFrozenBefore = PlayerFreeze.IsFrozen;
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);
            Hook();
            open = true;
            openedFrame = Time.frameCount;
            root.SetActive(true);
            Refresh();
            return true;
        }

        void Hook()
        {
            Keyboard kb = Keyboard.current;
            if (kb == hooked) return;
            if (hooked != null) hooked.onTextInput -= OnText;
            hooked = kb;
            if (kb != null) kb.onTextInput += OnText;
        }

        void OnDestroy() { if (hooked != null) hooked.onTextInput -= OnText; }

        // typed characters (works with any keyboard layout, e.g. AZERTY)
        void OnText(char c)
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (char.IsDigit(c) && typed.Length < maxLength) { typed += c; textFrame = Time.frameCount; Refresh(); }
            else if (c == '-' && typed.Length == 0) { typed = "-"; Refresh(); }
        }

        void Update()
        {
            if (!open) return;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
            if (Time.frameCount == openedFrame) return;   // the E that opened it

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Hook();
            if (kb != null)
            {
                // number keys by position too (AZERTY top row without Shift types & é " ... instead of digits)
                if (textFrame != Time.frameCount)
                    for (int d = 0; d <= 9; d++)
                        if ((kb[DigitKeys[d]].wasPressedThisFrame || kb[NumpadKeys[d]].wasPressedThisFrame) && typed.Length < maxLength)
                        { typed += (char)('0' + d); Refresh(); }
                if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) { if (typed.Length == 0 && textFrame != Time.frameCount) { typed = "-"; Refresh(); } }
                if (kb.backspaceKey.wasPressedThisFrame && typed.Length > 0) { typed = typed.Substring(0, typed.Length - 1); Refresh(); }
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Confirm(); return; }
                if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
            }
            if (mouse != null)
            {
                bool over = RectTransformUtility.RectangleContainsScreenPoint(confirmRect, mouse.position.ReadValue(), null);
                confirmFrame.color = over ? Color.white : new Color(0.7f, 0.7f, 0.75f, 1f);
                if (over && mouse.leftButton.wasPressedThisFrame) { Confirm(); return; }
                if (mouse.rightButton.wasPressedThisFrame) { Close(); return; }
            }
            float blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? 1f : 0f;
            answerText.text = (typed.Length > 0 ? typed : "") + (blink > 0f ? "_" : " ");
        }

        void Confirm()
        {
            if (typed.Length == 0 || typed == "-") return;
            bool right = check == null || check(typed.Trim());
            if (right) { Close(); return; }
            typed = "";
            Refresh();
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ShowWrong());
        }

        IEnumerator ShowWrong()
        {
            messageText.text = wrongText;
            RectTransform r = root.transform.GetChild(0) as RectTransform;   // the box
            Vector2 home = r.anchoredPosition;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                r.anchoredPosition = home + new Vector2(Mathf.Sin(t * 70f) * 14f * (1f - t / 0.35f), 0f);
                yield return null;
            }
            r.anchoredPosition = home;
            messageRoutine = null;
        }

        void Close()
        {
            open = false;
            root.SetActive(false);
            if (!wasFrozenBefore && PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(false);
            Action a = onClosed; onClosed = null; check = null;
            a?.Invoke();
        }

        void Refresh() => answerText.text = typed.Length > 0 ? typed : "_";

        // ---------- UI (built in code, looks like the dialogue box) ----------
        void BuildUI()
        {
            DialogueBox look = Resources.Load<DialogueBox>("DialogueBox");
            TMP_FontAsset font = look != null ? look.textFont : null;
            TMP_FontAsset nameFont = look != null && look.nameFont != null ? look.nameFont : font;
            Sprite frame = look != null ? look.frame : null;
            Color textColor = look != null ? look.textColor : new Color(0.95f, 0.9f, 0.8f);
            Color gold = look != null ? look.nameColor : new Color(1f, 0.82f, 0.3f);

            root = new GameObject("CodeCanvas", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform box = Rect("Box", root.transform, new Vector2(0.5f, 0.5f), new Vector2(1100f, 560f), Vector2.zero);
            Frame(box.gameObject, frame);

            titleText = Text(Rect("Title", box, new Vector2(0.5f, 1f), new Vector2(1000f, 80f), new Vector2(0f, -70f)), nameFont, 54f, gold);
            sequenceText = Text(Rect("Sequence", box, new Vector2(0.5f, 1f), new Vector2(1000f, 90f), new Vector2(0f, -175f)), font, 50f, textColor);

            RectTransform field = Rect("Field", box, new Vector2(0.5f, 0.5f), new Vector2(340f, 100f), new Vector2(0f, -40f));
            Frame(field.gameObject, frame).color = new Color(0.8f, 0.8f, 0.85f, 1f);
            answerText = Text(Rect("Answer", field, new Vector2(0.5f, 0.5f), new Vector2(320f, 90f), Vector2.zero), font, 56f, gold);

            messageText = Text(Rect("Message", box, new Vector2(0.5f, 0f), new Vector2(1000f, 60f), new Vector2(0f, 150f)), font, 34f, new Color(1f, 0.45f, 0.4f));

            confirmRect = Rect("Confirm", box, new Vector2(0.5f, 0f), new Vector2(300f, 76f), new Vector2(0f, 75f));
            confirmFrame = Frame(confirmRect.gameObject, frame);
            Text(Rect("Text", confirmRect, new Vector2(0.5f, 0.5f), new Vector2(280f, 70f), Vector2.zero), font, 36f, textColor).text = "Confirm";

            hintText = Text(Rect("Hint", box, new Vector2(1f, 0f), new Vector2(420f, 40f), new Vector2(-230f, 28f)), font, 24f, new Color(0.75f, 0.7f, 0.6f));
            hintText.alignment = TextAlignmentOptions.BottomRight;
            hintText.text = "[Enter] confirm   [Esc] close";
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            if (anchor.y == 1f) rt.pivot = new Vector2(0.5f, 0.5f);
            if (anchor.y == 0f) rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        static Image Frame(GameObject go, Sprite frame)
        {
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            if (frame != null) { img.sprite = frame; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.25f; }
            else img.color = new Color(0.06f, 0.06f, 0.14f, 0.95f);
            return img;
        }

        static TextMeshProUGUI Text(RectTransform rt, TMP_FontAsset font, float size, Color color)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }
    }
}
