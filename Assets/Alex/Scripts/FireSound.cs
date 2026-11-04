using UnityEngine;

namespace NeuroEscape
{
    /// Crackling fire sound for torches. Loops, starts at a random point (so torches next to each other
    /// don't sound identical) and stops automatically when the object is hidden (e.g. torch taken from its bracket).
    public class FireSound : MonoBehaviour
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;

        [Tooltip("1 = sound comes from the torch (gets quieter far away). Lower = more 'in your head' (used for the torch in hand).")]
        [Range(0f, 1f)] public float spatial = 1f;

        [Tooltip("Full volume inside this distance (meters).")]
        public float minDistance = 1f;
        [Tooltip("Silent beyond this distance (meters).")]
        public float maxDistance = 7f;

        public Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        AudioSource source;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume;
            source.spatialBlend = spatial;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        }

        void OnEnable()
        {
            if (source == null || clip == null) return;
            source.time = Random.Range(0f, clip.length * 0.95f);
            source.Play();
        }

        void OnDisable()
        {
            if (source != null) source.Stop();
        }
    }
}
