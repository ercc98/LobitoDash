using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MagicVillageDash.Obstacles
{
    /// <summary>Where an obstacle ended up: which lane and at what Z. Used by the coin pass.</summary>
    public readonly struct ObstaclePlacement
    {
        public readonly int Lane;
        public readonly float Z;
        public ObstaclePlacement(int lane, float z) { Lane = lane; Z = z; }
    }

    public sealed class ObstacleRailGenerator : MonoBehaviour
    {
        [Header("Factories")]
        [Tooltip("One or more obstacle factories; a random one will be used.")]
        [SerializeField] private ObstacleFactory[] obstacleFactories;

        [Header("Lanes (global, symmetric)")]
        [SerializeField, Min(2)] private int laneCount = 3;
        [SerializeField] private float laneWidth = 2.5f;
        [SerializeField] private float laneCenterX = 0f;

        [Header("Rules")]
        [Range(0, 1)][SerializeField] private float obstacleChancePerLane = 0.226f;
        [Range(1, 5)][SerializeField] private int obstacleLinePerChunk = 2;
        [SerializeField] private bool ensureAtLeastOneSafeLane = true;
        
        /// <summary>
        /// Fills the chunk with obstacles and returns the per-lane blocked mask
        /// (index = lane, true = an obstacle lives in that lane for this chunk).
        /// Callers (e.g. the coin pass) can keep collectibles on the safe lanes.
        /// </summary>
        public Dictionary<int, float[]> FillRange(Transform parent, float chunkLength, float zStart, float zEnd)
        {
            if (laneCount <= 0) return null;

            Dictionary<int, float[]> blocked = new Dictionary<int, float[]>();
                
            float deltaObstaclePosition = chunkLength / obstacleLinePerChunk;

            SpawnObstacles(parent, blocked, deltaObstaclePosition, zStart);

            if (ensureAtLeastOneSafeLane)
                EnsureOneSafeLane(parent, blocked);

            return blocked;
        }

        void SpawnObstacles(Transform parent, Dictionary<int, float[]> blocked, float deltaObstaclePosition, float zStart)
        {
            for (int i = 0; i < laneCount; i++)
            {
                if (obstacleFactories == null) continue;

                for (int j = 0; j < obstacleLinePerChunk; j++)
                {
                    if (Random.value <= obstacleChancePerLane)
                    {
                        var obstacleFactory = obstacleFactories[Random.Range(0, obstacleFactories.Length)];
                        if (obstacleFactory)
                        {
                            float x = LaneX(i);
                            float z = zStart + deltaObstaclePosition * j;
                            var pos = new Vector3(x, parent.position.y, z);

                            obstacleFactory.Spawn(parent, pos, Quaternion.identity, true);
                            if (!blocked.ContainsKey(i))
                                blocked.Add(i, NewEmptyLine());
                            blocked[i][j] = z - zStart; // relative Z of the obstacle in this chunk
                        }
                    }
                }
            }
        }

        void EnsureOneSafeLane(Transform parent, Dictionary<int, float[]> blocked)
        {
            if (HasAnySafeLane(blocked)) return;

            int idx = Random.Range(0, blocked.Count);
            var hazard = FindHazardClosestToLaneX(parent, idx);
            if (hazard == null) return;

            RecycleHazard(hazard);
            blocked.Remove(idx);
        }

        bool HasAnySafeLane(Dictionary<int, float[]> blocked)
        {
            for (int i = 0; i < blocked.Count; i++)
                if (blocked.Count < laneCount) return true;
            return false;
        }

        ObstacleHazard FindHazardClosestToLaneX(Transform parent, int laneIndex)
        {
            float x = LaneX(laneIndex);

            float bestSqr = float.PositiveInfinity;
            ObstacleHazard best = null;

            foreach (Transform child in parent)
            {
                var hazard = child.GetComponent<ObstacleHazard>();
                if (hazard == null) continue;

                float dx = child.position.x - x;
                float sqr = dx * dx;

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = hazard;
                }
            }

            return best;
        }

        void RecycleHazard(ObstacleHazard hazard)
        {
            if (obstacleFactories == null) return;

            foreach (var obstacleFactory in obstacleFactories)
            {
                if (obstacleFactory)
                {
                    obstacleFactory.Recycle(hazard);
                    break;
                }
            }
        }

        /// <summary>
        /// A fresh per-lane line of obstacle slots, pre-filled with NaN so empty slots
        /// are distinguishable from a real obstacle sitting at relative Z 0.
        /// </summary>
        float[] NewEmptyLine()
        {
            var line = new float[obstacleLinePerChunk];
            for (int k = 0; k < line.Length; k++) line[k] = float.NaN;
            return line;
        }

        int MidLane() => Mathf.Clamp(laneCount / 2, 0, laneCount - 1);

        float LaneX(int laneIndex)
        {
            int mid = MidLane();
            int delta = laneIndex - mid;
            return laneCenterX + delta * laneWidth;
        }
    
    }
}
