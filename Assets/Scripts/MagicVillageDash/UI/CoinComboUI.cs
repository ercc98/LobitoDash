using UnityEngine;
using TMPro;
using MagicVillageDash.Score;
using MagicVillageDash.Audio;
using ErccDev.Foundation.Audio;
namespace MagicVillageDash.UI
{
    public sealed class CoinComboUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text liveCounterText;   // small, corner
        [SerializeField] private TMP_Text comboResultText;   // big, center
        [SerializeField] private CanvasGroup liveGroup;
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private MonoBehaviour coinCounterProvider;
        [SerializeField] private Animator coinComboAnimator;
        [SerializeField] private Animator resultCoinComboAnimator;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float comboTimeout = 1.5f; // seconds without a coin to end combo
        [SerializeField, Min(0f)] private float resultShowSeconds = 1.25f;

        private int _coinsInCombo;
        private int _currentCombo;
        private float _lastCoinTime;
        private bool _comboActive;
        private ICoinCounter coinCounter;

        void Awake()
        {
            ShowLive(false);
            ShowResult(false);
            coinCounter = coinCounterProvider as ICoinCounter ?? FindAnyObjectByType<CoinCounter>(FindObjectsInactive.Exclude);
        }

        void OnEnable()
        {
            coinCounter.CoinsChanged += RegisterCoinPickup;
        }

        void OnDisable()
        {
            coinCounter.CoinsChanged -= RegisterCoinPickup;
        }

        void Update()
        {
            if (_comboActive && Time.time - _lastCoinTime >= comboTimeout)
            {
                EndCombo();
            }
        }

        public void RegisterCoinPickup(int amount = 1)
        {
            _coinsInCombo++;
            coinComboAnimator.SetTrigger("LevelUp");
            int currentCombo = _coinsInCombo / 20 + 1;
            if (currentCombo > _currentCombo && coinComboAnimator != null)
            {
                coinComboAnimator.SetTrigger("LevelUp");
                _currentCombo = currentCombo;
                liveCounterText.text = $"x{_currentCombo}";
            }
            
            _lastCoinTime = Time.time;
            _comboActive = true;

            
            if(_currentCombo > 1) 
                ShowLive(true);
        }

        private void EndCombo()
        {
            _comboActive = false;
            ShowLive(false);

            if (_currentCombo > 1)
            {
                comboResultText.text = $"+{_currentCombo * _coinsInCombo}";
                ShowResult(true);
                Invoke(nameof(HideResult), resultShowSeconds);
                coinCounter.Add(_currentCombo * _coinsInCombo);
            }
            _currentCombo = 0;
            _coinsInCombo = 0;
        }

        private void HideResult() => ShowResult(false);

        private void ShowLive(bool show)
        {
            if (liveGroup == null) return; 
            
            liveGroup.alpha = show ? 1f : 0f;
            liveGroup.blocksRaycasts = show;
            liveGroup.interactable = show;
        }

        private void ShowResult(bool show)
        {
            if (resultGroup == null) return;

            resultGroup.alpha = show ? 1f : 0f;
            resultGroup.blocksRaycasts = show;
            resultGroup.interactable = show;
            if (resultCoinComboAnimator != null && show)
            {
                resultCoinComboAnimator.SetTrigger("Show");
                AudioManager.Instance?.Play("ResultCombo", SoundCategory.SFX);   
            }
            
        }
    }
}