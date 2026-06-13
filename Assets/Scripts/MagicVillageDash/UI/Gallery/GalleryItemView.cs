using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MagicVillageDash.UI.Gallery
{
    /// <summary>
    /// One card in the rewards gallery: icon, title, description, and a locked state. Reused for both
    /// achievements and collection rewards — the controller fills it via <see cref="Bind"/> with whatever
    /// it knows (owned/unlocked yes-no, plus an optional 0..1 progress for achievements still in flight).
    /// Pure view: no service lookups, no persistence — it just renders what it's handed.
    /// </summary>
    public sealed class GalleryItemView : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Locked state")]
        [Tooltip("Dim overlay / lock badge shown while the entry isn't earned yet.")]
        [SerializeField] private GameObject lockedOverlay;
        [Tooltip("Tint applied to the icon while locked (e.g. dark silhouette).")]
        [SerializeField] private Color lockedTint = new(0.15f, 0.15f, 0.15f, 1f);
        [Tooltip("Placeholder sprite shown for hidden/secret entries that aren't earned yet.")]
        [SerializeField] private Sprite hiddenIcon;

        [Header("Progress (achievements)")]
        [Tooltip("Optional fill bar for partially-completed achievements. Hidden when owned or unused.")]
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressText;

        private Color _unlockedTint = Color.white;

        void Awake()
        {
            if (icon != null) _unlockedTint = icon.color;
        }

        /// <summary>
        /// Renders one entry. <paramref name="hidden"/> keeps secret entries faceless until earned;
        /// <paramref name="progress01"/> below 0 means "no progress bar" (collection rewards), otherwise
        /// it drives the fill while locked.
        /// </summary>
        public void Bind(Sprite sprite, string title, string description, bool owned,
                         bool hidden = false, float progress01 = -1f)
        {
            bool reveal = owned || !hidden;

            if (icon != null)
            {
                icon.sprite = reveal ? sprite : (hiddenIcon != null ? hiddenIcon : sprite);
                icon.color  = owned ? _unlockedTint : lockedTint;
                icon.enabled = icon.sprite != null;
            }

            if (titleText != null)
                titleText.text = reveal ? title : "???";

            if (descriptionText != null)
                descriptionText.text = reveal ? description : "Keep playing to discover this one.";

            if (lockedOverlay != null)
                lockedOverlay.SetActive(!owned);

            bool showBar = !owned && progress01 >= 0f;
            if (progressFill != null)
            {
                progressFill.transform.parent.gameObject.SetActive(showBar);
                progressFill.fillAmount = Mathf.Clamp01(progress01);
            }
            if (progressText != null && showBar)
                progressText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f)}%";
        }
    }
}
