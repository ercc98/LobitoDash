using UnityEngine;
using GoogleMobileAds.Api;
using ErccDev.Foundation.Core.Gameplay;

namespace MagicVillageDash.AdMobScripts
{
    /// <summary>
    /// Loads and shows AdMob interstitials, gated to once every <see cref="gamesPerAd"/> finished runs.
    /// Counts each <see cref="GameEvents.GameOver"/>; the restart flow calls <see cref="TryShow"/>, which
    /// shows an ad (and resets the counter) only when enough games have passed and an ad is ready. A fresh
    /// ad is preloaded after each show so the next one is ready in time.
    /// </summary>
    public class Interstitial : MonoBehaviour
    {
        [Tooltip("AdMob interstitial unit id. The default is Google's TEST id — replace before release.")]
        [SerializeField] private string adUnitId = "ca-app-pub-3940256099942544/1033173712";
        [Tooltip("Show an interstitial once every N finished games.")]
        [SerializeField, Min(1)] private int gamesPerAd = 3;

        public static Interstitial Instance { get; private set; }
        private InterstitialAd interstitialAd;
        private int gamesSinceAd;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            GameEvents.GameOver += OnGameOver;
        }

        public void Start()
        {
            // Initialize the SDK once, then keep an ad warm.
            MobileAds.Initialize((InitializationStatus initStatus) => RequestInterstitial());
        }

        /// <summary>Each finished run counts toward the next ad.</summary>
        private void OnGameOver() => gamesSinceAd++;

        /// <summary>
        /// Show an interstitial if enough games have passed since the last one. Call this from the
        /// restart flow. Returns true if an ad was actually shown.
        /// </summary>
        public bool TryShow()
        {
            if (gamesSinceAd < gamesPerAd)
                return false;

            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                // Not ready yet — don't make the player wait; just make sure one is loading for next time.
                RequestInterstitial();
                return false;
            }

            gamesSinceAd = 0;
            interstitialAd.Show();
            return true;
        }

        public void RequestInterstitial()
        {
            // Drop any stale ad before loading a fresh one.
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            var adRequest = new AdRequest();
            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Interstitial ad failed to load with error: " + error?.GetMessage());
                    return;
                }

                interstitialAd = ad;
                RegisterEventHandlers(ad);
            });
        }

        private void RegisterEventHandlers(InterstitialAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                // Raised when the ad is estimated to have earned money.
            };
            ad.OnAdImpressionRecorded += () =>
            {
                // Raised when an impression is recorded for an ad.
            };
            ad.OnAdClicked += () =>
            {
                // Raised when a click is recorded for an ad.
            };
            ad.OnAdFullScreenContentOpened += () =>
            {
                // Raised when the ad opened full screen content.
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                // Closed — preload the next ad so it's ready for the next cycle.
                RequestInterstitial();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                // Failed to open — try to get a fresh ad ready.
                RequestInterstitial();
            };
        }

        void OnDestroy()
        {
            if (Instance == this)
                GameEvents.GameOver -= OnGameOver;

            if (interstitialAd != null)
                interstitialAd.Destroy();
        }
    }
}
