using System;
using UnityEngine;
using ErccDev.Foundation.Core.Save;
using MagicVillageDash.Data;
using ErccDev.Foundation.Core.Notifications; // SaveService

namespace MagicVillageDash.Score
{
    /// <summary>Aggregates coins + distance into a single score.</summary>
    public sealed class RunScoreSystem : MonoBehaviour, IRunScoreSystem
    {
        [Header("Sources")]
        [SerializeField] private MonoBehaviour distanceTrackerProvider;
        [SerializeField] private MonoBehaviour coinCounterProvider;

        [Header("Scoring")]
        [Tooltip("Points per whole meter traveled.")]
        [SerializeField] private int pointsPerMeter = 1;
        [Tooltip("Points per coin collected.")]
        [SerializeField] private int pointsPerCoin  = 10;

        [Header("Runtime (read-only)")]
        [SerializeField] private int   currentScore;
        [SerializeField] private int   bestScore;
        [SerializeField] private float bestDistance;
        [SerializeField] private int   bestCoins;

        public int   CurrentScore       => currentScore;
        public int   BestScore          => bestScore;
        public float BestDistance       => bestDistance;
        public int   BestCoins          => bestCoins;

        public bool HasBestScore => bestScore > 0;
        public bool HasBestDistance => bestDistance > 0f;
        public bool HasBestCoins => bestCoins > 0;

        private bool isFirstCongratzForBestScore = true;
        private bool isFirstCongratzForBestDistanc = true;
        private bool isFirstCongratzForBestCoins = true;

        public event Action<int>   OnScoreChanged;
        public event Action<int>   OnBestScoreChanged;
        public event Action<float> OnBestDistanceChanged;
        public event Action<int>   OnBestCoinsChanged;

        public RunStatsData runStatsData;

        IDistanceTracker distanceTracker;
        ICoinCounter     coinCounter;

        void Awake()
        {
            distanceTracker = distanceTrackerProvider   as IDistanceTracker     ?? FindAnyObjectByType<DistanceTracker>(FindObjectsInactive.Exclude);
            coinCounter = coinCounterProvider as ICoinCounter ?? FindAnyObjectByType<CoinCounter>(FindObjectsInactive.Exclude);
            GameDataService._instance.LoadAll();
            bestCoins = runStatsData.bestCoins;
            bestDistance = runStatsData.bestDistance;
            bestScore = runStatsData.bestScore;
        }

        void OnEnable()
        {
            if (coinCounter    != null) coinCounter.CoinsChanged       += OnCoinsChanged;
        }

        void OnDisable()
        {
            if (coinCounter != null) coinCounter.CoinsChanged -= OnCoinsChanged;
        }

        void Update()
        {
            RecomputeScore(distanceTracker.CurrentDistance);
            float dist = distanceTracker != null ? distanceTracker.CurrentDistance : 0f;
            int   c    = coinCounter != null ? coinCounter.Coins : 0;
            if (isFirstCongratzForBestScore && currentScore > bestScore && HasBestScore)
            {
                OnBestScoreChanged?.Invoke(bestScore);
                isFirstCongratzForBestScore = false;
            }

            if (isFirstCongratzForBestDistanc && dist > bestDistance && HasBestDistance)
            {
                OnBestDistanceChanged?.Invoke(bestDistance);
                isFirstCongratzForBestDistanc = false;
            }

            if (isFirstCongratzForBestCoins && c > bestCoins && HasBestCoins)
            {
                OnBestCoinsChanged?.Invoke(bestCoins);
                isFirstCongratzForBestCoins = false;
            }
        }

        void RecomputeScore(float meters)
        {
            int metersPoints = (int)meters * pointsPerMeter;
            int coinPoints   = (coinCounter != null ? coinCounter.Coins : 0) * pointsPerCoin;
            int newScore     = metersPoints + coinPoints;

            if (newScore != currentScore)
            {
                currentScore = newScore;
                OnScoreChanged?.Invoke(currentScore);
            }
        }

        void OnCoinsChanged(int _)
        {
            RecomputeScore(distanceTracker != null ? distanceTracker.CurrentDistance : 0f);
        }

        /// <summary>Call at Game Over to save bests.</summary>
        public void CommitIfBest()
        {
            float dist = distanceTracker != null ? distanceTracker.CurrentDistance : 0f;
            int   c    = coinCounter != null ? coinCounter.Coins : 0;

            bool changed = false;

            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                changed = true;
                //OnBestScoreChanged?.Invoke(bestScore);
                
            }

            if (dist > bestDistance)
            {
                bestDistance = dist;
                changed = true;
                //OnBestDistanceChanged?.Invoke(bestDistance);
            }

            if (c > bestCoins)
            {
                bestCoins = c;
                changed = true;
                //OnBestCoinsChanged?.Invoke(bestCoins);
            }

            if (changed)
            {
                runStatsData.RegisterRun(bestScore, bestDistance, bestCoins);
                GameDataService._instance.SaveAll();
                //SaveService.SaveObject(runStatsData, runStatsData.FileName);
            }
        }

        public void ResetRun()
        {
            currentScore = 0;
            OnScoreChanged?.Invoke(currentScore);
            coinCounter?.ResetCoins(0);
            distanceTracker?.ResetDistance();
        }
    }
}
