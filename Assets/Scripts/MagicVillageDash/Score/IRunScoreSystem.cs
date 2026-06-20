using System;

namespace MagicVillageDash.Score
{
    public interface IRunScoreSystem
    {
        int CurrentScore { get; }
        int BestCoins { get; }
        int BestScore { get; }
        float BestDistance { get; }

        event Action<int>   OnBestScoreChanged;
        event Action<float> OnBestDistanceChanged;
        event Action<int>   OnBestCoinsChanged;

        void ResetRun();
        void CommitIfBest();
    }
}
