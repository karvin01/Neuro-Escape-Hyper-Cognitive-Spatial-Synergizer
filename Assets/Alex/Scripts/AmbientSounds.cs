using UnityEngine;

namespace NeuroEscape
{
    /// Every so often plays a random creepy sound somewhere in the dark around the player
    /// (screams, someone running, chains, gates...). Each sound has its own volume.
    /// "Muffled" sounds play further away and dull, like they come from behind the walls,
    /// so far-away running never sounds like the player's own footsteps.
    /// Drop the Alex_AmbientSounds prefab into a scene.
    public class AmbientSounds : MonoBehaviour
    {
        [System.Serializable]
        public class Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.3f;
            [Tooltip("Plays further away and dull, as if from another room.")]
            public bool muffled;
            [Tooltip("Higher = picked more often. 0 = never.")]
            public float chance = 1f;
        }

        public Sound[] sounds;

        [Tooltip("Seconds between sounds (random between min and max).")]
        public Vector2 delay = new Vector2(10f, 25f);

        [Tooltip("How far from the player normal sounds happen (meters).")]
        public Vector2 distance = new Vector2(3f, 7f);

        [Tooltip("How far from the player muffled sounds happen (meters).")]
        public Vector2 muffledDistance = new Vector2(7f, 11f);

        [Tooltip("Turns every ambient sound up or down at once.")]
        [Range(0f, 1f)] public float masterVolume = 1f;

        public Vector2 pitchRange = new Vector2(0.92f, 1.05f);

        float nextTime;
        int lastIndex = -1;

        void Start() => nextTime = Time.time + Random.Range(delay.x, delay.y);

        void Update()
        {
            if (sounds == null || sounds.Length == 0 || Time.time < nextTime) return;
            nextTime = Time.time + Random.Range(delay.x, delay.y);

            Camera cam = Camera.main;
            Sound s = Pick();
            if (cam == null || s == null || s.clip == null) return;

            Vector2 range = s.muffled ? muffledDistance : distance;
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(range.x, range.y);
            Vector3 pos = cam.transform.position + new Vector3(dir.x, Random.Range(-0.5f, 1.5f), dir.y);

            var go = new GameObject("AmbientSound");
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = s.clip;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.dopplerLevel = 0f;
            src.volume = s.volume * masterVolume;
            src.pitch = Random.Range(pitchRange.x, pitchRange.y);
            if (s.muffled)
            {
                var lp = go.AddComponent<AudioLowPassFilter>();
                lp.cutoffFrequency = 1100f;
            }
            src.Play();
            Destroy(go, s.clip.length / Mathf.Max(0.1f, src.pitch) + 0.3f);
        }

        Sound Pick()
        {
            float total = 0f;
            foreach (Sound s in sounds) if (s != null && s.clip != null) total += Mathf.Max(0f, s.chance);
            if (total <= 0f) return null;

            for (int tries = 0; tries < 4; tries++)   // avoid the same sound twice in a row
            {
                float r = Random.Range(0f, total);
                for (int i = 0; i < sounds.Length; i++)
                {
                    Sound s = sounds[i];
                    if (s == null || s.clip == null) continue;
                    r -= Mathf.Max(0f, s.chance);
                    if (r <= 0f)
                    {
                        if (i == lastIndex && tries < 3 && sounds.Length > 1) break;
                        lastIndex = i;
                        return s;
                    }
                }
            }
            return null;
        }
    }
}
