using ErccDev.Foundation.Audio;

namespace MagicVillageDash.Audio
{
    public sealed class AudioManager : AudioManagerBase, IAudioManager
    {
        public static AudioManager Instance { get; private set; }

        protected override void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            base.Awake();
        }

        // Typed convenience methods
        public void Play(AmbientId id) => Play(id, SoundCategory.Ambient);
        public void Play(MusicId   id) => Play(id, SoundCategory.Music);
        public void Play(SfxId     id) => Play(id, SoundCategory.SFX);
        public void Play(UIId      id) => Play(id, SoundCategory.UI);
        public void Play(VoiceId   id) => Play(id, SoundCategory.Voice);

        public void PlayLoop(AmbientId id) => PlayLoop(id, SoundCategory.Ambient);
        public void PlayLoop(MusicId   id) => PlayLoop(id, SoundCategory.Music);
        public void PlayLoop(SfxId     id) => PlayLoop(id, SoundCategory.SFX);

        // Is this exact clip already looping in its category? Lets callers avoid
        // restarting a loop that's already playing (e.g. same song across scenes).
        public bool IsLoopPlaying(MusicId   id) => IsLoopPlaying(id.ToString(), SoundCategory.Music);
        public bool IsLoopPlaying(AmbientId id) => IsLoopPlaying(id.ToString(), SoundCategory.Ambient);

        private bool IsLoopPlaying(string id, SoundCategory cat)
        {
            var src   = GetSource(cat);
            var entry = GetGroup(cat)?.Get(id);
            return src != null && entry?.clip != null &&
                   src.isPlaying && src.loop && src.clip == entry.clip;
        }

        public override void StopLoop(SoundCategory cat)=> base.StopLoop(cat);
        public void StopLoop(MusicId id)                => StopLoop(id, SoundCategory.Music);
        public void StopLoop(AmbientId id)              => StopLoop(id, SoundCategory.Ambient);
        public void StopLoop(SfxId id)                  => StopLoop(id, SoundCategory.SFX);
        public void StopLoop(UIId id)                   => StopLoop(id, SoundCategory.UI);
        public void StopLoop(VoiceId id)                => StopLoop(id, SoundCategory.Voice);


        public void SetMusicVolume(float v01)       => SetVolume(SoundCategory.Music, v01);
        public void SetSfxVolume(float v01)         => SetVolume(SoundCategory.SFX, v01);
        public void SetAmbientVolume(float v01)     => SetVolume(SoundCategory.Ambient, v01);
        public void SetUiVolume(float v01)          => SetVolume(SoundCategory.UI, v01);
        public void SetVoiceVolume(float v01)       => SetVolume(SoundCategory.Voice, v01);
        

        public void SetMusicEnabled(bool on)       => Toggle(SoundCategory.Music, on);
        public void SetSfxEnabled(bool on)         => Toggle(SoundCategory.SFX, on);
        public void SetAmbientEnabled(bool on)     => Toggle(SoundCategory.Ambient, on);
        public void SetUiEnabled(bool on)          => Toggle(SoundCategory.UI, on);
        public void SetVoiceEnabled(bool on)       => Toggle(SoundCategory.Voice, on);
    }
}