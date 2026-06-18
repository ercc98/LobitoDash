using System.Collections.Generic;
using ErccDev.Foundation.Core.Factories;
using UnityEngine;

namespace MagicVillageDash.Collectibles
{

    public sealed class CoinRailGenerator : MonoBehaviour
    {
        [Header("Factory")]
        [SerializeField] private CoinFactory coinFactoryProvider;
        IFactory<CoinCollectible> iCoinFactory;

        [Header("Lanes (global, symmetric)")]
        [SerializeField, Min(2)] private int laneCount = 3;
        [SerializeField] private float laneWidth = 2.5f;  // distance between lane centers
        [SerializeField] private float laneCenterX = 0f;  // middle lane X

        [Header("Rail Shape")]
        [SerializeField] private float coinSpacingZ = 1.8f;    // distance between coins along Z
        [SerializeField] private float minStraightZ = 8f;      // min straight distance before a turn
        [SerializeField] private float maxStraightZ = 18f;     // max straight distance before a turn
        [SerializeField] private float transitionLengthZ = 12f;// how long to move from lane A to B
        [Range(0, 1)]
        [SerializeField] private float laneChangeChance = 0.35f;// chance per chunk the rail drifts to an adjacent lane (variety, not obstacle dodging)
        [SerializeField] private float coinHeight = 1.0f;      // Y position for coins

        [Header("Obstacle Jump Arc")]
        [Tooltip("Peak extra height of the coin arc, added on top of coinHeight at the obstacle's Z.")]
        [SerializeField] private float jumpArcHeight = 2.0f;
        [Tooltip("How far (in Z) on each side of an obstacle the arc reaches before returning to coinHeight.")]
        [SerializeField] private float arcHalfWidthZ = 3.6f;

        // ---- Runtime state (persistent across chunk fills) ----
        float _lastCoinZ = float.NegativeInfinity;
        bool  _initialized;
        int   _currentLane;    // 0..laneCount-1
        int   _targetLane;     // next lane during transition
        float _segStartZ;      // current segment start Z
        float _segEndZ;        // current segment end Z
        SegmentType _segment;

        enum SegmentType { Straight, Transition }

        // ------------- Public API -------------

        /// <summary>Call when a new run starts, giving the Z to start generating from (e.g., player.z).</summary>
        public void ResetPathAt(float startZ)
        {
            _initialized  = false;
            _lastCoinZ    = startZ - coinSpacingZ; // so first coin can spawn near start
            _currentLane  = MidLane();
            _targetLane   = _currentLane;
            _segStartZ    = startZ;
            _segEndZ      = startZ;
            _segment      = SegmentType.Straight;
        }

        /// <summary>
        /// Spawn coins for the given Z range into the given parent (usually the chunk transform).
        /// Assumes chunks are created in increasing Z order.
        /// <paramref name="blockedLanes"/> (index = lane) marks lanes occupied by obstacles in
        /// this chunk; coins falling on a blocked lane are skipped so the rail stays on safe lanes.
        /// Pass null to spawn everywhere.
        /// </summary>
        public void FillRange(Transform parent, float zStart, float zEnd, Dictionary<int, float[]> blockedLanes = null)
        {
            if (!_initialized) InitializeAt(zStart);

            float z = zStart;
            PlanNextSegment(zStart);
            while (z < zEnd)
            {
                // Compute X at this Z
                float x = (_segment == SegmentType.Straight)
                    ? LaneX(_currentLane)
                    : Mathf.Lerp(LaneX(_currentLane), LaneX(_targetLane), Mathf.InverseLerp(_segStartZ, _segEndZ, z));

                // One coin per Z step. If the nearest obstacle in this lane is close enough,
                // lift the coin into a jump arc that peaks over the obstacle's Z.
                float y = coinHeight + ArcLift(blockedLanes, x, z - zStart);
                iCoinFactory.Spawn(new Vector3(x, y, z), Quaternion.identity, parent);
                z += coinSpacingZ;
            }
            _currentLane = _targetLane;
        }

        /// <summary>
        /// Lay a straight line of coins between two world positions, stepping along Z by
        /// <see cref="coinSpacingZ"/> and interpolating X and Y so the line can run down a lane,
        /// diagonally across lanes, and rise/fall in height (e.g. an arc over an obstacle).
        /// Used by predefined <see cref="CoinSegment"/> markers; this does not touch the
        /// automatic rail's lane state.
        /// </summary>
        public void FillLine(Transform parent, Vector3 startWorld, Vector3 endWorld)
        {
            EnsureFactory();
            if (iCoinFactory == null) return;

            // Always step from the lower Z to the higher Z.
            if (endWorld.z < startWorld.z) (startWorld, endWorld) = (endWorld, startWorld);

            float zStart = startWorld.z;
            float zEnd   = endWorld.z;

            if (zEnd - zStart < 0.0001f)
            {
                iCoinFactory.Spawn(startWorld, Quaternion.identity, parent);
                return;
            }

            for (float z = zStart; z <= zEnd + 0.001f; z += coinSpacingZ)
            {
                float t = Mathf.InverseLerp(zStart, zEnd, z);
                float x = Mathf.Lerp(startWorld.x, endWorld.x, t);
                float y = Mathf.Lerp(startWorld.y, endWorld.y, t);
                iCoinFactory.Spawn(new Vector3(x, y, z), Quaternion.identity, parent);
            }
        }

