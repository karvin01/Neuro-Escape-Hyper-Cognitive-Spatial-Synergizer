using UnityEngine;

namespace NeuroEscape
{
    /// Slowly turns this object around the world Y axis (e.g. the wand), with an optional gentle bob.
    public class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = 40f;
        [Tooltip("Up and down movement in meters (0 = none).")]
        public float bobHeight = 0.05f;
        public float bobSpeed = 1.5f;
        Vector3 home;

        void Start() => home = transform.position;

        void Update()
        {
            transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
            if (bobHeight > 0f) transform.position = home + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }
    }
}
