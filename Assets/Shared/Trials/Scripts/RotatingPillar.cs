using System.Collections;
using UnityEngine;

namespace NeuroEscape
{
    /// An element pillar (Stone_Pillar_Sqr_b_Elements_Color). Press E to turn it 90 degrees around Y.
    /// It is right when its chosen element (e.g. Fire) faces the Target (its pedestal).
    /// The right angle is worked out by itself from the medallions on the pillar.
    /// It starts at a random WRONG angle. When right, 'Turn On When Right' (e.g. PortalRisingLights) is switched on.
    public class RotatingPillar : Interactable
    {
        public enum Element { Water, Fire, Wind, Earth }

        [Tooltip("Which symbol must face the target.")]
        public Element element = Element.Fire;
        [Tooltip("What the symbol must face (e.g. pedestal1).")]
        public Transform target;
        [Tooltip("Switched on while the pillar is right (e.g. the pedestal's PortalRisingLights).")]
        public GameObject turnOnWhenRight;
        [Tooltip("Start at a random wrong angle (otherwise it keeps the angle from the scene).")]
        public bool randomStart = true;

        [Header("Feel")]
        public float turnSeconds = 0.6f;
        public AudioClip turnSound;
        public AudioClip rightSound;
        [Range(0f, 1f)] public float volume = 0.6f;
        public string prompt = "Press E to turn the pillar";

        public bool IsRight => step == rightStep;
        public bool Locked { get; set; }

        PillarPuzzle puzzle;
        Quaternion baseRotation;
        int step, rightStep;
        bool turning;

        public override string Prompt => turning || Locked ? "" : prompt;

        void Awake()
        {
            if (GetComponentInChildren<Collider>() == null) AddColliderAround();
            baseRotation = transform.rotation;
            rightStep = FindRightStep();
            step = 0;
            if (randomStart)
            {
                int wrong = Random.Range(1, 4);           // 1..3 steps away from right
                step = (rightStep + wrong) % 4;
                transform.rotation = RotationFor(step);
            }
            if (turnOnWhenRight != null) turnOnWhenRight.SetActive(IsRight);
        }

        void Start() => puzzle = FindFirstObjectByType<PillarPuzzle>();

        Quaternion RotationFor(int s) => Quaternion.Euler(0f, 90f * s, 0f) * baseRotation;

        // Which of the 4 angles points the element's medallion most at the target.
        int FindRightStep()
        {
            if (target == null) { Debug.LogWarning(name + ": no Target set, so no angle is right."); return -1; }
            Vector3 outLocal = ElementDirectionLocal();
            Vector3 toTarget = target.position - transform.position; toTarget.y = 0f;
            int best = 0; float bestDot = -2f;
            for (int s = 0; s < 4; s++)
            {
                Vector3 d = RotationFor(s) * outLocal; d.y = 0f;
                float dot = Vector3.Dot(d.normalized, toTarget.normalized);
                if (dot > bestDot) { bestDot = dot; best = s; }
            }
            return best;
        }

        // From the medallion child ("Medallion_Fire"...), or Karel's builder layout if it is missing.
        Vector3 ElementDirectionLocal()
        {
            string child = "Medallion_" + (element == Element.Earth ? "Rock" : element.ToString());
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != child) continue;
                Vector3 local = Quaternion.Inverse(transform.rotation) * (t.position - transform.position);
                local.y = 0f;
                if (local.sqrMagnitude > 0.0001f) return local.normalized;
            }
            switch (element)
            {
                case Element.Water: return Vector3.back;
                case Element.Fire: return Vector3.forward;
                case Element.Wind: return Vector3.right;
                default: return Vector3.left;
            }
        }

        public override void Interact()
        {
            if (turning || Locked) return;
            StartCoroutine(Turn());
        }

        IEnumerator Turn()
        {
            turning = true;
            if (turnOnWhenRight != null && IsRight) turnOnWhenRight.SetActive(false);
            Pickup.PlaySound(turnSound, volume);
            Quaternion from = transform.rotation;
            step = (step + 1) % 4;
            Quaternion to = RotationFor(step);
            for (float t = 0f; t < turnSeconds; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / turnSeconds));
                yield return null;
            }
            transform.rotation = to;
            turning = false;
            if (IsRight)
            {
                if (turnOnWhenRight != null) turnOnWhenRight.SetActive(true);
                Pickup.PlaySound(rightSound, volume);
            }
            if (puzzle != null) puzzle.Check();
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

        [ContextMenu("Test: turn to the right angle (Play mode)")]
        void TestSolve()
        {
            if (!Application.isPlaying || rightStep < 0) return;
            step = rightStep;
            transform.rotation = RotationFor(step);
            if (turnOnWhenRight != null) turnOnWhenRight.SetActive(true);
            if (puzzle != null) puzzle.Check();
        }
    }
}
