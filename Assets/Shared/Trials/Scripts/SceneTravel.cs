using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuroEscape
{
    /// Moves the player with a fade to black:
    ///   SceneTravel.GoTo("Library1");               // a Travel Point in this scene
    ///   SceneTravel.GoToScene("Map1_Alex");         // another scene (its normal start)
    ///   SceneTravel.GoToScene("Main_map", "WandRoom"); // a Travel Point in another scene
    /// The scene must be in File > Build Profiles > Scene List.
    public class SceneTravel : MonoBehaviour
    {
        public const float FadeSeconds = 0.7f;

        static SceneTravel runner;
        static string pendingPoint;     // point to arrive at after a scene change
        static bool sceneStartHandled;

        /// True while fading/teleporting (other scripts can ignore input meanwhile).
        public static bool IsTraveling { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { runner = null; pendingPoint = null; IsTraveling = false; sceneStartHandled = false; }

        static SceneTravel Runner()
        {
            if (runner != null) return runner;
            var go = new GameObject("SceneTravel");
            DontDestroyOnLoad(go);
            runner = go.AddComponent<SceneTravel>();
            SceneManager.sceneLoaded += (s, m) => { sceneStartHandled = false; if (runner != null) runner.StartCoroutine(runner.CheckArrival()); };
            return runner;
        }

        // If the scene had no Travel Point with the wanted Id, forget it (so the next scene starts normally).
        IEnumerator CheckArrival()
        {
            yield return null;
            yield return null;
            if (pendingPoint != null)
            {
                Debug.LogWarning("SceneTravel: no Travel Point with Id '" + pendingPoint + "' in scene " + SceneManager.GetActiveScene().name + ".");
                pendingPoint = null;
            }
        }

        public static void GoTo(string pointId)
        {
            if (IsTraveling) return;
            Runner().StartCoroutine(Runner().TravelHere(pointId));
        }

        public static void GoToScene(string sceneName, string pointId = null)
        {
            if (IsTraveling || string.IsNullOrEmpty(sceneName)) return;
            Runner().StartCoroutine(Runner().TravelToScene(sceneName, pointId));
        }

        IEnumerator TravelHere(string pointId)
        {
            TravelPoint p = TravelPoint.Find(pointId);
            if (p == null) { Debug.LogWarning("SceneTravel: no Travel Point with Id '" + pointId + "' in this scene."); yield break; }
            IsTraveling = true;
            Freeze(true);
            yield return ScreenFader.FadeOut(FadeSeconds);
            PlacePlayer(p);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return ScreenFader.FadeIn(FadeSeconds);
            IsTraveling = false;
            Freeze(false);
            Arrived(p);
        }

        IEnumerator TravelToScene(string sceneName, string pointId)
        {
            IsTraveling = true;
            Freeze(true);
            yield return ScreenFader.FadeOut(FadeSeconds);
            pendingPoint = string.IsNullOrEmpty(pointId) ? null : pointId;
            IsTraveling = false;   // the new scene fades itself in
            SceneManager.LoadScene(sceneName);
        }

        /// Called by every Travel Point when its scene starts.
        internal static void OnPointReady(TravelPoint p)
        {
            Runner();
            bool arriving = pendingPoint != null;
            if (arriving ? p.id != pendingPoint : (!p.startOfScene || sceneStartHandled)) return;
            pendingPoint = null;
            sceneStartHandled = true;
            PlacePlayer(p);
            Arrived(p);
        }

        static void Arrived(TravelPoint p)
        {
            if (p.clearHotbarOnArrival) ClearHotbar();
            if (p.arrivalDialogue != null) GameMenu.WhenPlaying(() => DialogueBox.Play(p.arrivalDialogue));   // waits for "Play" on the title screen
        }

        static void ClearHotbar()
        {
            if (EquipManager.Instance != null) EquipManager.Instance.PutAway();
            Inventory inv = Inventory.Instance;
            if (inv == null) return;
            for (int i = 0; i < Inventory.SlotCount; i++)
                for (ItemData it = inv.GetSlot(i); it != null; it = inv.GetSlot(i)) inv.Remove(it);
        }

        static void Freeze(bool on)
        {
            if (PlayerFreeze.Instance != null) PlayerFreeze.Instance.SetFrozen(on);
        }

        /// Puts the player on the point, facing its forward, with its look pitch.
        public static void PlacePlayer(TravelPoint p)
        {
            CharacterController cc = FindFirstObjectByType<CharacterController>();
            if (cc == null) { Debug.LogWarning("SceneTravel: no player (Character Controller) found."); return; }
            bool was = cc.enabled;
            cc.enabled = false;   // a Character Controller ignores position changes while enabled
            cc.transform.SetPositionAndRotation(p.transform.position, Quaternion.Euler(0f, p.transform.eulerAngles.y, 0f));
            cc.enabled = was;
            Physics.SyncTransforms();
            SetPitch(cc.gameObject, p.lookPitch);
        }

        // The Starter Assets FirstPersonController keeps the up/down look in a private field.
        static void SetPitch(GameObject player, float pitch)
        {
            foreach (MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "FirstPersonController") continue;
                System.Type t = mb.GetType();
                FieldInfo pitchField = t.GetField("_cinemachineTargetPitch", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pitchField != null) pitchField.SetValue(mb, pitch);
                FieldInfo targetField = t.GetField("CinemachineCameraTarget", BindingFlags.Instance | BindingFlags.Public);
                if (targetField != null && targetField.GetValue(mb) is GameObject target && target != null)
                    target.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                return;
            }
        }
    }
}
