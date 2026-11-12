using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// Title screen + pause menu. Creates itself in every scene, nothing to place.
    /// - Title screen: when the game starts in the FIRST scene of the Scene List (Main_map), and after "Main Menu".
    ///   Play / Controls / Quit. The forest stays alive behind it. The intro dialogue waits until Play.
    /// - Pause menu: Esc while playing. Resume / Controls / Main Menu / Quit. Time and sound stop.
    ///   (Not while a dialogue, book, letter or code window is open: Esc closes those first.)
    /// Controls: mouse, or W/S / arrows + Enter (or E). Esc = back / resume.
    public class GameMenu : MonoBehaviour
    {
        public static GameMenu Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.mode != Mode.Hidden;

        enum Mode { Hidden, Title, Pause }
        enum Page { Main, Controls, Confirm }

        static bool showTitleOnLoad;
        static readonly List<Action> waitingForPlay = new List<Action>();

        Mode mode = Mode.Hidden;
        Page page;
        int selected, openedFrame = -1;
        Vector2 lastMouse = new Vector2(-1f, -1f);
        bool wasFrozenBefore;
        string confirmAction;
        LetterReaderUI letterReader;

        // UI
        CanvasGroup group;
        Image dim;
        TextMeshProUGUI titleText, subtitleText, bodyText, hintText, footerText;
        RectTransform marker;
        readonly List<TextMeshProUGUI> items = new List<TextMeshProUGUI>();
        readonly List<float> slide = new List<float>();
        string[] labels = new string[0];
        Coroutine fadeRoutine;

        static readonly Color Gold = new Color(0.91f, 0.77f, 0.42f);
        static readonly Color Parchment = new Color(0.93f, 0.88f, 0.78f, 0.78f);
        static readonly Color Muted = new Color(0.85f, 0.8f, 0.7f, 0.55f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; showTitleOnLoad = true; waitingForPlay.Clear(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateOnStart()
        {
            if (Instance != null) return;
            var go = new GameObject("GameMenu");
            DontDestroyOnLoad(go);
            go.AddComponent<GameMenu>();
        }

        /// Runs 'action' now, or (while the title screen is up) right after the player presses Play.
        public static void WhenPlaying(Action action)
        {
            if (action == null) return;
            if (Instance != null && Instance.mode == Mode.Title) waitingForPlay.Add(action);
            else action();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
            group.gameObject.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (showTitleOnLoad && SceneManager.GetActiveScene().buildIndex == 0) ShowTitle();
            showTitleOnLoad = false;
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            letterReader = null;
            if (showTitleOnLoad) { showTitleOnLoad = false; ShowTitle(); }
            else if (mode != Mode.Hidden) Close();
        }

        // ------------------------------------------------------------------ open / close
        void ShowTitle()
        {
            mode = Mode.Title;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            wasFrozenBefore = false;
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);
            titleText.text = "Neuro Escape";
            titleText.fontSize = 132f;
            subtitleText.text = "THREE TRIALS, ONE WAND";
            dim.color = new Color(0f, 0f, 0f, 0f);
            ShowPage(Page.Main);
            Appear();
        }

        void OpenPause()
        {
            mode = Mode.Pause;
            wasFrozenBefore = PlayerFreeze.IsFrozen;
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            titleText.text = "Paused";
            titleText.fontSize = 96f;
            subtitleText.text = "THE TRIALS CAN WAIT";
            dim.color = new Color(0f, 0f, 0f, 0.35f);
            ShowPage(Page.Main);
            Appear();
        }

        void Close()
        {
            Mode was = mode;
            mode = Mode.Hidden;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            group.gameObject.SetActive(false);
            if (!wasFrozenBefore && PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(false);
            if (was == Mode.Title)
            {
                var run = new List<Action>(waitingForPlay);
                waitingForPlay.Clear();
                foreach (Action a in run) a();
            }
        }

        void Appear()
        {
            openedFrame = Time.frameCount;
            group.gameObject.SetActive(true);
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeGroup(0f, 1f, 0.3f));
        }

        IEnumerator FadeGroup(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            group.alpha = to;
            fadeRoutine = null;
        }

        // ------------------------------------------------------------------ pages
        void ShowPage(Page p)
        {
            page = p;
            bodyText.text = "";
            switch (p)
            {
                case Page.Main:
                    labels = mode == Mode.Title ? new[] { "Play", "Controls", "Quit" }
                                                : new[] { "Resume", "Controls", "Main Menu", "Quit" };
                    break;
                case Page.Controls:
                    bodyText.text =
                        Row("W A S D", "walk") + Row("Mouse", "look around") + Row("Shift", "run") + Row("Space", "jump") +
                        Row("E", "use, read, talk") + Row("F", "take out / put away") + Row("1 - 6", "hotbar") +
                        Row("Tab", "free the mouse") + Row("Esc", "pause menu");
                    labels = new[] { "Back" };
                    break;
                case Page.Confirm:
                    bodyText.text = confirmAction == "menu"
                        ? "Back to the main menu?\nYour progress will be lost."
                        : "Leave the game?\nYour progress will be lost.";
                    labels = new[] { "Yes", "No" };
                    break;
            }
            float itemsTop = p == Page.Main ? 40f : (p == Page.Controls ? -420f : -150f);
            for (int i = 0; i < items.Count; i++)
            {
                bool used = i < labels.Length;
                items[i].gameObject.SetActive(used);
                if (!used) continue;
                items[i].text = labels[i];
                items[i].rectTransform.anchoredPosition = new Vector2(0f, itemsTop - i * 72f);
                slide[i] = 0f;
            }
            selected = p == Page.Confirm ? 1 : 0;   // "No" is pre-selected on the confirm page
            hintText.text = "W / S or mouse to choose,  Enter to confirm" + (mode == Mode.Pause || p != Page.Main ? ",  Esc to go back" : "");
        }

        static string Row(string key, string what) => "<color=#E8C46A>" + key + "</color><pos=34%>" + what + "\n";

        void Activate(int i)
        {
            if (i < 0 || i >= labels.Length) return;
            string l = labels[i];
            switch (l)
            {
                case "Play": Close(); break;
                case "Resume": Close(); break;
                case "Controls": ShowPage(Page.Controls); break;
                case "Back": ShowPage(Page.Main); break;
                case "No": ShowPage(Page.Main); break;
                case "Main Menu": confirmAction = "menu"; ShowPage(Page.Confirm); break;
                case "Quit":
                    if (mode == Mode.Title) QuitGame();
                    else { confirmAction = "quit"; ShowPage(Page.Confirm); }
                    break;
                case "Yes":
                    if (confirmAction == "menu") StartCoroutine(BackToMainMenu());
                    else QuitGame();
                    break;
            }
        }

        IEnumerator BackToMainMenu()
        {
            mode = Mode.Hidden;
            group.gameObject.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            ClearHotbar();
            yield return ScreenFader.FadeOut(0.6f);
            showTitleOnLoad = true;
            SceneManager.LoadScene(0);   // the first scene in the Scene List; it fades back in by itself
        }

        static void ClearHotbar()
        {
            if (EquipManager.Instance != null) EquipManager.Instance.PutAway();
            Inventory inv = Inventory.Instance;
            if (inv == null) return;
            for (int i = 0; i < Inventory.SlotCount; i++)
                for (ItemData it = inv.GetSlot(i); it != null; it = inv.GetSlot(i)) inv.Remove(it);
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ input
        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (mode == Mode.Hidden)
            {
                if (kb != null && kb.escapeKey.wasPressedThisFrame && CanPause()) OpenPause();
                return;
            }

            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;   // the player controller likes to grab the mouse
            if (!Cursor.visible) Cursor.visible = true;
            Animate();
            if (Time.frameCount == openedFrame) return;

            int n = labels.Length;
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                bool moved = (pos - lastMouse).sqrMagnitude > 1f;   // only follow the mouse when it moves, so W/S still work
                lastMouse = pos;
                for (int i = 0; i < n; i++)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(items[i].rectTransform, pos, null)) continue;
                    if (moved) selected = i;
                    if (mouse.leftButton.wasPressedThisFrame) { Activate(i); return; }
                }
            }
            if (kb == null) return;
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) selected = (selected - 1 + n) % n;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % n;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
            { Activate(selected); return; }
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (page != Page.Main) ShowPage(Page.Main);
                else if (mode == Mode.Pause) Close();
            }
        }

        bool CanPause()
        {
            if (SceneTravel.IsTraveling || ScreenFader.IsBlack) return false;
            if (DialogueBox.IsOpen || InspectViewer.IsOpen || CodeInputBox.IsOpen) return false;
            if (PlayerFreeze.LastChangeFrame == Time.frameCount) return false;   // Esc just closed something else
            if (letterReader == null) letterReader = FindFirstObjectByType<LetterReaderUI>();
            if (letterReader != null && letterReader.IsOpen) return false;
            return PlayerFreeze.Instance != null;   // only in scenes with a player
        }

        void Animate()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < labels.Length; i++)
            {
                bool sel = i == selected;
                slide[i] = Mathf.MoveTowards(slide[i], sel ? 1f : 0f, dt * 7f);
                float s = Mathf.SmoothStep(0f, 1f, slide[i]);
                RectTransform rt = items[i].rectTransform;
                rt.anchoredPosition = new Vector2(34f * s, rt.anchoredPosition.y);
                items[i].color = Color.Lerp(Parchment, Gold, s);
            }
            if (selected >= 0 && selected < labels.Length)
            {
                RectTransform target = items[selected].rectTransform;
                marker.anchoredPosition = Vector2.Lerp(marker.anchoredPosition, new Vector2(8f, target.anchoredPosition.y), 1f - Mathf.Exp(-dt * 18f));
                marker.localRotation = Quaternion.Euler(0f, 0f, 45f + Mathf.Sin(Time.unscaledTime * 2f) * 6f);
            }
        }

        // ------------------------------------------------------------------ building the UI in code
        void BuildUI()
        {
            DialogueBox look = Resources.Load<DialogueBox>("DialogueBox");
            TMP_FontAsset textFont = look != null ? look.textFont : null;
            TMP_FontAsset titleFont = look != null && look.nameFont != null ? look.nameFont : textFont;

            var canvasGO = new GameObject("MenuCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;   // above dialogue/books, below the black fade
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasGO.AddComponent<CanvasGroup>();

            // full-screen dim (pause) + dark gradient from the left
            dim = Stretch(NewImage("Dim", canvasGO.transform));
            dim.color = Color.clear;
            Image grad = Stretch(NewImage("Gradient", canvasGO.transform));
            grad.sprite = GradientSprite();
            grad.color = Color.white;

            // left column
            RectTransform col = NewRect("Column", canvasGO.transform);
            col.anchorMin = col.anchorMax = col.pivot = new Vector2(0f, 0.5f);
            col.sizeDelta = new Vector2(820f, 900f);
            col.anchoredPosition = new Vector2(150f, 40f);

            titleText = MakeText("Title", col, titleFont, 132f, Gold, new Vector2(0f, 300f), new Vector2(820f, 170f));
            titleText.alignment = TextAlignmentOptions.BottomLeft;
            subtitleText = MakeText("Subtitle", col, textFont, 28f, Muted, new Vector2(4f, 196f), new Vector2(820f, 44f));
            subtitleText.characterSpacing = 18f;

            Image line = NewImage("Line", col);
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = line.rectTransform.pivot = new Vector2(0f, 0.5f);
            line.rectTransform.sizeDelta = new Vector2(380f, 2f);
            line.rectTransform.anchoredPosition = new Vector2(4f, 150f);
            line.color = new Color(Gold.r, Gold.g, Gold.b, 0.6f);

            bodyText = MakeText("Body", col, textFont, 34f, Parchment, new Vector2(4f, -150f), new Vector2(700f, 520f));
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.rectTransform.pivot = new Vector2(0f, 1f);
            bodyText.rectTransform.anchoredPosition = new Vector2(4f, 110f);
            bodyText.lineSpacing = 18f;

            // the diamond that marks the selected item
            Image m = NewImage("Marker", col);
            marker = m.rectTransform;
            marker.anchorMin = marker.anchorMax = new Vector2(0f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = new Vector2(12f, 12f);
            m.color = Gold;

            for (int i = 0; i < 4; i++)
            {
                TextMeshProUGUI t = MakeText("Item" + (i + 1), col, textFont, 50f, Parchment, Vector2.zero, new Vector2(520f, 66f));
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.margin = new Vector4(30f, 0f, 0f, 0f);
                items.Add(t);
                slide.Add(0f);
            }

            hintText = MakeText("Hint", canvasGO.transform, textFont, 22f, Muted, Vector2.zero, new Vector2(1200f, 40f));
            hintText.rectTransform.anchorMin = hintText.rectTransform.anchorMax = hintText.rectTransform.pivot = new Vector2(0f, 0f);
            hintText.rectTransform.anchoredPosition = new Vector2(154f, 50f);
            footerText = MakeText("Footer", canvasGO.transform, textFont, 22f, Muted, Vector2.zero, new Vector2(600f, 40f));
            footerText.rectTransform.anchorMin = footerText.rectTransform.anchorMax = footerText.rectTransform.pivot = new Vector2(1f, 0f);
            footerText.rectTransform.anchoredPosition = new Vector2(-60f, 50f);
            footerText.alignment = TextAlignmentOptions.BottomRight;
            footerText.text = "Alex & Karel  -  2026";

            group.alpha = 0f;
        }

        static Sprite GradientSprite()
        {
            const int w = 256;
            var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int x = 0; x < w; x++)
            {
                float t = x / (w - 1f);
                float a = 0.9f * (1f - Mathf.SmoothStep(0.15f, 0.75f, t));
                tex.SetPixel(x, 0, new Color(0.02f, 0.015f, 0.03f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f));
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Image NewImage(string name, Transform parent)
        {
            Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        static Image Stretch(Image img)
        {
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static TextMeshProUGUI MakeText(string name, Transform parent, TMP_FontAsset font, float size, Color color, Vector2 pos, Vector2 boxSize)
        {
            RectTransform rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = boxSize;
            rt.anchoredPosition = pos;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }
    }
}
