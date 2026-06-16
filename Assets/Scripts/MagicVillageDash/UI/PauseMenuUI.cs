using UnityEngine;
using UnityEngine.UI;
using ErccDev.Foundation.Pause;
using ErccDev.Foundation.Loader;
using ErccDev.Foundation.Data;
using MagicVillageDash.Audio;
using MagicVillageDash.Data;
using ErccDev.Foundation.Audio;

namespace MagicVillageDash.UI
{
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour pauseServiceProvider;
        


        [Header("UI")]
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;  
        [SerializeField] private Button quitButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private GameObject settingsMenuGO; // SettingsMenuUI (optional)

        [Header("Tutorial reset")]
        [SerializeField] private PlayerProfileData playerProfileData;

        [SerializeField] private bool useTimeScalePause = true;

        private IPauseService pause;

        private void Awake()
        {
            pause = pauseServiceProvider as IPauseService;
            if (pause == null)
            {
                Debug.LogError($"{nameof(PauseMenuUI)}: pauseServiceProvider must implement IPauseService.");
                enabled = false;
                return;
            }

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);

            // Optional buttons
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialClicked);
        }
        

        private void OnEnable()
        {
            pause.Changed += OnPauseChanged;

            ApplyPauseState(pause.IsPaused, "Sync");
        }

        private void OnDisable()
        {
            if (pause != null)
                pause.Changed -= OnPauseChanged;
        }

        private void OnPauseChanged(bool isPaused, string reason)
        {
            ApplyPauseState(isPaused, reason);
        }

        private void ApplyPauseState(bool isPaused, string reason)
        {
            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(isPaused);

            if (useTimeScalePause)
                Time.timeScale = isPaused ? 0f : 1f;
        }

        private void OnResumeClicked()
        {
            pause.Resume("ResumeButton");            
        }

        private void OnSettingsClicked()
        {
            settingsMenuGO?.SetActive(true);
            AudioManager.Instance.Play(UIId.Continue);
        }

        private void OnQuitClicked()
        {
            SceneLoader.Instance.LoadSceneAsync("MainMenuScene");
            AudioManager.Instance.Play(UIId.Back);
        }

        private void OnRestartClicked()
        {
            SceneLoader.Instance.LoadSceneAsync("RunnerScene");
            AudioManager.Instance.Play(UIId.Back);
            AudioManager.Instance?.StopLoop(SoundCategory.Music);
        }

        // Clears the "tutorial seen" flag and restarts the run so the tutorial replays from the top.
        private void OnTutorialClicked()
        {
            if (playerProfileData != null)
            {
                playerProfileData.TutorialCompleted = false;
                GameDataService._instance?.SaveAll();
            }

            pause.Resume("TutorialButton"); // restore timeScale before reloading
            SceneLoader.Instance.LoadSceneAsync("RunnerScene");
            AudioManager.Instance.Play(UIId.Back);
            AudioManager.Instance?.StopLoop(SoundCategory.Music);
        }
    }
}
