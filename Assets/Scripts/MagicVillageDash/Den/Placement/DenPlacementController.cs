using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Collections;
using MagicVillageDash.Data;
using MoreMountains.Feedbacks;
using MagicVillageDash.Audio;

namespace MagicVillageDash.Den.Placement
{
    /// <summary>
    /// The brain of the den's "earn it → place it" loop. Builds the tray (owned − placed) from the
    /// collection, feeds it to the carousel, and runs the place flow: tap a tray item → arrows light
    /// up on every free slot → tap an arrow → the structure is built there and saved. Tapping a built
    /// structure with nothing armed sends it back to the tray. Already-placed structures are rebuilt
    /// into their saved slots on load.
    ///
    /// Reward-agnostic and read-only against the collection: ownership/discovery is decided elsewhere
    /// (relic pickups, achievements); this only decides WHERE owned structures sit, in <see cref="DenPlacementData"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DenPlacementController : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("What the player owns. The same SO relics AND achievement rewards write to, so the den " +
                 "sees both. Already in GameDataService's save list — no scene manager needed to read it.")]
        [SerializeField] private CollectionProgressData collectionProgress;
        [SerializeField] private DenPlacementData placement;
        [Tooltip("Shared list of every collection entry. The den uses the ModelCollectionEntry ones " +
                 "(icon for the tray, modelPrefab for the built structure).")]
        [SerializeField] private CollectionCatalog catalog;

        [Header("Scene")]
        [SerializeField] private List<DenSlot> slots = new();
        [SerializeField] private DenTrayCarouselUI tray;
        [Tooltip("Camera used to raycast taps onto slot arrows. Defaults to Camera.main.")]
        [SerializeField] private UnityEngine.Camera raycastCamera;

        [Header("Tap raycast")]
        [Tooltip("Layers the slot arrows live on. Keep arrows off the UI/EventSystem path.")]
        [SerializeField] private LayerMask slotMask = ~0;
        [SerializeField, Min(1f)] private float rayMaxDistance = 500f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem placeParticles;
        [SerializeField] private MMF_Player TargetMMF_Player;
        [SerializeField] private AudioManager audioManager;
        private ModelCollectionEntry _selected;
        private readonly List<ModelCollectionEntry> _trayItems = new();

        void Awake()
        {
            if (raycastCamera == null) raycastCamera = UnityEngine.Camera.main;
        }

        void Start()
        {
            // Pull the latest save into the SOs, then rebuild both lookups before reading them.
            GameDataService._instance?.LoadAll();
            collectionProgress?.Invalidate();
            placement?.Invalidate();

            RebuildPlaced();
            RefreshTray();
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            // Don't steal taps meant for the tray / paging buttons.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var screenPos = pointer.position.ReadValue();

            // Item armed → drop it on a free slot. Nothing armed → pick a built structure back up.
            if (_selected != null) TryPlaceAtPointer(screenPos);
            else                   TryPickUpAtPointer(screenPos);
        }

        // ---------- Tray ----------

        /// <summary>Arms a structure for placement and lights up every free slot's arrow.</summary>
        public void Select(ModelCollectionEntry entry)
        {
            if (entry == null) return;
            _selected = entry;
            ShowArrows(true);
        }

        /// <summary>Cancels the current selection and hides the arrows.</summary>
        public void CancelSelection()
        {
            _selected = null;
            ShowArrows(false);
        }

        private void RefreshTray()
        {
            _trayItems.Clear();
            _trayItems.AddRange(DenInventory.EnumerateUnplaced(catalog, collectionProgress, placement));
            tray?.Show(_trayItems, Select);
        }

        // ---------- Placement ----------

        private void TryPlaceAtPointer(Vector2 screenPos)
        {
            if (raycastCamera == null) return;

            var ray = raycastCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, rayMaxDistance, slotMask, QueryTriggerInteraction.Collide))
                return;

            var slot = hit.collider.GetComponentInParent<DenSlot>();
            if (slot == null || slot.IsOccupied) return;

            PlaceSelectedAt(slot);
        }

        private void PlaceSelectedAt(DenSlot slot)
        {
            slot.Build(_selected.modelPrefab);
            placement?.PlaceAt(slot.SlotId, _selected.entryId);
            placeParticles?.transform.SetPositionAndRotation(slot.transform.position, placeParticles.transform.rotation);
            placeParticles?.Play();
            TargetMMF_Player?.PlayFeedbacks();
            audioManager?.Play(SfxId.Landing);
            GameDataService._instance?.SaveAll();

            _selected = null;
            ShowArrows(false);
            RefreshTray();
        }

        /// <summary>Tap a built structure (with nothing armed) to tear it down and return it to the tray.</summary>
        private void TryPickUpAtPointer(Vector2 screenPos)
        {
            if (raycastCamera == null) return;

            var ray = raycastCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, rayMaxDistance, slotMask, QueryTriggerInteraction.Collide))
                return;

            var slot = hit.collider.GetComponentInParent<DenSlot>();
            if (slot == null || !slot.IsOccupied) return;

            slot.Clear();
            placement?.ClearSlot(slot.SlotId);
            GameDataService._instance?.SaveAll();
            RefreshTray();
        }

        private void ShowArrows(bool show)
        {
            foreach (var slot in slots)
                if (slot != null) slot.ShowArrow(show && !slot.IsOccupied);
        }

        private void RebuildPlaced()
        {
            if (placement == null) return;
            foreach (var p in placement.Placements)
            {
                var slot = FindSlot(p.slotId);
                var entry = FindEntry(p.entryId);
                if (slot != null && entry != null)
                    slot.Build(entry.modelPrefab);
            }
        }

        private DenSlot FindSlot(string slotId)
        {
            foreach (var slot in slots)
                if (slot != null && slot.SlotId == slotId) return slot;
            return null;
        }

        private ModelCollectionEntry FindEntry(string entryId)
            => catalog != null ? catalog.Get<ModelCollectionEntry>(entryId) : null;
    }
}
