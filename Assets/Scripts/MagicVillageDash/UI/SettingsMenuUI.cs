using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ErccDev.Foundation.Data;
using MagicVillageDash.Settings;
using MagicVillageDash.Audio;

namespace MagicVillageDash.UI
{
    public sealed class SettingsMenuUI : MonoBehaviour
    {
        [SerializeField] private SettingsData settings;
        [SerializeField] private MonoBehaviour settingsApplierProvider; // ISettingsApplier

        [Header("UI")]
        [SerializeField] private GameObject settingsMenuRoot;
        [SerializeField] private Button acceptButton;

        [Header("Audio")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider ambientSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;
        [SerializeField] private Slider voiceSlider;
        

        [Header("Audio Toggles")]
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle ambientToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle uiToggle;
        [SerializeField] private Toggle voiceToggle;

        [Header("Graphics / Gameplay")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle vibrationToggle;

        private ISettingsApplier applier;
        private bool suppressEvents;

        private void Awake()
        {
            applier = settingsApplierProvider as ISettingsApplier;
            if (acceptButton != null) acceptButton.onClick.AddListener(OnCloseButtonClicked);
            if (settings == null) Debug.LogError($"{nameof(SettingsMenuUI)}: SettingsData missing.", this);
            if (applier == null) Debug.LogError($"{nameof(SettingsMenuUI)}: applierProvider must implement ISettingsApplier.", this);
        }

        private void OnEnable()
        {
            if (settings == null) return;

            RefreshUIFromData();

            Bind();

            Apply();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void RefreshUIFromData()
        {
            suppressEvents = true;

            if (masterSlider) masterSlider.value = settings.MasterVolume;
            if (musicSlider) musicSlider.value = settings.MusicVolume;
            if (ambientSlider) ambientSlider.value = settings.AmbientVolume;
            if (sfxSlider) sfxSlider.value = settings.SfxVolume;
            if (uiSlider) uiSlider.value = settings.UiVolume;
            if (voiceSlider) voiceSlider.value = settings.VoiceVolume;


            if (musicToggle)   musicToggle.isOn   = settings.MusicEnabled;
            if (ambientToggle) ambientToggle.isOn = settings.AmbientEnabled;
            if (sfxToggle)     sfxToggle.isOn     = settings.SfxEnabled;
            if (uiToggle)      uiToggle.isOn      = settings.UiEnabled;
            if (voiceToggle)   voiceToggle.isOn   = settings.VoiceEnabled;

            if (qualityDropdown) qualityDropdown.value = settings.QualityLevel;
            if (vibrationToggle) vibrationToggle.isOn  = settings.Vibration;

            suppressEvents = false;
        }

        private void Bind()
        {
            if (masterSlider) masterSlider.onValueChanged.AddListener(OnMaster);
            if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicVol);
            if (ambientSlider) ambientSlider.onValueChanged.AddListener(OnAmbientVol);
            if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSfxVol);
            if (uiSlider) uiSlider.onValueChanged.AddListener(OnUiVol);
            if (voiceSlider) voiceSlider.onValueChanged.AddListener(OnVoiceVol);

            if (musicToggle)   musicToggle.onValueChanged.AddListener(OnMusicEnabled);
            if (ambientToggle) ambientToggle.onValueChanged.AddListener(OnAmbientEnabled);
            if (sfxToggle)     sfxToggle.onValueChanged.AddListener(OnSfxEnabled);
            if (uiToggle)      uiToggle.onValueChanged.AddListener(OnUiEnabled);
            if (voiceToggle)   voiceToggle.onValueChanged.AddListener(OnVoiceEnabled);

            if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQuality);
            if (vibrationToggle) vibrationToggle.onValueChanged.AddListener(OnVibration);
        }

        private void Unbind()
        {
            if (masterSlider) masterSlider.onValueChanged.RemoveListener(OnMaster);
            if (musicSlider) musicSlider.onValueChanged.RemoveListener(OnMusicVol);
            if (ambientSlider) ambientSlider.onValueChanged.RemoveListener(OnAmbientVol);
            if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSfxVol);
            if (uiSlider) uiSlider.onValueChanged.RemoveListener(OnUiVol);
            if (voiceSlider) voiceSlider.onValueChanged.RemoveListener(OnVoiceVol);

            if (musicToggle)   musicToggle.onValueChanged.RemoveListener(OnMusicEnabled);
            if (ambientToggle) ambientToggle.onValueChanged.RemoveListener(OnAmbientEnabled);
            if (sfxToggle)     sfxToggle.onValueChanged.RemoveListener(OnSfxEnabled);
            if (uiToggle)      uiToggle.onValueChanged.RemoveListener(OnUiEnabled);
            if (voiceToggle)   voiceToggle.onValueChanged.RemoveListener(OnVoiceEnabled);

            if (qualityDropdown) qualityDropdown.onValueChanged.RemoveListener(OnQuality);
            if (vibrationToggle) vibrationToggle.onValueChanged.RemoveListener(OnVibration);
        }

        private void Apply()
        {
            applier?.Apply(settings);
        }

        private void OnMaster(float v)          { if (suppressEvents) return; settings.MasterVolume = v; Apply(); }
        private void OnMusicVol(float v)        { if (suppressEvents) return; settings.MusicVolume = v; Apply(); }
        private void OnAmbientVol(float v)      { if (suppressEvents) return; settings.AmbientVolume = v; Apply(); }
        private void OnSfxVol(float v)          { if (suppressEvents) return; settings.SfxVolume = v; Apply(); }
        private void OnUiVol(float v)           { if (suppressEvents) return; settings.UiVolume = v; Apply(); }
        private void OnVoiceVol(float v)        { if (suppressEvents) return; settings.VoiceVolume = v; Apply(); }

        private void OnMusicEnabled(bool on)    { if (suppressEvents) return; settings.MusicEnabled   = on; Apply(); }
        private void OnAmbientEnabled(bool on)  { if (suppressEvents) return; settings.AmbientEnabled = on; Apply(); }
        private void OnSfxEnabled(bool on)      { if (suppressEvents) return; settings.SfxEnabled     = on; Apply(); }
        private void OnUiEnabled(bool on)       { if (suppressEvents) return; settings.UiEnabled      = on; Apply(); }
        private void OnVoiceEnabled(bool on) { if (suppressEvents) return; settings.VoiceEnabled = on; Apply(); }
        
        private void OnQuality(int idx)         { if (suppressEvents) return; settings.QualityLevel = idx; Apply(); }
        private void OnVibration(bool on) { if (suppressEvents) return; settings.Vibration = on; Apply(); }

        private void OnCloseButtonClicked()
        {
            gameObject.SetActive(false);
            AudioManager.Instance.Play(UIId.Continue);
        }
    }
}
