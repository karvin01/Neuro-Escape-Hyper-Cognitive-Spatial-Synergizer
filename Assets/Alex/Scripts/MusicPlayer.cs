using UnityEngine;

namespace NeuroEscape
{
    /// Plays background music on a loop, fading in at the start. Drop the Alex_Music prefab into a scene.
    public class MusicPlayer : MonoBehaviour
    {
        public AudioClip music;
        [Range(0f, 1f)] public float volume = 0.35f;
        public float fadeInSeconds = 4f;

        AudioSource source;

        void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = music;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            if (music != null) source.Play();
        }

        void Update()
        {
            if (source == null) return;
            float speed = volume / Mathf.Max(0.01f, fadeInSeconds);
            source.volume = Mathf.MoveTowards(source.volume, volume, speed * Time.deltaTime);
        }
    }
}
