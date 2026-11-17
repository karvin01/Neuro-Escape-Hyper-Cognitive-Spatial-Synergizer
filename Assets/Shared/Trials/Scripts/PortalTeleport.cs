using UnityEngine;

namespace NeuroEscape
{
    /// The portal door. Asleep until Activate() (the Pillar Puzzle calls it); then the purple glow
    /// fades in and pressing E sends the player to the Travel Point (or scene) below, with a fade.
    public class PortalTeleport : Interactable
    {
        [Tooltip("Travel Point Id to go to (e.g. Library1).")]
        public string travelPoint = "Library1";
        [Tooltip("Optional. Another scene to load instead (then Travel Point is a point in that scene, or empty).")]
        public string otherScene;

        [Header("Look")]
        [Tooltip("Switched on when the portal wakes up (the purple inside).")]
        public GameObject glow;
        [Tooltip("Optional light that pulses with the glow.")]
        public Light glowLight;
        public float lightIntensity = 3f;
        public AudioClip activateSound;
        public AudioClip enterSound;
        [Range(0f, 1f)] public float volume = 0.7f;
        public string prompt = "Press E to step through the portal";
        public bool startsActive;

        public bool IsActive { get; private set; }
        float shown;
        Renderer[] glowRenderers;
        MaterialPropertyBlock block;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        Color baseColor = new Color(0.65f, 0.25f, 1f, 1f);

        public override string Prompt => IsActive && !SceneTravel.IsTraveling ? prompt : "";

        void Awake()
        {
            if (glow != null)
            {
                glowRenderers = glow.GetComponentsInChildren<Renderer>(true);
                if (glowRenderers.Length > 0 && glowRenderers[0].sharedMaterial != null && glowRenderers[0].sharedMaterial.HasProperty(ColorId))
                    baseColor = glowRenderers[0].sharedMaterial.GetColor(ColorId);
                glow.SetActive(false);
            }
            block = new MaterialPropertyBlock();
            if (startsActive) Activate();
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            if (glow != null) glow.SetActive(true);
            Pickup.PlaySound(activateSound, volume);
        }

        void Update()
        {
            if (!IsActive || glow == null) return;
            shown = Mathf.MoveTowards(shown, 1f, Time.deltaTime / 1.5f);   // fade in over 1.5 s
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 2.2f);
            if (glowRenderers != null)
                foreach (Renderer r in glowRenderers)
                {
                    r.GetPropertyBlock(block);
                    block.SetColor(ColorId, baseColor * (shown * pulse));
                    r.SetPropertyBlock(block);
                }
            if (glowLight != null) glowLight.intensity = lightIntensity * shown * pulse;
        }

        public override void Interact()
        {
            if (!IsActive || SceneTravel.IsTraveling) return;
            Pickup.PlaySound(enterSound, volume);
            if (!string.IsNullOrEmpty(otherScene)) SceneTravel.GoToScene(otherScene, travelPoint);
            else SceneTravel.GoTo(travelPoint);
        }

        [ContextMenu("Test: wake the portal (Play mode)")]
        void TestActivate() { if (Application.isPlaying) Activate(); }
    }
}
