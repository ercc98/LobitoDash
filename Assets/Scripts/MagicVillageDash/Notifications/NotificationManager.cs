using UnityEngine;
using ErccDev.Foundation.Core.Notifications;
using MagicVillageDash.Audio;

namespace MagicVillageDash.Notifications
{
    /// <summary>
    /// Game-side notification engine. Subclasses Foundation's <see cref="NotificationManagerBase"/>
    /// (queue + one-at-a-time pump live in the base) and adds a little game flavor: an optional UI
    /// sting through <see cref="AudioManager"/> each time a toast surfaces. Registers itself as the
    /// <see cref="NotificationService"/> default in the base's OnEnable, so anything — achievements,
    /// collections, run records — can push a toast in one line. Same subclass-and-route pattern as
    /// <c>AchievementManager</c> / <c>CollectionManager</c>.
    /// </summary>
    public sealed class NotificationManager : NotificationManagerBase
    {
        [Header("Audio")]
        [Tooltip("Play a short UI sting when a toast appears.")]
        [SerializeField] private bool playSound = true;
        [SerializeField] private UIId stingSound = UIId.Accept;

        // ---------- Lifecycle ----------

        protected override void OnEnable()
        {
            base.OnEnable();          // registers as the NotificationService default
            OnNotification += HandleShown;
        }

        private void OnDisable() => OnNotification -= HandleShown;

        // ---------- Hooks ----------

        private void HandleShown(NotificationData data)
        {
            if (playSound)
                AudioManager.Instance?.Play(stingSound);
        }
    }
}
