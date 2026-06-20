using MoreMountains.Feedbacks;
using UnityEngine;

namespace MagicVillageDash.Den.Placement
{
    /// <summary>
    /// A spot in the den where one structure can be built — drop this on each empty marker GameObject
    /// under <c>PlacementSlots</c> (Slot1…Slot5). It owns nothing but its own ground: a stable id for
    /// persistence, an <see cref="arrowIndicator"/> shown while the player is choosing where to drop
    /// the selected item, and the single structure currently built here. The
    /// <see cref="DenPlacementController"/> raycasts the arrow's collider to detect a tap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DenSlot : MonoBehaviour
    {
        [Tooltip("Stable id saved with the placement. Leave empty to use the GameObject name.")]
        [SerializeField] private string slotId;

        [Tooltip("Arrow/marker child shown above the spot while picking a slot. Needs a collider to be tappable.")]
        [SerializeField] private GameObject arrowIndicator;

        [Tooltip("Where the structure is parented/spawned. Defaults to this transform.")]
        [SerializeField] private Transform anchor;
        [SerializeField] private MMF_Player TargetMMF_Player;

        /// <summary>Persisted id — never changes once items have been saved against it.</summary>
        public string SlotId => string.IsNullOrEmpty(slotId) ? name : slotId;

        public bool IsOccupied => Placed != null;
        public GameObject Placed { get; private set; }

        // Invisible box wrapped around the built structure so a tap can pick it back up, even when the
        // model prefab ships no collider of its own. Lives on the arrow's layer so the controller's slot
        // raycast catches it; torn down with the structure.
        private GameObject _tapBody;

        void Awake() => ShowArrow(false);

        public void ShowArrow(bool show)
        {
            if (arrowIndicator != null)
                arrowIndicator.SetActive(show);
        }

        /// <summary>Builds a structure here, replacing anything already on the spot. Hides the arrow.</summary>
        public GameObject Build(GameObject prefab)
        {
            Clear();
            ShowArrow(false);
            if (prefab == null) return null;

            var at = anchor != null ? anchor : transform;
            Placed = Instantiate(prefab, at.position, at.rotation, at);
            BuildTapBody();
            TargetMMF_Player?.PlayFeedbacks();
            return Placed;
        }

        /// <summary>Tears down whatever is built here (does not touch the saved data).</summary>
        public void Clear()
        {
            if (_tapBody != null) Destroy(_tapBody);
            _tapBody = null;
            if (Placed != null) Destroy(Placed);
            Placed = null;
        }

        /// <summary>
        /// Fits an axis-aligned trigger box around the built structure's renderers so it can be tapped to
        /// pick up, regardless of the model prefab's own colliders. Assumes the slot marker is unscaled.
        /// </summary>
        private void BuildTapBody()
        {
            if (Placed == null) return;

            var rends = Placed.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;

            var bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);

            _tapBody = new GameObject("TapBody");
            var t = _tapBody.transform;
            t.SetParent(transform, worldPositionStays: false);
            t.position = bounds.center;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
            _tapBody.layer = arrowIndicator != null ? arrowIndicator.layer : gameObject.layer;

            var box = _tapBody.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = bounds.size;
        }
    }
}
