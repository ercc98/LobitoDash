using UnityEngine;
using ErccDev.Foundation.Core.Notifications;
using MagicVillageDash.Audio;

namespace MagicVillageDash.Score
{
    /// <summary>
    /// Listens to <see cref="RunScoreSystem"/>'s "new best" events and celebrates them — fires an
    /// optional particle burst and/or pushes a toast through the <see cref="NotificationService"/>.
    /// Reward-agnostic and read-only: it never mutates the score system, it only reacts to it, so the
    /// scoring/persistence layer stays free of presentation concerns.
    ///
    /// The best-record events fire the instant the run ends (inside <c>CommitIfBest</c>), which is ~2s
    /// before the game-over panel slides in. Pick the timing with <see cref="celebrateImmediately"/>:
    ///   • true  — react the moment the record is set.
    ///   • false — just remember which records broke; call <see cref="ShowPendingCelebrations"/> when
    ///             the panel opens so the burst lands with it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BestRecordCelebrator : MonoBehaviour
    {
        [Header("Source (auto-found if left empty)")]
        [Tooltip("Anything implementing IRunScoreSystem — the best-record events live on the interface.")]
        [SerializeField] private MonoBehaviour runScoreSystemProvider;

        private IRunScoreSystem runScoreSystem;

        [Header("When to celebrate")]
        [Tooltip("On = burst the moment a record is set. Off = wait for ShowPendingCelebrations().")]
        [SerializeField] private bool celebrateImmediately = false;

        [Header("Particle bursts (optional, per record type)")]
        [SerializeField] private ParticleSystem scoreVfx;
        [SerializeField] private ParticleSystem distanceVfx;
        [SerializeField] private ParticleSystem coinVfx;
        [SerializeField] private ParticleSystem gameOverVfx;
        [SerializeField] private ParticleSystem gameOverVfx2;
        [SerializeField] private ParticleSystem gameOverVfx3;

        [Header("Toast notifications")]
        [SerializeField] private bool pushNotifications = true;
        [SerializeField] private NotificationCategory category = NotificationCategory.Reward;
        [SerializeField] private Sprite recordIcon;
        [SerializeField, Min(0.1f)] private float toastDuration = NotificationData.DefaultDuration;

        // Captured while celebrateImmediately is off; consumed by ShowPendingCelebrations().
        private bool _pendingScore;
        private bool _pendingDistance;
        private bool _pendingCoins;
        private int _lastScore;
        private float _lastDistance;
        private int _lastCoins;

        private void Awake()
        {
            runScoreSystem = runScoreSystemProvider as IRunScoreSystem
                          ?? FindAnyObjectByType<RunScoreSystem>(FindObjectsInactive.Exclude);
        }

        private void OnEnable()
        {
            if (runScoreSystem == null) return;
            runScoreSystem.OnBestScoreChanged    += HandleScore;
            runScoreSystem.OnBestDistanceChanged += HandleDistance;
            runScoreSystem.OnBestCoinsChanged    += HandleCoins;
        }

        private void OnDisable()
        {
            if (runScoreSystem == null) return;
            runScoreSystem.OnBestScoreChanged    -= HandleScore;
            runScoreSystem.OnBestDistanceChanged -= HandleDistance;
            runScoreSystem.OnBestCoinsChanged    -= HandleCoins;
        }

        // ---------- Event handlers ----------

        private void HandleScore(int best)
        {
            _lastScore = best;
            if (celebrateImmediately) CelebrateScore();
            _pendingScore = true;
        }

        private void HandleDistance(float best)
        {
            _lastDistance = best;
            if (celebrateImmediately) CelebrateDistance();
            _pendingDistance = true;
        }

        private void HandleCoins(int best)
        {
            _lastCoins = best;
            if (celebrateImmediately) CelebrateCoins();
            _pendingCoins = true;
        }

        // ---------- Deferred trigger ----------

        /// <summary>
        /// Fire any records that broke this run, then clear them. Call this when the game-over panel
        /// opens (e.g. from SimpleGameMenus right after the panel is shown) when
        /// <see cref="celebrateImmediately"/> is off.
        /// </summary>
        public void ShowPendingCelebrations()
        {
            //if (_pendingScore)    CelebrateScore();
            //if (_pendingDistance) CelebrateDistance();
            //if (_pendingCoins)    CelebrateCoins();
            if (_pendingScore || _pendingDistance || _pendingCoins)
            {
                gameOverVfx?.Play();
                gameOverVfx2?.Play();
                gameOverVfx3?.Play();
                AudioManager.Instance.Play(VoiceId.Celebration);
            }
            _pendingScore = _pendingDistance = _pendingCoins = false;
        }

        // ---------- Celebrations ----------

        private void CelebrateScore() =>
            Celebrate(scoreVfx, "New Best Score!", $"{_lastScore:n0} points");

        private void CelebrateDistance() =>
            Celebrate(distanceVfx, "New Best Distance!", $"{_lastDistance:n0} m");

        private void CelebrateCoins() =>
            Celebrate(coinVfx, "New Coin Record!", $"{_lastCoins:n0} coins");

        private void Celebrate(ParticleSystem vfx, string title, string message)
        {
            if (vfx != null) vfx.Play();
            if (pushNotifications)
                NotificationService.Notify(new NotificationData(title, message, recordIcon, category, toastDuration));
        }
    }
}
