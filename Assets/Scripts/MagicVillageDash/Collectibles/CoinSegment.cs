using UnityEngine;

namespace MagicVillageDash.Collectibles
{
    /// <summary>
    /// Marks a predefined coin line on a chunk, instead of letting the rail span the whole chunk
    /// automatically. Drop one (or more) of these on a chunk prefab and assign two marker
    /// transforms — coins are laid between them, following both the markers' Z and X (so the line
    /// can run straight down a lane or diagonally across lanes). Spacing/height come from
    /// <see cref="CoinRailGenerator"/>; the markers decide where each coin sits.
    /// If a chunk has no <see cref="CoinSegment"/> children, the rail fills the chunk as before.
    /// </summary>
    public sealed class CoinSegment : MonoBehaviour
    {
        [Header("Markers (world Z bounds the coins)")]
        [Tooltip("GameObject marking the start of the coin segment. Falls back to this transform.")]
        [SerializeField] private Transform startMarker;
        [Tooltip("GameObject marking the end of the coin segment. Falls back to this transform.")]
        [SerializeField] private Transform endMarker;

        Transform StartT => startMarker ? startMarker : transform;
        Transform EndT   => endMarker   ? endMarker   : transform;

        /// <summary>World position of the start marker (defines both X and Z of the coin line).</summary>
        public Vector3 StartWorld => StartT.position;

        /// <summary>World position of the end marker (defines both X and Z of the coin line).</summary>
        public Vector3 EndWorld => EndT.position;

        /// <summary>Lower world-space Z bound of the segment.</summary>
        public float MinZ => Mathf.Min(StartT.position.z, EndT.position.z);

        /// <summary>Upper world-space Z bound of the segment.</summary>
        public float MaxZ => Mathf.Max(StartT.position.z, EndT.position.z);

        /// <summary>True when the two markers describe a usable (non-zero) range.</summary>
        public bool IsValid => MaxZ - MinZ > 0.001f;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Vector3 a = StartT.position;
            Vector3 b = EndT.position;
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.5f);
            Gizmos.DrawWireSphere(b, 0.5f);
        }
#endif
    }
}
