using System.IO;
using UnityEditor;
using UnityEngine;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Data;

namespace MagicVillageDash.EditorTools
{
    /// <summary>
    /// Dev-only utility to wipe persistent test data: clears the AchievementData,
    /// CollectionProgressData, DenPlacementData and RunStatsData ScriptableObject assets
    /// AND deletes the shared <c>playerdata.json</c> save file the runtime loads from.
    ///
    /// Menu: MagicVillageDash ▸ Test ▸ Reset Save Data
    /// </summary>
    public static class TestDataResetter
    {
        // Matches GameDataServiceBase's default fileName.
        private const string SaveFileName = "playerdata.json";

        [MenuItem("MagicVillageDash/Test/Reset Save Data")]
        public static void ResetAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset Save Data",
                    "This clears Achievements, Collection, Den placements and Run stats " +
                    "(both the SO assets and the on-disk playerdata.json).\n\nContinue?",
                    "Reset", "Cancel"))
                return;

            ResetSoAssets();
            DeleteSaveFile();

            Debug.Log("[TestDataResetter] Save data reset complete.");
        }

        /// <summary>Reset just the SO assets without touching the JSON file.</summary>
        [MenuItem("MagicVillageDash/Test/Reset SO Assets Only")]
        public static void ResetSoAssets()
        {
            foreach (var a in LoadAll<AchievementData>())      { a.ResetAll(); Dirty(a); }
            foreach (var c in LoadAll<CollectionProgressData>()){ c.Clear();    Dirty(c); }
            foreach (var d in LoadAll<DenPlacementData>())      { d.ResetAll(); Dirty(d); }
            foreach (var r in LoadAll<RunStatsData>())          { r.ResetAll(); Dirty(r); }

            AssetDatabase.SaveAssets();
            Debug.Log("[TestDataResetter] SO assets reset.");
        }

        /// <summary>Delete only the persisted playerdata.json (next load starts fresh).</summary>
        [MenuItem("MagicVillageDash/Test/Delete playerdata.json Only")]
        public static void DeleteSaveFile()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[TestDataResetter] Deleted save file: {path}");
            }
            else
            {
                Debug.Log($"[TestDataResetter] No save file at: {path}");
            }
        }

        private static System.Collections.Generic.IEnumerable<T> LoadAll<T>() where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<T>(path);
                if (so != null) yield return so;
            }
        }

        private static void Dirty(Object o) => EditorUtility.SetDirty(o);
    }
}
