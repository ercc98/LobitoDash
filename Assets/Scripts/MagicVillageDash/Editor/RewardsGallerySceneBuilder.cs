using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MagicVillageDash.Achievements;
using MagicVillageDash.UI.Gallery;

namespace MagicVillageDash.Editor
{
    /// <summary>
    /// One-click builder for the Rewards Gallery scene. Because the project can't be driven through the
    /// live editor from here, this constructs the whole screen programmatically — Canvas, scrollable grid,
    /// reusable card prefab, the <see cref="RewardsGalleryController"/> wired to the achievement +
    /// collection catalogs, and a Back button — then saves it as <c>Assets/Scenes/RewardsGalleryScene.unity</c>
    /// and offers to add it to Build Settings. Re-runnable: rebuilds the scene from scratch each time.
    ///
    /// Run via <b>MagicVillageDash ▸ Build Rewards Gallery Scene</b>. Pure authoring tool, editor-only.
    /// </summary>
    public static class RewardsGallerySceneBuilder
    {
        private const string ScenePath        = "Assets/Scenes/RewardsGalleryScene.unity";
        private const string PrefabDir        = "Assets/Prefabs/UI";
        private const string CardPrefabPath   = PrefabDir + "/GalleryItemCard.prefab";
        private const string AchCatalogPath   = "Assets/Settings/Achievements/AchievementCatalog.asset";
        private const string CollCatalogPath  = "Assets/Settings/Collection/CollectionCatalog.asset";

        [MenuItem("MagicVillageDash/Build Rewards Gallery Scene")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Build Rewards Gallery Scene",
                    "This creates/overwrites:\n" +
                    $"• {ScenePath}\n• {CardPrefabPath}\n• {AchCatalogPath}\n\nContinue?",
                    "Build", "Cancel"))
                return;

            var achievementCatalog = EnsureAchievementCatalog();
            var collectionCatalog  = AssetDatabase.LoadAssetAtPath<ErccDev.Foundation.Core.Collection.CollectionCatalog>(CollCatalogPath);
            var cardPrefab         = BuildCardPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateEventSystem();
            var canvas = CreateCanvas();
            BuildBackground(canvas.transform);
            BuildHeader(canvas.transform, out var summaryText);
            var content = BuildScrollGrid(canvas.transform);
            BuildBackButton(canvas.transform);

