using UnityEngine;
using UnityEngine.Events;

namespace NeuroEscape
{
    /// Put on ANY object (a book, a picture, a gravestone...) to make it inspectable:
    /// look at it and press E to open it on screen. Nothing is picked up.
    /// - Look = Open Book: the pages are shown two at a time, like an open book.
    /// - Look = Paper:     one page at a time.
    /// - Look = Picture:   a big picture with the first page as its caption.
    /// A/D or the arrow keys (or a click) turn the pages, E / Esc / right-click closes.
    /// No collider on the object? One is added automatically around it.
    public class Inspectable : Interactable
    {
        public enum Look { OpenBook, Paper, Picture }

        [Header("What you see")]
        [Tooltip("Name shown in the prompt and as the heading. Change it to anything.")]
        public string title = "Old Book";
        public Look look = Look.OpenBook;
        [Tooltip("The text, one entry per page.")]
        [TextArea(3, 12)] public string[] pages;
        [Tooltip("Paper picture behind the text (e.g. Alex_Paper_Full). Empty = plain parchment.")]
        public Sprite paper;
        [Tooltip("Look = Picture: the picture to show (import the image as Texture Type 'Sprite (2D and UI)').")]
        public Sprite picture;
        [Tooltip("Optional. Also put the picture on this object in the world (e.g. the canvas of a frame).")]
        public Renderer showPictureOn;
        [Tooltip("Optional. A Book Text file (Create > Neuro Escape > Book Text): its title and pages are used instead of the ones above.")]
        public BookData book;
        [Tooltip("Optional. Show an existing page/letter item instead: its name, text and paper are used.")]
        public ItemData item;

        [Header("Interaction")]
        [Tooltip("Empty = 'Press E to read <title>' (or 'look at' for pictures).")]
        public string prompt;
        public AudioClip openSound;
        [Range(0f, 1f)] public float openVolume = 0.5f;
        [Tooltip("Runs every time it is opened (e.g. count found easter eggs).")]
        public UnityEvent onInspected;

        public int TimesInspected { get; private set; }

        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        void Awake()
        {
            if (GetComponentInChildren<Collider>() == null) AddColliderAround();
            ApplyPictureToWorld();
        }

        void OnValidate() => ApplyPictureToWorld();

        void ApplyPictureToWorld()
        {
            if (showPictureOn == null || picture == null) return;
            var block = new MaterialPropertyBlock();
            showPictureOn.GetPropertyBlock(block);
            block.SetTexture(BaseMap, picture.texture);
            showPictureOn.SetPropertyBlock(block);
        }

        void AddColliderAround()
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            var box = gameObject.AddComponent<BoxCollider>();
            if (rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            foreach (Renderer r in rends) b.Encapsulate(r.bounds);
            Vector3 s = transform.lossyScale;
            box.center = transform.InverseTransformPoint(b.center);
            box.size = new Vector3(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
                                   b.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                                   b.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
        }

        public string Title => item != null ? item.itemName : book != null ? book.title : title;

        public override string Prompt
        {
            get
            {
                if (InspectViewer.IsOpen || DialogueBox.IsOpen) return "";
                if (!string.IsNullOrEmpty(prompt)) return prompt;
                return (look == Look.Picture ? "Press E to look at " : "Press E to read ") + Title;
            }
        }

        public override void Interact() => Open();

        /// Opens it on screen (also usable from events).
        public void Open()
        {
            if (InspectViewer.IsOpen || DialogueBox.IsOpen) return;
            string[] text = item != null ? new[] { item.text } : book != null ? book.pages : pages;
            Sprite p = item != null && item.paper != null ? item.paper : paper;
            if (!InspectViewer.Show(Title, text, p, look == Look.Picture ? picture : null, look == Look.OpenBook)) return;
            if (openSound != null) Pickup.PlaySound(openSound, openVolume);
            TimesInspected++;
            onInspected?.Invoke();
        }
    }
}
