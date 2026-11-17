using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// The final reward (the wand). Works like a Pickup: press E to put it in the hotbar.
    /// Then, after 'End Delay' seconds, the screen fades to black, the end text shows, and the game ends.
    public class ClaimToWin : Pickup
    {
        [Header("The end")]
        public float endDelay = 5f;
        public float fadeSeconds = 2f;
        [TextArea(2, 5)] public string endText = "You are a real wizard now.\n\nThank you for playing Neuro Escape!";
        [Tooltip("How long the end text stays before the game closes.")]
        public float endTextSeconds = 6f;

        public override void Interact()
        {
            bool had = item != null && Inventory.Instance != null && Inventory.Instance.Has(item);
            base.Interact();
            bool has = item != null && Inventory.Instance != null && Inventory.Instance.Has(item);
            if (has && !had) EndRunner.Begin(endDelay, fadeSeconds, endText, endTextSeconds);
        }

        // Lives on its own object (the wand itself is removed when picked up).
        class EndRunner : MonoBehaviour
        {
            public static void Begin(float delay, float fade, string text, float textSeconds)
            {
                var go = new GameObject("GameEnd");
                DontDestroyOnLoad(go);
                EndRunner r = go.AddComponent<EndRunner>();
                r.StartCoroutine(r.Run(delay, fade, text, textSeconds));
            }

            IEnumerator Run(float delay, float fade, string text, float textSeconds)
            {
                yield return new WaitForSeconds(delay);
                if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(true);
                yield return ScreenFader.FadeOut(fade);
                ShowText(text);
                yield return new WaitForSecondsRealtime(textSeconds);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }

            void ShowText(string text)
            {
                if (string.IsNullOrEmpty(text)) return;
                var canvasGO = new GameObject("EndCanvas", typeof(RectTransform));
                canvasGO.transform.SetParent(transform, false);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 600;   // above the black fade
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                var go = new GameObject("Text", typeof(RectTransform));
                go.transform.SetParent(canvasGO.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(200f, 0f); rt.offsetMax = new Vector2(-200f, 0f);
                var t = go.AddComponent<TextMeshProUGUI>();
                DialogueBox look = Resources.Load<DialogueBox>("DialogueBox");
                if (look != null && look.textFont != null) t.font = look.nameFont != null ? look.nameFont : look.textFont;
                t.fontSize = 60f;
                t.color = new Color(1f, 0.85f, 0.4f);
                t.alignment = TextAlignmentOptions.Center;
                t.text = text;
                StartCoroutine(FadeTextIn(t));
            }

            static IEnumerator FadeTextIn(TextMeshProUGUI t)
            {
                for (float a = 0f; a < 1f; a += Time.unscaledDeltaTime / 1.5f) { t.alpha = a; yield return null; }
                t.alpha = 1f;
            }
        }
    }
}
