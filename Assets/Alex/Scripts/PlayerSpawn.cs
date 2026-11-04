using UnityEngine;

namespace NeuroEscape
{
    /// Where the player starts. When the scene loads, the player is moved here, facing this object's
    /// forward (blue arrow). If the player falls below 'Fall Height' (e.g. through a gap in the floor),
    /// they are put back here. Put it on an empty object (e.g. "Spawn"); move or turn that object to
    /// change the start. Shows as a green capsule in the Scene view.
    public class PlayerSpawn : MonoBehaviour
    {
        [Tooltip("The player to move. Empty = found automatically (the object with the Character Controller).")]
        public CharacterController player;

        [Tooltip("Move the player here when the scene starts.")]
        public bool placeOnStart = true;

        [Tooltip("When the player gets below this height (Y), they are put back here.")]
        public float fallHeight = -10f;

        [Tooltip("Optional. Played when the player is put back after a fall.")]
        public AudioClip respawnSound;
        [Range(0f, 1f)] public float respawnVolume = 0.5f;

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<CharacterController>();
            if (player == null) { Debug.LogWarning("PlayerSpawn: no player (Character Controller) found in the scene."); return; }
            if (placeOnStart) MovePlayerHere();
        }

        void Update()
        {
            if (player != null && player.transform.position.y < fallHeight)
            {
                MovePlayerHere();
                Pickup.PlaySound(respawnSound, respawnVolume);
            }
        }

        /// Puts the player on this spot. Can also be called from an event (e.g. a trap).
        public void MovePlayerHere()
        {
            if (player == null) return;
            bool wasEnabled = player.enabled;
            player.enabled = false;   // a Character Controller ignores position changes while enabled
            player.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            player.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(p + Vector3.up * 0.3f, 0.3f);
            Gizmos.DrawWireSphere(p + Vector3.up * 1.5f, 0.3f);
            Gizmos.DrawLine(p + new Vector3(0.3f, 0.3f, 0f), p + new Vector3(0.3f, 1.5f, 0f));
            Gizmos.DrawLine(p + new Vector3(-0.3f, 0.3f, 0f), p + new Vector3(-0.3f, 1.5f, 0f));
            Gizmos.color = Color.blue;
            Vector3 f = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;
            Gizmos.DrawLine(p + Vector3.up * 1.5f, p + Vector3.up * 1.5f + f * 0.8f);
        }
    }
}
