using UnityEngine;

namespace NeuroEscape
{
    /// Turns this object to face the camera (only around the up axis, so it stays upright).
    /// Used for flat pixel characters like the standing wizard.
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
