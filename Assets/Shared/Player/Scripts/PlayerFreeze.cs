using System.Collections.Generic;
using UnityEngine;

namespace NeuroEscape
{
    /// Freezes the player (movement + mouse look) and frees the mouse while a letter or menu is open.
    /// Put this on the PlayerRig root. It finds the Starter Assets FirstPersonController by itself.
    public class PlayerFreeze : MonoBehaviour
    {
        public static PlayerFreeze Instance { get; private set; }
        public static bool IsFrozen { get; private set; }
        public static int LastChangeFrame { get; private set; } = -1;

        [Tooltip("Optional. Leave empty: the FirstPersonController is found automatically.")]
        public MonoBehaviour[] disableWhileFrozen;

        void Awake()
        {
            Instance = this;
            IsFrozen = false;

            if (disableWhileFrozen == null || disableWhileFrozen.Length == 0)
            {
                var found = new List<MonoBehaviour>();
                foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && mb.GetType().Name == "FirstPersonController")
                        found.Add(mb);
                }
                disableWhileFrozen = found.ToArray();

                if (found.Count == 0)
                    Debug.LogWarning("PlayerFreeze: no FirstPersonController found under " + name +
                                     ". Drag it into 'Disable While Frozen'.");
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsFrozen = false;
            }
        }

        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
            LastChangeFrame = Time.frameCount;

            foreach (MonoBehaviour b in disableWhileFrozen)
                if (b != null) b.enabled = !frozen;

            Cursor.lockState = frozen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = frozen;
        }
    }
}