        /// <summary>
        /// Extra height for a coin at relative Z <paramref name="relZ"/> in the lane nearest
        /// <paramref name="x"/>. Returns 0 when no obstacle is within <see cref="arcHalfWidthZ"/>;
        /// otherwise a smooth arc peaking at <see cref="jumpArcHeight"/> over the nearest obstacle's Z.
        /// </summary>
        float ArcLift(Dictionary<int, float[]> blockedLanes, float x, float relZ)
        {
            if (!IsLaneBlocked(blockedLanes, x)) return 0f;

            float[] line = blockedLanes[LaneOfX(x)];
            float nearestDz = float.PositiveInfinity;
            for (int i = 0; i < line.Length; i++)
            {
                float obZ = line[i];
                if (float.IsNaN(obZ)) continue; // empty slot, no obstacle here
                float dz = Mathf.Abs(obZ - relZ);
                if (dz < nearestDz) nearestDz = dz;
            }

            if (nearestDz > arcHalfWidthZ) return 0f;

            // 1 at the obstacle's Z, easing to 0 at the arc edges (smoothstep for a soft curve).
            float t = Mathf.SmoothStep(1f, 0f, nearestDz / arcHalfWidthZ);
            return jumpArcHeight * t;
        }

        /// <summary>True when the lane nearest <paramref name="x"/> is flagged blocked.</summary>
        bool IsLaneBlocked(Dictionary<int, float[]> blockedLanes, float x)
        {
            if (blockedLanes == null) return false;
            int lane = LaneOfX(x);
            return lane >= 0 && blockedLanes.ContainsKey(lane) && blockedLanes[lane] != null;
        }

        /// <summary>Nearest lane index for a world X (inverse of <see cref="LaneX"/>).</summary>
        int LaneOfX(float x)
        {
            int mid = MidLane();
            int idx = Mathf.RoundToInt((x - laneCenterX) / laneWidth) + mid;
            return Mathf.Clamp(idx, 0, laneCount - 1);
        }

        // ------------- Internals -------------

        void InitializeAt(float startZ)
        {
            _currentLane = MidLane();
            _targetLane  = _currentLane;
            _segment     = SegmentType.Straight;
            _segStartZ   = startZ;
            _segEndZ     = startZ + Random.Range(minStraightZ, maxStraightZ);
            _initialized = true;
            if (_lastCoinZ == float.NegativeInfinity)
                _lastCoinZ = startZ - coinSpacingZ;

            EnsureFactory();
        }

        /// <summary>Resolve the coin factory once, lazily — works even if a predefined chunk fills first.</summary>
        void EnsureFactory()
        {
            if (iCoinFactory != null) return;
            iCoinFactory = coinFactoryProvider as IFactory<CoinCollectible> ?? FindAnyObjectByType<CoinFactory>(FindObjectsInactive.Exclude);
        }

        void PlanNextSegment(float zStart)
        {
            // We no longer flee blocked lanes: coins stay on their lane and arc over
            // obstacles (see ArcLift). Lane changes are now just occasional variety.
            if (Random.value < laneChangeChance)
            {
                int to = ChooseAdjacentLane(_currentLane);
                _segment = SegmentType.Transition;
                _segStartZ = zStart;
                _segEndZ = _segStartZ + transitionLengthZ;
                _targetLane = to;
            }
            else
            {
                _segment = SegmentType.Straight;
                _targetLane = _currentLane;
            }
        }

        int ChooseAdjacentLane(int from)
        {
            // Prefer moving inward if at edges; otherwise pick left/right randomly
            if (from <= 0)                 return from + 1;
            if (from >= laneCount - 1)     return from - 1;
            return Random.value < 0.5f ? from - 1 : from + 1;
        }

        int MidLane() => Mathf.Clamp(laneCount / 2, 0, laneCount - 1);

        float LaneX(int laneIndex)
        {
            // 0..N-1 => symmetric around laneCenterX
            int mid = MidLane();
            int delta = laneIndex - mid;
            return laneCenterX + delta * laneWidth;
        }
    }
}
