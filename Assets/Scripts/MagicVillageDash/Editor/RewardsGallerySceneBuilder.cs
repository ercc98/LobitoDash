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
    /// The look matches Magic Village Dash's cozy "Wolfland" style: parchment cards on a warm forest
    /// backdrop, the game's playful fonts (CherryBombOne for headings, Fredoka for body), a gold ribbon
    /// on earned cards and a paw-print lock on the rest.
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

        private const string TitleFontPath    = "Assets/Fonts/CherryBombOne-Regular SDF.asset";
        private const string BodyFontPath     = "Assets/Fonts/Fredoka-VariableFont_wdth,wght SDF.asset";
        private const string PawIconPath      = "Assets/Images/HUD/PawPrint.png";

        // ----- Wolfland palette -----
        private static readonly Color Forest      = new(0.13f, 0.20f, 0.16f, 1f);   // backdrop
        private static readonly Color ForestDeep  = new(0.09f, 0.14f, 0.11f, 1f);   // scroll well
        private static readonly Color Wood        = new(0.36f, 0.25f, 0.16f, 1f);   // header band
        private static readonly Color Parchment   = new(0.96f, 0.92f, 0.82f, 1f);   // card face
        private static readonly Color Ink         = new(0.27f, 0.19f, 0.12f, 1f);   // text on parchment
        private static readonly Color Cream       = new(0.98f, 0.95f, 0.88f, 1f);   // text on wood
        private static readonly Color Gold        = new(0.95f, 0.76f, 0.28f, 1f);   // earned accent
        private static readonly Color Leaf        = new(0.45f, 0.62f, 0.32f, 1f);   // progress fill
        private static readonly Color LockTint    = new(0.55f, 0.53f, 0.48f, 1f);   // locked icon
        private static readonly Color Shade       = new(0.10f, 0.10f, 0.10f, 0.55f);// locked overlay

        private static TMP_FontAsset _titleFont;
        private static TMP_FontAsset _bodyFont;
        private static Sprite _round;   // built-in rounded UI sprite for sliced panels/buttons

        [MenuItem("MagicVillageDash/Build Rewards Gallery Scene")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Build Rewards Gallery Scene",
                    "This creates/overwrites:\n" +
                    $"• {ScenePath}\n• {CardPrefabPath}\n• {AchCatalogPath}\n\nContinue?",
                    "Build", "Cancel"))
                return;

            _titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath) ?? TMP_Settings.defaultFontAsset;
            _bodyFont  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath)  ?? TMP_Settings.defaultFontAsset;
            _round     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

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
            Debug.Log($"[RewardsGallery] Built styled scene at {ScenePath}. " +
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
            rt.sizeDelta = new Vector2(240, 300);
            Panel(root.GetComponent<Image>(), Parchment);

            // Gold "earned" ribbon frame behind the face, revealed only when owned.
            var frame = new GameObject("EarnedFrame", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(frame, root);
            var frameRt = (RectTransform)frame.transform;
            frameRt.anchorMin = Vector2.zero; frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(-8, -8); frameRt.offsetMax = new Vector2(8, 8);
            Panel(frame.GetComponent<Image>(), Gold);
            frame.transform.SetAsFirstSibling();

            var icon  = MakeImage("Icon", rt, new Vector2(150, 150), new Vector2(0, 58));
            var title = MakeText("Title", rt, new Vector2(216, 40), new Vector2(0, -42), 26, _titleFont, Ink);
            var desc  = MakeText("Description", rt, new Vector2(216, 84), new Vector2(0, -108), 15, _bodyFont, Ink);

            // Locked overlay: dim panel + paw-print lock, toggled by the view.
            var overlay = new GameObject("LockedOverlay", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(overlay, root);
            Stretch((RectTransform)overlay.transform);
            Panel(overlay.GetComponent<Image>(), Shade);
            var paw = MakeImage("PawLock", (RectTransform)overlay.transform, new Vector2(90, 90), new Vector2(0, 10));
            var pawSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PawIconPath);
            if (pawSprite != null) { paw.GetComponent<Image>().sprite = pawSprite; paw.GetComponent<Image>().color = Cream; }
            else Panel(paw.GetComponent<Image>(), Cream);

            // Progress bar: track + leaf-green fill, plus a percent label (achievements only).
            var barRoot = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(barRoot, root);
            var barRt = (RectTransform)barRoot.transform;
            barRt.sizeDelta = new Vector2(210, 18);
            barRt.anchoredPosition = new Vector2(0, -160);
            Panel(barRoot.GetComponent<Image>(), ForestDeep);
            var fill = MakeImage("Fill", barRt, new Vector2(210, 18), Vector2.zero);
            var fillImg = fill.GetComponent<Image>();
            Panel(fillImg, Leaf);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            Stretch((RectTransform)fill.transform);
            var progText = MakeText("ProgressText", rt, new Vector2(90, 22), new Vector2(0, -160), 13, _bodyFont, Cream);

            var view = root.AddComponent<GalleryItemView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue            = icon.GetComponent<Image>();
            so.FindProperty("titleText").objectReferenceValue       = title;
            so.FindProperty("descriptionText").objectReferenceValue = desc;
            so.FindProperty("earnedFrame").objectReferenceValue     = frame;
            so.FindProperty("lockedOverlay").objectReferenceValue   = overlay;
            so.FindProperty("lockedTint").colorValue                = LockTint;
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
            go.GetComponent<Image>().color = Forest;
        }

        private static void BuildHeader(Transform parent, out TMP_Text summaryText)
        {
            // Wood banner across the top.
            var band = new GameObject("HeaderBand", typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(band, parent.gameObject);
            var brt = (RectTransform)band.transform;
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0, 230);
            brt.anchoredPosition = Vector2.zero;
            Panel(band.GetComponent<Image>(), Wood);

            var title = MakeText("TitleHeader", brt, new Vector2(960, 100), new Vector2(0, -70), 62, _titleFont, Gold);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            title.text = "Rewards & Achievements";

            summaryText = MakeText("Summary", brt, new Vector2(600, 50), new Vector2(0, -170), 34, _bodyFont, Cream);
            var srt = (RectTransform)summaryText.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            summaryText.text = "Earned 0 / 0";
        }

        private static Transform BuildScrollGrid(Transform parent)
        {
            var scrollGo = new GameObject("Gallery ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            GameObjectUtility.SetParentAndAlign(scrollGo, parent.gameObject);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0.04f, 0.1f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.86f);
            scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;
            var well = scrollGo.GetComponent<Image>();
            Panel(well, ForestDeep);
            well.color = new Color(ForestDeep.r, ForestDeep.g, ForestDeep.b, 0.6f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            GameObjectUtility.SetParentAndAlign(viewportGo, scrollGo);
            var viewportRt = (RectTransform)viewportGo.transform;
            Stretch(viewportRt);
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            GameObjectUtility.SetParentAndAlign(contentGo, viewportGo);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 300);
            grid.spacing = new Vector2(28, 28);
            grid.padding = new RectOffset(30, 30, 30, 30);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            return contentGo.transform;
        }

        private static void BuildBackButton(Transform parent)
        {
            var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(320, 100);
            rt.anchoredPosition = new Vector2(0, 50);
            var img = go.GetComponent<Image>();
            Panel(img, Gold);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.86f, 0.45f, 1f);
            colors.pressedColor = new Color(0.8f, 0.62f, 0.2f, 1f);
            btn.colors = colors;

            var label = MakeText("Label", rt, new Vector2(320, 100), Vector2.zero, 38, _titleFont, Wood);
            Stretch((RectTransform)label.transform);
            label.text = "Back";

            var nav = go.AddComponent<GallerySceneNav>();
            btn.onClick.AddListener(nav.Back);
        }

        // ---------- Primitives ----------

        /// <summary>Applies the rounded sliced sprite (if available) and a tint to an Image.</summary>
        private static void Panel(Image img, Color color)
        {
            if (_round != null) { img.sprite = _round; img.type = Image.Type.Sliced; }
            img.color = color;
        }

        private static GameObject MakeImage(string name, RectTransform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, Vector2 size, Vector2 pos,
                                         float fontSize, TMP_FontAsset font, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Ellipsis;
            if (font != null) t.font = font;
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
