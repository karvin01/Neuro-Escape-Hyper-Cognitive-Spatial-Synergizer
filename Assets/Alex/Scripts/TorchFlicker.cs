using UnityEngine;

namespace NeuroEscape
{
    /// Makes a torch feel alive: the light flickers and the flame picture animates.
    /// Already set up on the torch prefabs; nothing to fill in.
    public class TorchFlicker : MonoBehaviour
    {
        [Tooltip("The torch's Point Light. Empty = found automatically.")]
        public Light torchLight;

        [Tooltip("The flame quads (they use a 4-frame flame picture).")]
        public Renderer[] flames;

        [Tooltip("How much the brightness wobbles.")]
        public float flickerAmount = 0.6f;

        [Tooltip("How fast the brightness wobbles.")]
        public float flickerSpeed = 7f;

        [Tooltip("Frames in the flame picture and how fast they play.")]
        public int frames = 4;
        public float framesPerSecond = 9f;

        static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock block;
        float baseIntensity;
        float seed;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            seed = Random.value * 100f;
            if (torchLight == null) torchLight = GetComponentInChildren<Light>();
            if (torchLight != null) baseIntensity = torchLight.intensity;
        }

        void Update()
        {
            float t = Time.time * flickerSpeed + seed;
            if (torchLight != null)
            {
                float n = Mathf.PerlinNoise(t, seed) * 2f - 1f;
                torchLight.intensity = Mathf.Max(0f, baseIntensity + n * flickerAmount);
            }

            if (flames == null || frames < 1) return;
            int frame = (int)((Time.time + seed) * framesPerSecond);
            float w = 1f / frames;
            for (int i = 0; i < flames.Length; i++)
            {
                Renderer r = flames[i];
                if (r == null) continue;
                int f = (frame + i * 2) % frames;
                r.GetPropertyBlock(block);
                block.SetVector(BaseMapST, new Vector4(w, 1f, f * w, 0f));
                r.SetPropertyBlock(block);
            }
        }
    }
}
