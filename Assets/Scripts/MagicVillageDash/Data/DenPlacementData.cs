using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicVillageDash.Data
{
    /// <summary>
    /// The only den-specific state the collection can't express: which owned structures are currently
    /// BUILT in the den. Ownership itself lives in the collection (CollectionProgressData) — discovering
    /// an entry (relic pickup or achievement unlock) makes it owned; this just remembers where the
    /// player chose to place it. Tray = owned − placed.
    ///
    /// Mirrors AchievementData / CollectionProgressData and rides GameDataService's ScriptableObject
    /// list (playerdata.json). Pure data: the placement UI calls <c>GameDataService.SaveAll()</c> after
    /// mutating it.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicVillageDash/Data/Den Placement Data", fileName = "DenPlacementData")]
    public sealed class DenPlacementData : ScriptableObject
    {
        [Header("Built in the den")]
        [SerializeField] private List<string> placedIds = new();

        // Fast lookup, rebuilt lazily from the serialized list after a load.
        private HashSet<string> _set;

        public IReadOnlyList<string> PlacedIds => placedIds;
        public int Count => placedIds.Count;

        public bool IsPlaced(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            EnsureSet();
            return _set.Contains(entryId);
        }

        /// <summary>Marks an entry as built. Returns true only when it was not already placed.</summary>
        public bool Place(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            EnsureSet();
            if (!_set.Add(entryId)) return false;

            placedIds.Add(entryId);
            return true;
        }

        /// <summary>Returns a structure to the tray. Returns true if it was placed.</summary>
        public bool Unplace(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            EnsureSet();
            if (!_set.Remove(entryId)) return false;

            placedIds.Remove(entryId);
            return true;
        }

        public void ResetAll()
        {
            placedIds.Clear();
            _set = null;
        }

        /// <summary>Drops the cached lookup so it rebuilds from the (freshly loaded) list.</summary>
        public void Invalidate() => _set = null;

        private void EnsureSet()
        {
            if (_set != null && _set.Count == placedIds.Count) return;
            _set = new HashSet<string>(placedIds, StringComparer.Ordinal);
        }
    }
}