            // Controller, wired through SerializedObject so we can set the private [SerializeField] fields.
            var controllerGo = new GameObject("RewardsGalleryController");
            GameObjectUtility.SetParentAndAlign(controllerGo, canvas.gameObject);
            var controller = controllerGo.AddComponent<RewardsGalleryController>();
            var so = new SerializedObject(controller);
            so.FindProperty("achievementCatalog").objectReferenceValue = achievementCatalog;
            so.FindProperty("collectionCatalog").objectReferenceValue  = collectionCatalog;
            so.FindProperty("itemPrefab").objectReferenceValue         = cardPrefab.GetComponent<GalleryItemView>();
            so.FindProperty("itemContainer").objectReferenceValue      = content;
            so.FindProperty("summaryText").objectReferenceValue        = summaryText;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AddToBuildSettings();

            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log($"[RewardsGallery] Built scene at {ScenePath}. " +
                      "Wire its 'Open' button (GallerySceneNav) into the intro/den menus to reach it.");
        }

        // ---------- Catalogs ----------

        private static AchievementCatalog EnsureAchievementCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalog>(AchCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AchievementCatalog>();
                Directory.CreateDirectory(Path.GetDirectoryName(AchCatalogPath));
                AssetDatabase.CreateAsset(catalog, AchCatalogPath);
            }
            catalog.RefreshFromProject();   // pull in every AchievementDefinition in the project
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ---------- Card prefab ----------

        private static GameObject BuildCardPrefab()
        {
            var root = new GameObject("GalleryItemCard", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(220, 280);
            root.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

            var icon = MakeImage("Icon", rt, new Vector2(140, 140), new Vector2(0, 50));
            var title = MakeText("Title", rt, new Vector2(200, 36), new Vector2(0, -50), 22, FontStyles.Bold);
            var desc  = MakeText("Description", rt, new Vector2(200, 70), new Vector2(0, -110), 14, FontStyles.Normal);

            // Locked overlay: dim panel + lock label, toggled by the view.
            var overlay = new GameObject("LockedOverlay", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(overlay, root);
            Stretch((RectTransform)overlay.transform);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var lockLabel = MakeText("LockLabel", (RectTransform)overlay.transform, new Vector2(120, 40), Vector2.zero, 28, FontStyles.Bold);
            lockLabel.text = "🔒";

            // Progress bar: background + filled image, plus a percent label.
            var barRoot = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(barRoot, root);
            var barRt = (RectTransform)barRoot.transform;
            barRt.sizeDelta = new Vector2(200, 16);
            barRt.anchoredPosition = new Vector2(0, -150);
            barRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var fill = MakeImage("Fill", barRt, new Vector2(200, 16), Vector2.zero);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            Stretch((RectTransform)fill.transform);
            var progText = MakeText("ProgressText", rt, new Vector2(80, 20), new Vector2(0, -150), 12, FontStyles.Normal);

            var view = root.AddComponent<GalleryItemView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue            = icon.GetComponent<Image>();
            so.FindProperty("titleText").objectReferenceValue       = title;
            so.FindProperty("descriptionText").objectReferenceValue = desc;
            so.FindProperty("lockedOverlay").objectReferenceValue   = overlay;
            so.FindProperty("progressFill").objectReferenceValue    = fillImg;
            so.FindProperty("progressText").objectReferenceValue    = progText;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(PrefabDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------- Scene UI ----------

        private static void CreateEventSystem()
        {
            // New Input System: drive UI through InputSystemUIInputModule, not the legacy
            // StandaloneInputModule (which throws once Active Input Handling is "Input System Package").
            var go = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);   // portrait mobile
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void BuildBackground(Transform parent)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.12f, 1f);
        }

        private static void BuildHeader(Transform parent, out TMP_Text summaryText)
        {
            var title = MakeText("TitleHeader", parent as RectTransform, new Vector2(900, 90), Vector2.zero, 60, FontStyles.Bold);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -60);
            title.text = "Rewards & Achievements";

            summaryText = MakeText("Summary", parent as RectTransform, new Vector2(600, 50), Vector2.zero, 34, FontStyles.Normal);
            var srt = (RectTransform)summaryText.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0, -160);
            summaryText.text = "Earned 0 / 0";
        }

        private static Transform BuildScrollGrid(Transform parent)
        {
            var scrollGo = new GameObject("Gallery ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            GameObjectUtility.SetParentAndAlign(scrollGo, parent.gameObject);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.78f);
            scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            GameObjectUtility.SetParentAndAlign(viewportGo, scrollGo);
            var viewportRt = (RectTransform)viewportGo.transform;
            Stretch(viewportRt);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            GameObjectUtility.SetParentAndAlign(contentGo, viewportGo);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220, 280);
            grid.spacing = new Vector2(24, 24);
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            return contentGo.transform;
        }

        private static void BuildBackButton(Transform parent)
        {
            var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(280, 90);
            rt.anchoredPosition = new Vector2(0, 50);
            go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.85f, 1f);

            var label = MakeText("Label", rt, new Vector2(280, 90), Vector2.zero, 34, FontStyles.Bold);
            Stretch((RectTransform)label.transform);
            label.alignment = TextAlignmentOptions.Center;
            label.text = "Back";

            var nav = go.AddComponent<GallerySceneNav>();
            go.GetComponent<Button>().onClick.AddListener(nav.Back);
        }

        // ---------- Primitives ----------

        private static GameObject MakeImage(string name, RectTransform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, Vector2 size, Vector2 pos, float fontSize, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.enableWordWrapping = true;
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ---------- Build settings ----------

        private static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
