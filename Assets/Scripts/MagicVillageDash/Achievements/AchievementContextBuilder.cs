using UnityEngine;
using ErccDev.Foundation.Core.Achievements;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Score;
using MagicVillageDash.Collections;

namespace MagicVillageDash.Achievements
{
    /// <summary>
    /// Gathers the run-time game services and exposes them as an <see cref="IAchievementContext"/>.
    /// Mirrors TutorialContextBuilder. Doubles as the context itself so it can be dropped straight
    /// onto the manager's <c>contextProvider</c> slot — the service map is built lazily on first
    /// access so it never races the manager's Awake.
    /// </summary>
    public sealed class AchievementContextBuilder : MonoBehaviour, IAchievementContextBuilder, IAchievementContext
    {
        [Header("Providers (assign in Inspector)")]
        [SerializeField] private MonoBehaviour runScoreSystemProvider;   // IRunScoreSystem
        [SerializeField] private MonoBehaviour coinCounterProvider;      // ICoinCounter
        [SerializeField] private MonoBehaviour distanceTrackerProvider;  // IDistanceTracker
        [SerializeField] private MonoBehaviour collectionProvider;       // ICollectionService (CollectionManager)

        [Header("Optional Auto-Find (fallback)")]
        [SerializeField] private bool autoFindIfMissing = true;

        private AchievementContext _context;

        public IAchievementContext Build()
        {
            var score = runScoreSystemProvider  as IRunScoreSystem;
            var coins = coinCounterProvider     as ICoinCounter;
            var dist  = distanceTrackerProvider as IDistanceTracker;
            var coll  = collectionProvider      as ICollectionService;

            if (autoFindIfMissing)
            {
                score ??= FindAnyObjectByType<RunScoreSystem>(FindObjectsInactive.Exclude);
                coins ??= FindAnyObjectByType<CoinCounter>(FindObjectsInactive.Exclude);
                dist  ??= FindAnyObjectByType<DistanceTracker>(FindObjectsInactive.Exclude);
                coll  ??= FindAnyObjectByType<CollectionManager>(FindObjectsInactive.Exclude);
            }

#if UNITY_EDITOR
            if (score == null) Debug.LogWarning("[AchievementContextBuilder] Missing IRunScoreSystem (assign runScoreSystemProvider).", this);
            if (coins == null) Debug.LogWarning("[AchievementContextBuilder] Missing ICoinCounter (assign coinCounterProvider).", this);
            if (dist  == null) Debug.LogWarning("[AchievementContextBuilder] Missing IDistanceTracker (assign distanceTrackerProvider).", this);
            if (coll  == null) Debug.LogWarning("[AchievementContextBuilder] Missing ICollectionService — UnlockCollectionEntry rewards won't grant.", this);
#endif

            return new AchievementContext()
                .Add<IRunScoreSystem>(score)
                .Add<ICoinCounter>(coins)
                .Add<IDistanceTracker>(dist)
                .Add<ICollectionService>(coll);
        }

        // ---------- IAchievementContext (delegates to the lazily-built map) ----------

        private AchievementContext Ctx => _context ??= (AchievementContext)Build();

        public bool TryGet<T>(out T service) where T : class => Ctx.TryGet(out service);
        public T    Get<T>() where T : class                 => Ctx.Get<T>();
    }
}
