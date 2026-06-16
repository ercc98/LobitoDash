using ErccDev.Foundation.Data;
using MagicVillageDash.Audio;
using UnityEngine;

namespace MagicVillageDash.Settings
{
    public class SettingsApplier : MonoBehaviour, ISettingsApplier
    {
        public static SettingsApplier _instance;
        [Header("Apply on startup")]
        [SerializeField] private SettingsData settings; // applied once on Start so saved settings take effect before the menu opens

        [Header("Optional")]
        [SerializeField] private MonoBehaviour audioManagerProvider; // should be an AudioManager (optional)

        private AudioManager audioManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            DontDestroyOnLoad(gameObject);
            audioManager = audioManagerProvider as AudioManager;
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioManager>();
            }
            if(audioManager == null)
            {
                Debug.LogWarning($"{nameof(SettingsApplier)}: No AudioManager found. Audio settings will not be applied.");
            }
        }

        private void Start()
        {
            // Push saved settings to audio/graphics at boot; the menu only re-applies on edits.
            if (settings != null) Apply(settings);
        }
        
        public void Apply(SettingsData settings)
        {
            if (settings == null) return;            
            settings.Clamp();

            AudioListener.volume = settings.MasterVolume;

            GraphicsQualityManager.Apply(settings.QualityLevel);

            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<AudioManager>();
            }
            
            audioManager.SetMusicVolume(settings.MusicVolume);
            audioManager.SetSfxVolume(settings.SfxVolume);
            audioManager.SetAmbientVolume(settings.AmbientVolume);
            audioManager.SetUiVolume(settings.UiVolume);
            audioManager.SetVoiceVolume(settings.VoiceVolume);

            audioManager.SetMusicEnabled(settings.MusicEnabled);
            audioManager.SetAmbientEnabled(settings.AmbientEnabled);
            audioManager.SetSfxEnabled(settings.SfxEnabled);
            audioManager.SetUiEnabled(settings.UiEnabled);
            audioManager.SetVoiceEnabled(settings.VoiceEnabled);

            // Vibration: apply via your own vibration service if you have one.
            // Example: VibrationService.Enabled = settings.Vibration;
        }
        
    }
}