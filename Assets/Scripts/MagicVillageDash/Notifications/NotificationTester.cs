#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using ErccDev.Foundation.Core.Notifications;

namespace MagicVillageDash.Notifications
{
    /// <summary>
    /// Dev-only harness for eyeballing the notification toast without playing a full run.
    /// Pushes through the static <see cref="NotificationService"/> facade, so it drives whatever
    /// <see cref="NotificationManager"/> / <see cref="NotificationToastView"/> is live in the scene.
    ///
    /// Two ways to fire:
    ///   • Bind the public Notify* methods to UGUI Button OnClick events, or
    ///   • Leave <see cref="showOnGuiButtons"/> on and tap the on-screen IMGUI buttons in Play mode
    ///     (zero wiring — just drop this component on any GameObject).
    ///
    /// To actually SEE a toast you need a <see cref="NotificationToastView"/> in the scene; add a
    /// <see cref="NotificationManager"/> too so duration/queueing behave like the real thing.
    /// </summary>
    public sealed class NotificationTester : MonoBehaviour
    {
        [Header("Optional icons (shown on the toast)")]
        [SerializeField] private Sprite achievementIcon;
        [SerializeField] private Sprite rewardIcon;
        [SerializeField] private Sprite collectionIcon;
        [SerializeField] private Sprite infoIcon;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float duration = NotificationData.DefaultDuration;

        [Header("On-screen test buttons")]
        [Tooltip("Draw IMGUI buttons in Play mode so you can fire toasts with no UI wiring.")]
        [SerializeField] private bool showOnGuiButtons = true;
        [SerializeField, Min(1f)] private float buttonScale = 2.5f;

        // ---------- Public API (bind these to UGUI Buttons) ----------

        public void NotifyInfo() => Push(
            "Welcome back!", "Day 3 streak  -  +100 coins", infoIcon, NotificationCategory.Info);

        public void NotifyAchievement() => Push(
            "Marathon Runner", "Run 1,000 m in a single run", achievementIcon, NotificationCategory.Achievement);

        public void NotifyReward() => Push(
            "Tasty Treat", "Lucky bone  -  +250 bonus coins", rewardIcon, NotificationCategory.Reward);

        public void NotifyCollection() => Push(
            "Ancient Relic", "Forest set  -  3 / 8 found", collectionIcon, NotificationCategory.Collection);

        public void NotifyCustom() => Push(
            "Custom Toast", "Anything you want here", null, NotificationCategory.Custom);

        /// <summary>Fires one of each, back-to-back, to exercise the queue.</summary>
        public void NotifyBurst()
        {
            NotifyInfo();
            NotifyAchievement();
            NotifyReward();
            NotifyCollection();
        }

        // ---------- Core ----------

        public void Push(string title, string message, Sprite icon, NotificationCategory category)
        {
            NotificationService.Notify(new NotificationData(title, message, icon, category, duration));
            Debug.Log($"[NotificationTester] {category}: {title}", this);
        }

        // ---------- Zero-wiring IMGUI buttons ----------

        private void OnGUI()
        {
            if (!showOnGuiButtons) return;

            var prev = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(buttonScale, buttonScale, 1f));

            float y = 10f, w = 150f, h = 34f, x = 10f, gap = 6f;
            if (GUI.Button(new Rect(x, y, w, h), "Info"))        NotifyInfo();        y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Achievement")) NotifyAchievement(); y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Reward"))      NotifyReward();      y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Collection"))  NotifyCollection();  y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Custom"))      NotifyCustom();      y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Burst (all)")) NotifyBurst();

            GUI.matrix = prev;
        }
    }
}
#endif
