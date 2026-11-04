using System.Collections.Generic;
using UnityEngine;

namespace NeuroEscape
{
    /// Moves each object to a different random spawn point.
    /// - Key with 3 possible spots: Objects = the key, Spawn Points = 3 empty markers.
    /// - Pages that swap places: Objects = the pages, Spawn Points = at least as many markers.
    /// - Key that appears after a puzzle: untick Spawn On Start; the puzzle calls Spawn().
    /// Spawn points are empty objects; their rotation is used too. They show as yellow spheres in the Scene view.
    public class RandomSpawner : MonoBehaviour
    {
        [Tooltip("Objects already in the scene that should be placed randomly.")]
        public GameObject[] objects;

        [Tooltip("Possible positions (empty objects). Need at least as many as Objects.")]
        public Transform[] spawnPoints;

        [Tooltip("Untick if a puzzle should make the objects appear later (it calls Spawn()).")]
        public bool spawnOnStart = true;

        public bool HasSpawned { get; private set; }

        void Awake()
        {
            if (!spawnOnStart)
                foreach (GameObject o in objects)
                    if (o != null) o.SetActive(false);
        }

        void Start()
        {
            if (spawnOnStart) Spawn();
        }

        /// Places the objects. Can be called by a puzzle (e.g. from an AnswerButton's On Correct event).
        public void Spawn()
        {
            if (HasSpawned) return;
            HasSpawned = true;

            var points = new List<Transform>();
            foreach (Transform p in spawnPoints)
                if (p != null) points.Add(p);

            // Shuffle the points.
            for (int i = points.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (points[i], points[j]) = (points[j], points[i]);
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                if (i >= points.Count)
                {
                    Debug.LogWarning("RandomSpawner on " + name + ": not enough spawn points for all objects.");
                    break;
                }
                objects[i].transform.SetPositionAndRotation(points[i].position, points[i].rotation);
                objects[i].SetActive(true);
            }
        }

        void OnDrawGizmos()
        {
            if (spawnPoints == null) return;
            Gizmos.color = Color.yellow;
            foreach (Transform p in spawnPoints)
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.2f);
        }
    }
}
