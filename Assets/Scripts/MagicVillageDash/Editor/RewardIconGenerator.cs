using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using MagicVillageDash.Collections;

namespace MagicVillageDash.Editor
{
    /// <summary>
    /// Bakes a thumbnail icon for every reward prefab under <c>Assets/Prefabs/Rewards</c> that doesn't
    /// already have one, matching the existing <c>&lt;Prefab&gt;_Icon.png</c> convention (the older
    /// structures shipped with these; the newer drops did not). Each prefab is rendered on a transparent
    /// background from a 3/4 angle, saved as a Sprite, and assigned to the collection entry it belongs to
    /// (matched by shared name tokens) whenever that entry is still using the paw-print placeholder.
    ///
    /// Run via <b>MagicVillageDash ▸ Generate Reward Icons</b>. Idempotent: skips prefabs that already
    /// have an <c>_Icon.png</c>, so re-running only fills the gaps.
    /// </summary>
    public static class RewardIconGenerator
    {
        private const string RewardsDir = "Assets/Prefabs/Rewards";
        private const string EntryDir   = "Assets/Settings/Collection";
        private const string PawGuid    = "049e145ba838f7b4db356629876a7241";   // placeholder icon
        private const int    Size       = 256;

        private static readonly HashSet<string> StopWords = new() { "prefab", "the", "a", "of" };

        [MenuItem("MagicVillageDash/Generate Reward Icons")]
        public static void Generate()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { RewardsDir });
            var generated = new List<(string name, Sprite sprite)>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var dir  = Path.GetDirectoryName(path).Replace('\\', '/');
                var name = Path.GetFileNameWithoutExtension(path);
                var iconPath = $"{dir}/{name}_Icon.png";

                if (File.Exists(iconPath)) continue;   // already has an icon — leave it alone

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tex = RenderIcon(prefab, Size);
                if (tex == null)
                {
                    Debug.LogWarning($"[RewardIcons] Could not render '{name}' (no renderers?).");
                    continue;
                }

                File.WriteAllBytes(iconPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);
                ConfigureAsSprite(iconPath);

                generated.Add((name, AssetDatabase.LoadAssetAtPath<Sprite>(iconPath)));
                Debug.Log($"[RewardIcons] Baked {iconPath}");
            }

            if (generated.Count > 0)
            {
                AssignToEntries(generated);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[RewardIcons] Done — generated {generated.Count} icon(s).");
        }

        // ---------- Rendering ----------

        private static Texture2D RenderIcon(GameObject prefab, int size)
        {
            var pru = new PreviewRenderUtility();
            GameObject inst = null;
            try
            {
                inst = Object.Instantiate(prefab);
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                var bounds = CalcBounds(inst);
                if (bounds.size == Vector3.zero) return null;

                pru.AddSingleGO(inst);

                var cam = pru.camera;
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // transparent
                cam.fieldOfView     = 30f;
                cam.nearClipPlane   = 0.01f;
                cam.farClipPlane    = 1000f;

                // Frame the bounds from a friendly 3/4 angle.
                var viewDir = (Quaternion.Euler(22f, -40f, 0f) * Vector3.forward).normalized;
                float radius = bounds.extents.magnitude;
                float dist   = radius / Mathf.Sin(Mathf.Deg2Rad * cam.fieldOfView * 0.5f) * 1.15f;
                cam.transform.position = bounds.center - viewDir * dist;
                cam.transform.rotation = Quaternion.LookRotation(viewDir);

                pru.lights[0].intensity = 1.1f;
                pru.lights[0].transform.rotation = Quaternion.Euler(35f, -40f, 0f);
                pru.lights[1].intensity = 0.65f;
                pru.ambientColor = new Color(0.45f, 0.45f, 0.45f, 1f);

                pru.BeginPreview(new Rect(0, 0, size, size), GUIStyle.none);
                pru.Render(true, true);
                var rt = pru.EndPreview() as RenderTexture;

                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                RenderTexture.active = prevActive;
                return tex;
            }
            finally
            {
                if (inst != null) Object.DestroyImmediate(inst);
                pru.Cleanup();
            }
        }

        private static Bounds CalcBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void ConfigureAsSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            ti.textureType        = TextureImporterType.Sprite;
            ti.spriteImportMode   = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled      = false;
            ti.wrapMode           = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }

        // ---------- Assignment ----------

        private static void AssignToEntries(List<(string name, Sprite sprite)> generated)
        {
            var entries = AssetDatabase.FindAssets("t:ModelCollectionEntry", new[] { EntryDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<ModelCollectionEntry>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(e => e != null)
                .ToList();

            foreach (var (name, sprite) in generated)
            {
                if (sprite == null) continue;
                var tokens = Tokenize(name);

                ModelCollectionEntry best = null;
                int bestScore = 0;
                foreach (var e in entries)
                {
                    if (!IsPlaceholder(e.icon)) continue;   // don't clobber hand-picked icons
                    int score = Tokenize(e.title).Count(tokens.Contains);
                    if (score > bestScore) { bestScore = score; best = e; }
                }

                if (best != null && bestScore > 0)
                {
                    best.icon = sprite;
                    EditorUtility.SetDirty(best);
                    Debug.Log($"[RewardIcons] Assigned '{name}' icon to entry '{best.title}'.");
                }
                else
                {
                    Debug.Log($"[RewardIcons] No matching entry for '{name}' — icon created, assign it manually.");
                }
            }
        }

        private static bool IsPlaceholder(Sprite icon)
        {
            if (icon == null) return true;
            var path = AssetDatabase.GetAssetPath(icon);
            return string.IsNullOrEmpty(path) || AssetDatabase.AssetPathToGUID(path) == PawGuid;
        }

        /// <summary>Splits a name into lowercase word tokens (camelCase + separators), dropping noise.</summary>
        private static HashSet<string> Tokenize(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsUpper(c) && i > 0 && (char.IsLower(s[i - 1]) || char.IsDigit(s[i - 1])))
                    sb.Append(' ');
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }
            return sb.ToString().Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                     .Where(t => !StopWords.Contains(t))
                     .ToHashSet();
        }
    }
}
