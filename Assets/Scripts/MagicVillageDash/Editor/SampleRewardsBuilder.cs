using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ErccDev.Foundation.Core.Achievements;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Achievements;
using MagicVillageDash.Achievements.Conditions;
using MagicVillageDash.Collections;

namespace MagicVillageDash.Editor
{
    /// <summary>
    /// Authors a batch of new achievement → reward pairs and wires them end-to-end, since the editor
    /// can't be driven by hand from here. Each pair gets: a condition SO, an
    /// <see cref="AchievementDefinition"/>, a <see cref="ModelCollectionEntry"/> whose 3D payoff is a
    /// stylized prop from <c>Assets/IgnoreFolder</c>, and an <see cref="UnlockCollectionEntryReward"/>
    /// linking them. The new content is registered into both catalogs and appended to the RunnerScene's
    /// <see cref="AchievementManager"/> list so it actually evaluates/unlocks at play time.
    ///
    /// Run via <b>MagicVillageDash ▸ Create Sample Rewards &amp; Achievements</b>. Idempotent: skips ids
    /// that already exist.
    /// </summary>
    public static class SampleRewardsBuilder
    {
        private const string AchDir       = "Assets/Settings/Achievements";
        private const string EntryDir     = "Assets/Settings/Collection";
        private const string RewardDir    = "Assets/Settings/Rewards";
        private const string AchCatalog   = AchDir + "/AchievementCatalog.asset";
        private const string CollCatalog  = EntryDir + "/CollectionCatalog.asset";
        private const string RunnerScene  = "Assets/Scenes/RunnerScene.unity";
        private const string PropRoot     = "Assets/IgnoreFolder/EmaceArt/NatureForge LITE Stylized Meadow & Farm/Meshes/";
        private const string PawIcon      = "Assets/Images/HUD/PawPrint.png";

        private enum Metric { Coins, Distance, Score }

        private sealed class Spec
        {
            public string id;            // shared id stem (achievement + entry)
            public string achTitle, achDesc;
            public string rewardTitle, rewardDesc;
            public Metric metric;
            public float  target;
            public string fbx;           // file name under PropRoot
            public string achIcon;       // optional sprite path for the achievement card
        }

        // Three new cozy "Wolfland" structures earned by pushing further each run.
        private static readonly Spec[] Specs =
        {
            new() { id = "coin_hoarder",  achTitle = "Coin Hoarder", achDesc = "Collect 1000 coins in a single run.",
                    rewardTitle = "Storage Barrel", rewardDesc = "A sturdy barrel for the den's spoils.",
                    metric = Metric.Coins, target = 1000, fbx = "EA_M_Prop_Barrel_Small_01.fbx",
                    achIcon = "Assets/Images/Achievements/Achiev_Collect500Coins.png" },

            new() { id = "marathon_wolf", achTitle = "Marathon Wolf", achDesc = "Run 1500 meters without falling.",
                    rewardTitle = "Trailside Bench", rewardDesc = "A wooden bench to rest weary paws.",
                    metric = Metric.Distance, target = 1500, fbx = "EA_M_Furniture_Bench_Wooden_01.fbx",
                    achIcon = "Assets/Images/Achievements/Achiev_Dist500Units.png" },

            new() { id = "pack_leader",   achTitle = "Pack Leader", achDesc = "Reach a score of 10000.",
                    rewardTitle = "Hay Seat", rewardDesc = "A comfy hay bale to gather the pack around.",
                    metric = Metric.Score, target = 10000, fbx = "EA_Furniture_HayBaleSeat_01a.fbx",
                    achIcon = PawIcon },
        };

