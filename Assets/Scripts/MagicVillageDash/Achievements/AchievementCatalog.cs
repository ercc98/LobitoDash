using System.Collections.Generic;
using UnityEngine;
using ErccDev.Foundation.Core.Achievements;

namespace MagicVillageDash.Achievements
{
    /// <summary>
    /// One authored list of every achievement the game ships with — the single source of truth a
    /// gallery/HUD can iterate to show locked and unlocked entries alike. Mirrors Foundation's
    /// <see cref="ErccDev.Foundation.Core.Collection.CollectionCatalog"/>: the
    /// <see cref="AchievementManager"/> still owns evaluation/persistence, this just lets read-only UI
    /// enumerate the definitions without poking at the manager's private list.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicVillageDash/Achievements/Catalog", fileName = "AchievementCatalog")]
    public sealed class AchievementCatalog : ScriptableObject
    {
        [SerializeField] private List<AchievementDefinition> achievements = new();

        public IReadOnlyList<AchievementDefinition> Achievements => achievements;
        public int Count => achievements.Count;

#if UNITY_EDITOR
        /// <summary>Rebuilds from every achievement asset in the project, sorted by id. Editor-only.</summary>
        [ContextMenu("Refresh From Project")]
        public void RefreshFromProject()
        {
            achievements.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AchievementDefinition"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var def  = UnityEditor.AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
                if (def != null) achievements.Add(def);
            }
            achievements.Sort((a, b) => string.CompareOrdinal(
                a != null ? a.achievementId : "", b != null ? b.achievementId : ""));
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
