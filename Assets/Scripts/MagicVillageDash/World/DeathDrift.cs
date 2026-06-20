using System.Collections;
using MagicVillageDash.Runner;
using UnityEngine;

namespace MagicVillageDash.World
{
    /// <summary>
    /// Drifts this object back with the world for a moment: parents it under the
    /// <see cref="WorldMover"/> so it slides along -Z with the scenery, then (optionally)
    /// restores its original parent. Reusable by anything that "dies" and should coast away
    /// (enemies, props, rewards) — the owner decides when to start it and what happens next.
    /// </summary>
    public sealed class DeathDrift : MonoBehaviour
    {
        [Tooltip("How long to ride the world before detaching. Owner usually recycles right after.")]
        [SerializeField] private float driftSeconds = 2f;

        [Tooltip("If set, used as the world to attach to. Otherwise found lazily in the scene.")]
        [SerializeField] private WorldMover worldMover;
        [SerializeField] private LaneRunner laneRunner; // for safety: if the runner's disabled, the world isn't moving, so no need to drift


        [Tooltip("Restore the original parent when the drift ends. Leave off if the owner reparents (e.g. a factory recycle) right after.")]
        [SerializeField] private bool restoreParentOnEnd = false;

        /// <summary>Drift using the serialized <see cref="driftSeconds"/>.</summary>
        public IEnumerator Drift() => Drift(driftSeconds);

        /// <summary>Attach to the world, wait <paramref name="seconds"/>, then optionally detach.</summary>
        public IEnumerator Drift(float seconds)
        {
            if (worldMover == null) worldMover = FindAnyObjectByType<WorldMover>(FindObjectsInactive.Exclude);
            if (worldMover == null) yield break;
            laneRunner.enabled = false; // safety: if the runner's disabled, the world isn't moving, so no need to drift
            Transform originalParent = transform.parent;
            transform.SetParent(worldMover.transform, true); // keep world pose

            yield return new WaitForSeconds(seconds);
            laneRunner.enabled = true;
            if (restoreParentOnEnd) transform.SetParent(originalParent, true);
        }
    }
}
