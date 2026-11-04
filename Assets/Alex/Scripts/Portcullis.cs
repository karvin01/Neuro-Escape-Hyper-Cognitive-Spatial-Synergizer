using UnityEngine;

namespace NeuroEscape
{
    /// An iron gate that slides up when opened, or just shakes (rattles) without opening.
    /// Puzzle scripts call Open() or Rattle().
    public class Portcullis : MonoBehaviour
    {
        [Tooltip("The part that moves (the bars). Empty = this object.")]
        public Transform gate;

        [Tooltip("How far it rises when opened (meters).")]
        public float openHeight = 3.2f;
        [Tooltip("Rising speed (meters per second).")]
        public float speed = 0.8f;

        [Header("Rattle")]
        public float rattleTime = 1.5f;
        public float rattleAmount = 0.04f;

        public bool IsOpen { get; private set; }

        Vector3 closedPos;
        Vector3 basePos;
        float rattleUntil;

        void Awake()
        {
            if (gate == null) gate = transform;
            closedPos = gate.localPosition;
            basePos = closedPos;
        }

        public void Open() => IsOpen = true;

        public void Rattle() => rattleUntil = Time.time + rattleTime;

        void Update()
        {
            Vector3 target = IsOpen ? closedPos + Vector3.up * openHeight : closedPos;
            basePos = Vector3.MoveTowards(basePos, target, speed * Time.deltaTime);

            Vector3 shake = Vector3.zero;
            if (Time.time < rattleUntil)
                shake = new Vector3(Mathf.Sin(Time.time * 47f), Mathf.Sin(Time.time * 61f), 0f) * rattleAmount;

            gate.localPosition = basePos + shake;
        }
    }
}