        [MenuItem("MagicVillageDash/Create Sample Rewards & Achievements")]
        public static void Create()
        {
            if (!EditorUtility.DisplayDialog("Create Sample Rewards & Achievements",
                    $"Creates {Specs.Length} new achievement+reward pairs (3D props from IgnoreFolder), " +
                    "adds them to both catalogs, and appends the achievements to the RunnerScene " +
                    "AchievementManager.\n\nContinue?", "Create", "Cancel"))
                return;

            Directory.CreateDirectory(AchDir);
            Directory.CreateDirectory(EntryDir);
            Directory.CreateDirectory(RewardDir);

            var newAchievements = new List<AchievementDefinition>();
            var paw = AssetDatabase.LoadAssetAtPath<Sprite>(PawIcon);

            foreach (var s in Specs)
            {
                var achPath = $"{AchDir}/Achiev_{s.id}.asset";
                if (AssetDatabase.LoadAssetAtPath<AchievementDefinition>(achPath) != null)
                {
                    Debug.Log($"[SampleRewards] '{s.id}' already exists — skipping.");
                    continue;
                }

                // 1) Collection entry whose payoff is an IgnoreFolder prop.
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(PropRoot + s.fbx);
                if (model == null)
                    Debug.LogWarning($"[SampleRewards] Prop not found: {PropRoot + s.fbx} " +
                                     "(IgnoreFolder asset pack not installed?). Entry created without a model.");

                var entry = ScriptableObject.CreateInstance<ModelCollectionEntry>();
                entry.entryId     = s.id;
                entry.title       = s.rewardTitle;
                entry.description  = s.rewardDesc;
                entry.icon        = paw;
                entry.modelPrefab = model;
                AssetDatabase.CreateAsset(entry, $"{EntryDir}/Entry_{s.id}.asset");

                // 2) Reward that unlocks that entry on first achievement unlock.
                var reward = ScriptableObject.CreateInstance<UnlockCollectionEntryReward>();
                reward.rewardId = s.id;
                var rso = new SerializedObject(reward);
                rso.FindProperty("entryId").stringValue = s.id;
                rso.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(reward, $"{RewardDir}/Reward_Unlock_{s.id}.asset");

                // 3) Condition.
                var condition = MakeCondition(s, $"{AchDir}/Cond_{s.id}.asset");

                // 4) Achievement tying condition + reward together.
                var ach = ScriptableObject.CreateInstance<AchievementDefinition>();
                ach.achievementId = s.id;
                ach.title         = s.achTitle;
                ach.description   = s.achDesc;
                ach.icon          = AssetDatabase.LoadAssetAtPath<Sprite>(s.achIcon) ?? paw;
                ach.condition     = condition;
                ach.rewards       = new Reward[] { reward };
                AssetDatabase.CreateAsset(ach, achPath);

                newAchievements.Add(ach);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshCatalogs();
            RegisterWithManager(newAchievements);

            Debug.Log($"[SampleRewards] Done — added {newAchievements.Count} achievement+reward pairs. " +
                      "Rebuild the gallery (MagicVillageDash ▸ Build Rewards Gallery Scene) to see them.");
        }

        private static AchievementCondition MakeCondition(Spec s, string path)
        {
            AchievementCondition cond = s.metric switch
            {
                Metric.Coins    => ScriptableObject.CreateInstance<CoinsInRunCondition>(),
                Metric.Distance => ScriptableObject.CreateInstance<DistanceReachedCondition>(),
                _               => ScriptableObject.CreateInstance<ScoreReachedCondition>(),
            };

            var so = new SerializedObject(cond);
            var prop = s.metric switch
            {
                Metric.Coins    => so.FindProperty("targetCoins"),
                Metric.Distance => so.FindProperty("targetMeters"),
                _               => so.FindProperty("targetScore"),
            };
            if (prop.propertyType == SerializedPropertyType.Integer) prop.intValue = (int)s.target;
            else prop.floatValue = s.target;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(cond, path);
            return cond;
        }

        private static void RefreshCatalogs()
        {
            var ach = AssetDatabase.LoadAssetAtPath<AchievementCatalog>(AchCatalog);
            if (ach != null) { ach.RefreshFromProject(); EditorUtility.SetDirty(ach); }

            var coll = AssetDatabase.LoadAssetAtPath<CollectionCatalog>(CollCatalog);
            if (coll != null) { coll.RefreshFromProject(); EditorUtility.SetDirty(coll); }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Appends the new defs to the RunnerScene AchievementManager so they get evaluated.</summary>
        private static void RegisterWithManager(List<AchievementDefinition> defs)
        {
            if (defs.Count == 0) return;

            // Switching scenes below would discard unsaved edits — let the user save first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[SampleRewards] Cancelled before opening RunnerScene — assets were " +
                                 "created and catalogued, but not registered with the manager.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(RunnerScene, OpenSceneMode.Single);
            AchievementManager manager = null;
            foreach (var root in scene.GetRootGameObjects())
                if ((manager = root.GetComponentInChildren<AchievementManager>(true)) != null) break;

            if (manager == null)
            {
                Debug.LogWarning("[SampleRewards] No AchievementManager in RunnerScene — new achievements " +
                                 "are in the catalog (so the gallery shows them) but won't auto-unlock. " +
                                 "Add them to the manager's list manually.");
                return;
            }

            var so = new SerializedObject(manager);
            var list = so.FindProperty("achievements");
            var existing = new HashSet<Object>();
            for (int i = 0; i < list.arraySize; i++)
                existing.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);

            foreach (var def in defs)
                if (!existing.Contains(def))
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = def;
                }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SampleRewards] Registered {defs.Count} achievements with the RunnerScene manager.");
        }
    }
}
