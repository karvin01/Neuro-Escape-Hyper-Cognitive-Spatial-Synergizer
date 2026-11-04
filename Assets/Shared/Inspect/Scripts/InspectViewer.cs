using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// Shows an Inspectable on screen: an open book (two pages), a single paper, or a picture with a caption.
    /// Creates itself in every scene from Shared/Inspect/Resources/InspectViewer, so there is nothing to place.
    /// Controls: A/D or arrows (or click the left/right half) = turn pages, E / Esc / right-click = close.
    public class InspectViewer : MonoBehaviour
    {
        public static InspectViewer Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        [Header("Look")]
        public TMP_FontAsset textFont;
        public TMP_FontAsset titleFont;
        public Color inkColor = new Color(0.17f, 0.1f, 0.05f);
        public Color parchmentColor = new Color(0.93f, 0.86f, 0.7f);
        [Tooltip("Used when the object has no paper of its own.")]
        public Sprite defaultPaper;
        public float maxTextSize = 34f;

        [Header("Sounds")]
        [Tooltip("Played when a page is turned (a random one).")]
        public AudioClip[] pageTurnSounds;
        [Range(0f, 1f)] public float pageTurnVolume = 0.35f;

        bool open, book, wasFrozenBefore;
        int openedFrame = -1, page;   // first shown page
        string title;
        string[] pages;
        Sprite paper;

        RectTransform content;
        GameObject pictureView;
        Image pictureImage;
        TextMeshProUGUI pictureTitle, pictureCaption, footer;
        readonly Image[] pageImages = new Image[2];
        readonly TextMeshProUGUI[] pageTitles = new TextMeshProUGUI[2], pageBodies = new TextMeshProUGUI[2];
        GameObject root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateOnStart() => EnsureExists();

        static void EnsureExists()
        {
            if (Instance != null) return;
            InspectViewer prefab = Resources.Load<InspectViewer>("InspectViewer");
            InspectViewer v = prefab != null ? Instantiate(prefab) : new GameObject().AddComponent<InspectViewer>();
            v.name = "InspectViewer";
            DontDestroyOnLoad(v.gameObject);
        }

        /// Shows text pages (and optionally a picture). openBook = two pages side by side.
        public static bool Show(string title, string[] pages, Sprite paper, Sprite picture, bool openBook)
        {
            EnsureExists();
            return Instance.Begin(title, pages, paper, picture, openBook);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
            root.SetActive(false);
        }

        bool Begin(string t, string[] p, Sprite pap, Sprite picture, bool openBook)
        {
            if (open) return false;
            title = t;
            pages = p != null && p.Length > 0 ? p : new[] { "" };
            paper = pap != null ? pap : defaultPaper;
            page = 0;

            wasFrozenBefore = PlayerFreeze.IsFrozen;
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);
            open = true;
            openedFrame = Time.frameCount;
            root.SetActive(true);

            bool isPicture = picture != null;
            book = openBook && !isPicture;
            pictureView.SetActive(isPicture);
            for (int i = 0; i < 2; i++) pageImages[i].gameObject.SetActive(!isPicture && (i == 0 || book));

            if (isPicture) ShowPicture(picture);
            else { LayoutPages(); ShowPages(); }
            return true;
        }

        void Close()
        {
            open = false;
            root.SetActive(false);
            if (!wasFrozenBefore && PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(false);
        }

        void Update()
        {
            if (!open) return;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
            if (Time.frameCount == openedFrame) return;   // the E that opened it must not close it

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if ((kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.rightButton.wasPressedThisFrame)) { Close(); return; }

            if (pictureView.activeSelf) return;
            int step = book ? 2 : 1;
            bool next = kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame);
            bool prev = kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame);
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (mouse.position.ReadValue().x > Screen.width * 0.5f) next = true; else prev = true;
            }
            if (next && page + step < pages.Length) { page += step; ShowPages(); PageSound(); }
            else if (prev && page > 0) { page = Mathf.Max(0, page - step); ShowPages(); PageSound(); }
        }

        void PageSound()
        {
            if (pageTurnSounds == null || pageTurnSounds.Length == 0) return;
            AudioClip c = pageTurnSounds[Random.Range(0, pageTurnSounds.Length)];
            if (c != null) Pickup.PlaySound(c, pageTurnVolume);
        }

        // ---------- filling the pages ----------

        void ShowPages()
        {
            for (int i = 0; i < 2; i++)
            {
                if (!pageImages[i].gameObject.activeSelf) continue;
                int index = page + i;
                bool has = index < pages.Length;
                pageTitles[i].text = index == 0 ? title : "";
                pageBodies[i].text = has ? pages[index] : "";
                SetBodyArea(pageBodies[i].rectTransform, index == 0);
            }
            int total = book ? Mathf.CeilToInt(pages.Length / 2f) : pages.Length;
            int current = book ? page / 2 + 1 : page + 1;
            footer.text = total > 1
                ? "[A] previous        " + current + " / " + total + "        next [D]\n<size=70%>E to close</size>"
                : "<size=70%>E to close</size>";
        }

        static void SetBodyArea(RectTransform body, bool belowTitle)
        {
            body.anchorMin = new Vector2(0.13f, 0.12f);
            body.anchorMax = new Vector2(0.87f, belowTitle ? 0.74f : 0.88f);
            body.offsetMin = body.offsetMax = Vector2.zero;
        }

        void LayoutPages()
        {
            // fit one or two papers (keeping their shape) into the middle of the screen
            float aspect = paper != null ? paper.rect.width / paper.rect.height : 0.75f;
            int count = book ? 2 : 1;
            Vector2 area = new Vector2(1640f, 860f);
            float h = area.y, w = h * aspect;
            if (w * count > area.x) { w = area.x / count; h = w / aspect; }
            for (int i = 0; i < 2; i++)
            {
                Image img = pageImages[i];
                img.sprite = paper;
                img.color = paper != null ? Color.white : parchmentColor;
                RectTransform rt = img.rectTransform;
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = count == 1 ? Vector2.zero : new Vector2((i == 0 ? -0.5f : 0.5f) * (w + 6f), 0f);
            }
        }

        void ShowPicture(Sprite picture)
        {
            pictureImage.sprite = picture;
            pictureTitle.text = title;
            pictureCaption.text = pages[0];
            footer.text = "<size=70%>E to close</size>";
        }

        // ---------- building the UI (no prefab editing needed) ----------

        void BuildUI()
        {
            var canvasGO = new GameObject("InspectCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasGO;

            RectTransform dim = Rect("Dim", canvasGO.transform);
            Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.72f);
            dimImg.raycastTarget = false;

            content = Rect("Content", canvasGO.transform);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = new Vector2(0f, 40f);

            for (int i = 0; i < 2; i++)
            {
                RectTransform pr = Rect("Page" + (i + 1), content);
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
                pageImages[i] = pr.gameObject.AddComponent<Image>();
                pageImages[i].raycastTarget = false;

                RectTransform tr = Rect("Title", pr);
                tr.anchorMin = new Vector2(0.12f, 0.76f);
                tr.anchorMax = new Vector2(0.88f, 0.9f);
                tr.offsetMin = tr.offsetMax = Vector2.zero;
                pageTitles[i] = Text(tr, titleFont != null ? titleFont : textFont, 46f, inkColor, TextAlignmentOptions.Center);
                pageTitles[i].enableAutoSizing = true;
                pageTitles[i].fontSizeMin = 20f;
                pageTitles[i].fontSizeMax = 46f;

                RectTransform br = Rect("Text", pr);
                SetBodyArea(br, true);
                pageBodies[i] = Text(br, textFont, maxTextSize, inkColor, TextAlignmentOptions.TopLeft);
                pageBodies[i].enableAutoSizing = true;
                pageBodies[i].fontSizeMin = 14f;
                pageBodies[i].fontSizeMax = maxTextSize;
            }

            // picture + caption
            RectTransform pv = Rect("Picture", canvasGO.transform);
            Stretch(pv);
            pictureView = pv.gameObject;
            RectTransform pt = Rect("Title", pv);
            pt.anchorMin = new Vector2(0.1f, 0.88f); pt.anchorMax = new Vector2(0.9f, 0.97f);
            pt.offsetMin = pt.offsetMax = Vector2.zero;
            pictureTitle = Text(pt, titleFont != null ? titleFont : textFont, 52f, parchmentColor, TextAlignmentOptions.Center);
            RectTransform pi = Rect("Image", pv);
            pi.anchorMin = new Vector2(0.1f, 0.24f); pi.anchorMax = new Vector2(0.9f, 0.86f);
            pi.offsetMin = pi.offsetMax = Vector2.zero;
            pictureImage = pi.gameObject.AddComponent<Image>();
            pictureImage.preserveAspect = true;
            pictureImage.raycastTarget = false;
            RectTransform pc = Rect("Caption", pv);
            pc.anchorMin = new Vector2(0.15f, 0.1f); pc.anchorMax = new Vector2(0.85f, 0.22f);
            pc.offsetMin = pc.offsetMax = Vector2.zero;
            pictureCaption = Text(pc, textFont, 32f, parchmentColor, TextAlignmentOptions.Top);
            pictureCaption.enableAutoSizing = true;
            pictureCaption.fontSizeMin = 16f;
            pictureCaption.fontSizeMax = 32f;

            // footer
            RectTransform fr = Rect("Footer", canvasGO.transform);
            fr.anchorMin = new Vector2(0f, 0f); fr.anchorMax = new Vector2(1f, 0f);
            fr.pivot = new Vector2(0.5f, 0f);
            fr.sizeDelta = new Vector2(0f, 90f);
            fr.anchoredPosition = new Vector2(0f, 14f);
            footer = Text(fr, textFont, 30f, parchmentColor, TextAlignmentOptions.Bottom);
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI Text(RectTransform rt, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
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
