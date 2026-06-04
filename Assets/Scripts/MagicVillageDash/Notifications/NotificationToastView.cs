using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ErccDev.Foundation.Core.Notifications;

namespace MagicVillageDash.Notifications
{
    /// <summary>
    /// The on-screen toast. Subclasses Foundation's view-agnostic <see cref="NotificationViewBase"/>
    /// (which feeds us one <see cref="NotificationData"/> at a time, in queue order) and draws it:
    /// title, optional message, optional icon, a category-driven accent tint, and a CanvasGroup
    /// fade / Animator pulse — mirroring the <c>CoinComboUI</c> show/hide style. Hides itself after
    /// the notification's own Duration so the next one slides in clean.
    /// </summary>
    public sealed class NotificationToastView : NotificationViewBase
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text     titleText;
        [SerializeField] private TMP_Text     messageText;
        [SerializeField] private Image        iconImage;
        [SerializeField] private Image        accent;        // optional strip/frame tinted per category
        [SerializeField] private Animator     animator;      // optional; "Show"/"Hide" triggers

        [Header("Category accent tints")]
        [SerializeField] private Color infoColor        = Color.white;
        [SerializeField] private Color achievementColor = new(1f, 0.84f, 0.2f);   // gold
        [SerializeField] private Color rewardColor      = new(0.4f, 0.85f, 1f);   // sky
        [SerializeField] private Color collectionColor  = new(0.7f, 0.5f, 1f);    // violet
        [SerializeField] private Color customColor      = Color.white;

        protected override void OnEnable()
        {
            base.OnEnable();   // binds to the service's OnNotification
            ShowGroup(false);
        }

        // ---------- Render ----------

        protected override void Show(NotificationData data)
        {
            if (titleText)   titleText.SetText(data.Title ?? string.Empty);
            if (messageText) messageText.SetText(data.Message ?? string.Empty);

            if (iconImage)
            {
                iconImage.sprite  = data.Icon;
                iconImage.enabled = data.Icon != null;
            }

            if (accent) accent.color = TintFor(data.Category);

            ShowGroup(true);
            if (animator) animator.SetTrigger("Show");

            // The manager spaces toasts by Duration; pull ourselves off a touch before the next.
            CancelInvoke(nameof(Hide));
            if (data.Duration > 0f)
                Invoke(nameof(Hide), data.Duration);
        }

        protected override void Hide()
        {
            if (animator) animator.SetTrigger("Hide");
            else          ShowGroup(false);
        }

        // ---------- Helpers ----------

        private Color TintFor(NotificationCategory category) => category switch
        {
            NotificationCategory.Achievement => achievementColor,
            NotificationCategory.Reward      => rewardColor,
            NotificationCategory.Collection  => collectionColor,
            NotificationCategory.Custom      => customColor,
            _                                => infoColor,
        };

        private void ShowGroup(bool show)
        {
            if (!group) return;
            group.alpha          = show ? 1f : 0f;
            group.blocksRaycasts = show;
            group.interactable   = show;
        }
    }
}
