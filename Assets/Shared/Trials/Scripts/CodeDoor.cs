using UnityEngine;

namespace NeuroEscape
{
    /// A door with a number riddle. Press E: a window shows the sequence with [ ? ];
    /// typing the right number sends the player on (with a fade) to a Travel Point or another scene.
    public class CodeDoor : Interactable
    {
        public string title = "The Sequence";
        [Tooltip("What is shown, e.g. 4, 9, 7, 12, 10, 15, [ ? ]")]
        public string sequence = "4, 9, 7, 12, 10, 15, [ ? ]";
        [Tooltip("The right number.")]
        public string answer = "13";

        [Header("Where it leads")]
        [Tooltip("Travel Point Id to go to (e.g. Library2).")]
        public string travelPoint = "Library2";
        [Tooltip("Optional. Another scene to load instead (e.g. Map1_Alex). Then Travel Point is a point in that scene, or empty.")]
        public string otherScene;

        [Header("Feel")]
        public string prompt = "Press E to look at the door";
        public AudioClip rightSound;
        public AudioClip wrongSound;
        [Range(0f, 1f)] public float volume = 0.6f;

        public bool Solved { get; private set; }

        public override string Prompt => Solved || SceneTravel.IsTraveling || CodeInputBox.IsOpen ? "" : prompt;

        void Awake()
        {
            if (GetComponentInChildren<Collider>() == null)
            {
                Renderer[] rends = GetComponentsInChildren<Renderer>();
                var box = gameObject.AddComponent<BoxCollider>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    foreach (Renderer r in rends) b.Encapsulate(r.bounds);
                    Vector3 s = transform.lossyScale;
                    box.center = transform.InverseTransformPoint(b.center);
                    box.size = new Vector3(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
                                           b.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                                           b.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
                }
            }
        }

        public override void Interact()
        {
            if (Solved || SceneTravel.IsTraveling) return;
            CodeInputBox.Show(title, sequence, IsRight, OnClosed);
        }

        bool IsRight(string typed)
        {
            bool right = typed.Trim() == answer.Trim();
            Pickup.PlaySound(right ? rightSound : wrongSound, volume);
            if (right) Solved = true;
            return right;
        }

        void OnClosed()
        {
            if (!Solved) return;
            if (!string.IsNullOrEmpty(otherScene)) SceneTravel.GoToScene(otherScene, travelPoint);
            else SceneTravel.GoTo(travelPoint);
        }
    }
}
