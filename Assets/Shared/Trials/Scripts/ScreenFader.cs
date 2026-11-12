using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeuroEscape
{
    /// Black screen fade used for every teleport and scene change. Creates itself, nothing to place.
    ///   yield return ScreenFader.FadeOut(0.6f);   // to black
    ///   yield return ScreenFader.FadeIn(0.6f);    // back to the game
    /// After a scene change it fades in again by itself.
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;
        Image black;
        Coroutine running;
        float alpha;

        public static bool IsBlack => instance != null && instance.alpha > 0.99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        static ScreenFader Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("ScreenFader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenFader>();
            return instance;
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;   // above the hotbar, dialogue box and book viewer
            var img = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(transform, false);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
            black = img;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            if (alpha > 0.01f) StartCoroutine(FadeInAfterLoad());
        }

        IEnumerator FadeInAfterLoad()
        {
            yield return null;   // let the new scene place the player first
            yield return null;
            if (!SceneTravel.IsTraveling) yield return Fade(0f, 0.8f);
        }

        /// Fades to black.
        public static Coroutine FadeOut(float seconds) => Get().Run(1f, seconds);
        /// Fades from black back to the game.
        public static Coroutine FadeIn(float seconds) => Get().Run(0f, seconds);
        /// Instantly black (or clear).
        public static void Set(float a) { Get().SetAlpha(a); }

        Coroutine Run(float target, float seconds)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fade(target, seconds));
            return running;
        }

        IEnumerator Fade(float target, float seconds)
        {
            float start = alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            SetAlpha(target);
            running = null;
        }

        void SetAlpha(float a)
        {
            alpha = a;
            if (black != null) black.color = new Color(0f, 0f, 0f, a);
        }
    }
}
