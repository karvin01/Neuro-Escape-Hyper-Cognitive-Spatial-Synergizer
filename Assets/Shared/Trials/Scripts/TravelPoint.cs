using UnityEngine;

namespace NeuroEscape
{
    /// A place the player can be sent to (by a portal, a code door, the bonfire...).
    /// Put it on an empty object, move and turn it in the Scene view (blue arrow = where the player looks).
    /// - Id: the name other scripts use (e.g. "Library1").
    /// - Start Of Scene: the player starts here when the scene is played normally (not arriving from somewhere else).
    /// Shows as a purple capsule in the Scene view.
    public class TravelPoint : MonoBehaviour
    {
        public string id = "Point";

        [Tooltip("Tick on ONE point per scene: the player starts here when this scene is started normally.")]
        public bool startOfScene;

        [Tooltip("Look up (negative) or down (positive) when arriving, in degrees. 0 = straight ahead.")]
        [Range(-60f, 60f)] public float lookPitch;

        [Tooltip("Optional. Said when the player arrives here.")]
        public DialogueData arrivalDialogue;

        [Tooltip("Empty the hotbar when arriving (e.g. before the final reward).")]
        public bool clearHotbarOnArrival;

        void Start() => SceneTravel.OnPointReady(this);

        public static TravelPoint Find(string pointId)
        {
            foreach (TravelPoint p in FindObjectsByType<TravelPoint>(FindObjectsSortMode.None))
                if (p.id == pointId) return p;
            return null;
        }

        [ContextMenu("Test: travel here (Play mode)")]
        void TestTravel() { if (Application.isPlaying) SceneTravel.GoTo(id); }

        void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            Gizmos.color = startOfScene ? new Color(0.3f, 1f, 0.4f) : new Color(0.75f, 0.35f, 1f);
            Gizmos.DrawWireSphere(p + Vector3.up * 0.3f, 0.3f);
            Gizmos.DrawWireSphere(p + Vector3.up * 1.5f, 0.3f);
            Gizmos.DrawLine(p + new Vector3(0.3f, 0.3f, 0f), p + new Vector3(0.3f, 1.5f, 0f));
            Gizmos.DrawLine(p + new Vector3(-0.3f, 0.3f, 0f), p + new Vector3(-0.3f, 1.5f, 0f));
            Gizmos.color = Color.blue;
            Vector3 f = Quaternion.Euler(lookPitch, transform.eulerAngles.y, 0f) * Vector3.forward;
            Gizmos.DrawLine(p + Vector3.up * 1.5f, p + Vector3.up * 1.5f + f * 1f);
        }
    }
}
