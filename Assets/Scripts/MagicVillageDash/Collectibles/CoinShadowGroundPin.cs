using UnityEngine;

namespace MagicVillageDash.Collectibles
{
    /// <summary>
    /// Keeps a coin's ground shadow pinned to a fixed world height while the coin itself
    /// rides its jump arc (see <see cref="CoinRailGenerator"/>). Without this the shadow
    /// is a child of the coin root, so it floats up with the arc and looks wrong over
    /// obstacles. The shadow stays parented to the coin so it tracks X/Z for free — we only
    /// override its world Y so it lies on the ground.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoinShadowGroundPin : MonoBehaviour
    {
        [Tooltip("Shadow transform to keep grounded. Auto-found by the child named 'Shadow' if left empty.")]
        [SerializeField] private Transform shadow;
        [Tooltip("World-space Y the shadow rests at (the ground plane under the coins).")]
        [SerializeField] private float groundWorldY = -0.3f;

        void Awake()
        {
            if (!shadow) shadow = transform.Find("Shadow");
        }

        void LateUpdate()
        {
            if (!shadow) return;
            Vector3 p = shadow.position;
            p.y = groundWorldY;
            shadow.position = p;
        }
    }
}
