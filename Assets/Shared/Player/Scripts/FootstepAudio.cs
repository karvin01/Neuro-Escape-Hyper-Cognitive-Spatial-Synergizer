using UnityEngine;
using UnityEngine.Serialization;

namespace NeuroEscape
{
    /// Plays a footstep sound every few steps while the player walks on the ground.
    /// Put on the PlayerRig root; it finds the CharacterController by itself.
    /// Each step it looks straight down: if the floor (or a parent of it) has a FootstepSurface,
    /// that surface's sounds play. Otherwise the Default Surface plays.
    public class FootstepAudio : MonoBehaviour
    {
        [Header("Sounds per surface")]
        [FormerlySerializedAs("clips")] public AudioClip[] stoneClips;
        public AudioClip[] woodClips;
        public AudioClip[] grassClips;

        [Tooltip("Used when the floor has no FootstepSurface component.")]
        public FootstepSurfaceType defaultSurface = FootstepSurfaceType.Stone;

        [Header("Settings")]
        [Range(0f, 1f)] public float volume = 0.45f;

        [Tooltip("Meters walked between two steps (smaller player = smaller steps).")]
        public float stepDistance = 0.45f;

        public Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        CharacterController controller;
        AudioSource source;
        float travelled;
        int lastClip = -1;
        AudioClip[] lastSet;

        void Awake()
        {
            controller = GetComponentInChildren<CharacterController>();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void Update()
        {
            if (controller == null) return;

            Vector3 v = controller.velocity;
            v.y = 0f;
            float speed = v.magnitude;

            if (!controller.isGrounded || speed < 0.2f)
            {
                travelled = stepDistance * 0.7f;   // next step comes quickly after starting to walk
                return;
            }

            travelled += speed * Time.deltaTime;
            if (travelled >= stepDistance)
            {
                travelled = 0f;
                PlayStep();
            }
        }

        /// What is the player standing on right now?
        public FootstepSurfaceType CurrentSurface()
        {
            Bounds b = controller.bounds;
            if (Physics.Raycast(b.center, Vector3.down, out RaycastHit hit, b.extents.y + 0.4f,
                                ~0, QueryTriggerInteraction.Ignore))
            {
                FootstepSurface s = hit.collider.GetComponentInParent<FootstepSurface>();
                if (s != null) return s.surface;
            }
            return defaultSurface;
        }

        AudioClip[] ClipsFor(FootstepSurfaceType type)
        {
            AudioClip[] set = type == FootstepSurfaceType.Wood ? woodClips
                            : type == FootstepSurfaceType.Grass ? grassClips
                            : stoneClips;
            if (set == null || set.Length == 0) set = stoneClips;   // missing sounds -> fall back to stone
            return set;
        }

        void PlayStep()
        {
            AudioClip[] clips = ClipsFor(CurrentSurface());
            if (clips == null || clips.Length == 0) return;
            if (clips != lastSet) { lastSet = clips; lastClip = -1; }

            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == lastClip) i = (i + 1) % clips.Length;
            lastClip = i;
            if (clips[i] == null) return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clips[i], volume);
        }
    }
}
