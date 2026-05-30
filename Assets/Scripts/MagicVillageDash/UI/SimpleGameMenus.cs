using UnityEngine;
using UnityEngine.SceneManagement;
using ErccDev.Foundation.Core.Gameplay;
using MagicVillageDash.Score;
using TMPro;
using System.Collections;
using MagicVillageDash.Audio;
using ErccDev.Foundation.Loader;

namespace MagicVillageDash.UI
{
    public sealed class SimpleGameMenus : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text recordCoinText;
        [SerializeField] private CoinCounter coinCounterProvider;
        [SerializeField] private string format = "{0}";
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text recordDistanceText;
        [SerializeField] private TMP_Text recordDistanceText2;
        [SerializeField] private DistanceTracker distanceCounterProvider;
        [SerializeField] private string distanceFormat = "{0} m"; // e.g., "123 m" 
        [SerializeField] private RunScoreSystem runScoreSystemProvider;
        bool _started;
        bool _ended;
        ICoinCounter coinCounter;
        IRunScoreSystem runScoreSystem;

        void Awake()
        {
            coinCounter = coinCounterProvider as ICoinCounter ?? FindAnyObjectByType<CoinCounter>(FindObjectsInactive.Exclude);
            runScoreSystem = runScoreSystemProvider as IRunScoreSystem ?? FindAnyObjectByType<RunScoreSystem>(FindObjectsInactive.Exclude);

            SetPanel(startPanel,  true);
            SetPanel(gameOverPanel, false);
            Time.timeScale = 0f;

            _started = false;
            _ended   = false;

            GameEvents.GameOver   += OnGameOver;
            GameEvents.GameStarted += OnGameStarted;
            recordDistanceText2.SetText(distanceFormat, runScoreSystem.BestDistance);
        }

        void OnDestroy()
        {
            GameEvents.GameOver   -= OnGameOver;
            GameEvents.GameStarted -= OnGameStarted;
        }

        // --- UI hooks ---
        public void OnPressStart()
        {
            if (_started) return;
            _started = true;
            
            SetPanel(startPanel, false);
            Time.timeScale = 1f;

            AudioManager.Instance.Play(UIId.Accept);
            GameEvents.RaiseGameStarted();
        }

        public void OnPressRestart()
        {
            // Restart current scene (use SceneLoader if you prefer)
            Time.timeScale = 1f;
            AudioManager.Instance.Play(UIId.Continue);
            SceneLoader.Instance.LoadSceneAsync(SceneManager.GetActiveScene().name);
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
            
        }

        // --- Event handlers ---
        void OnGameOver()
        {
            if (_ended) return;
            _ended = true;
            coinText.SetText(format, coinCounter.Coins);
            recordCoinText.SetText(format, runScoreSystem.BestCoins);

            distanceText.SetText(distanceFormat, distanceCounterProvider.CurrentDistance);
            recordDistanceText.SetText(distanceFormat, runScoreSystem.BestDistance);
            recordDistanceText2.SetText(distanceFormat, runScoreSystem.BestDistance);
            StartCoroutine(WaitToGameOverPanel());
            
        }

        IEnumerator WaitToGameOverPanel()
        {
            yield return new WaitForSeconds(2f);
            SetPanel(gameOverPanel, true);
            Time.timeScale = 0f;
        }

        void OnGameStarted() {  }

        static void SetPanel(GameObject go, bool on)
        {
            if (go && go.activeSelf != on) go.SetActive(on);
        }
    }
}