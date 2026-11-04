using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeuroEscape
{
    /// Checks what is in the middle of the screen, shows its prompt ("Press E to ...")
    /// and calls Interact() when E is pressed. Put this on the PlayerRig root.
    public class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("How far away (in meters) the player can interact.")]
        public float reach = 2.5f;

        [Tooltip("The PromptText in the HUD.")]
        public TMP_Text promptText;

        Camera cam;

        void Update()
        {
            if (cam == null) cam = Camera.main;

            Interactable target = null;
            bool blocked = PlayerFreeze.IsFrozen || PlayerFreeze.LastChangeFrame == Time.frameCount;

            if (!blocked && cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, reach,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    target = hit.collider.GetComponentInParent<Interactable>();
                }
            }

            string prompt = target != null ? target.Prompt : "";
            if (promptText != null)
            {
                promptText.text = prompt;
                promptText.enabled = !string.IsNullOrEmpty(prompt);
            }

            Keyboard kb = Keyboard.current;
            if (target != null && kb != null && kb.eKey.wasPressedThisFrame)
                target.Interact();
        }
    }
}
