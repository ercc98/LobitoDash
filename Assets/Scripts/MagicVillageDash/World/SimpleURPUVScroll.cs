using UnityEngine;

namespace MagicVillageDash.World
{
    [RequireComponent(typeof(Renderer))]
    public class SimpleURPUVScroll : MonoBehaviour
    {
        // ── Shader property names (match your Cel Shader Graph references) ──
        private static readonly int OffsetID = Shader.PropertyToID("_Offset");
        private static readonly int TilingID = Shader.PropertyToID("_Tiling");

        [Header("Scroll Settings")]
        [SerializeField] private Vector2 uvPerSecond = new Vector2(0f, 1f);

        [Header("Optional: Link to World Speed")]
        [SerializeField] private GameSpeedController speedSource;
        [SerializeField] private float metersPerUV        = 1f;
        [SerializeField] private float directionMultiplier = 1f;

        [Header("Material Slot")]
        [SerializeField] private int materialIndex = 0;
        [SerializeField] private Renderer _ren;

        // ── Use MaterialPropertyBlock for GPU instancing / batching ──
        private MaterialPropertyBlock _mpb;
        private Vector2 _offset;
        private bool _valid;

        void Awake()
        {
            if (_ren == null) _ren = GetComponent<Renderer>();

            _mpb = new MaterialPropertyBlock();
            _ren.GetPropertyBlock(_mpb, materialIndex);

            // Validate the shader has our expected property
            var mats = _ren.sharedMaterials;
            materialIndex = Mathf.Clamp(materialIndex, 0, mats.Length - 1);
            var mat = mats[materialIndex];

            if (mat != null && mat.HasProperty(OffsetID))
            {
                // Seed offset from whatever the material already has set
                _offset = mat.GetVector(OffsetID);
                _valid  = true;
            }
            else
            {
                Debug.LogWarning($"[{nameof(SimpleURPUVScroll)}] '_Offset' not found on material '{mat?.name}'. " +
                                 "Check your Shader Graph property reference name.", this);
            }
        }

        void Update()
        {
            if (!_valid) return;

            // ── Accumulate offset ──
            if (speedSource != null)
            {
                float uvDeltaV = (speedSource.CurrentSpeed * Time.deltaTime * directionMultiplier)
                                 / Mathf.Max(0.0001f, metersPerUV);
                _offset.y += uvDeltaV;
            }
            else
            {
                _offset += uvPerSecond * Time.deltaTime;
            }

            // Wrap to avoid floating-point drift over long sessions
            _offset.x = Mathf.Repeat(_offset.x, 1f);
            _offset.y = Mathf.Repeat(_offset.y, 1f);

            // ── Push to GPU without creating a new material instance ──
            _mpb.SetVector(OffsetID, new Vector4(_offset.x, _offset.y, 0f, 0f));
            _ren.SetPropertyBlock(_mpb, materialIndex);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Let you tweak uvPerSecond at runtime in the Editor
            if (_mpb != null && _valid)
            {
                _mpb.SetVector(OffsetID, new Vector4(_offset.x, _offset.y, 0f, 0f));
                _ren.SetPropertyBlock(_mpb, materialIndex);
            }
        }
#endif
    }
}