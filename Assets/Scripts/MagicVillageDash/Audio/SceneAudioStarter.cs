using System.Collections;
using ErccDev.Foundation.Audio;
using UnityEngine;

namespace MagicVillageDash.Audio
{
    /// <summary>
    /// Drives looping music/ambient for a scene that has no run lifecycle to do it
    /// (e.g. the den). Reconciles on scene entry instead of blindly restarting: if the
    /// song this scene wants is already playing, it's left alone (no interruption); if
    /// it's different, it switches. AudioManager is a persistent singleton, so it may not
    /// exist yet when entering a scene directly in the editor — we wait a few frames for it.
    /// </summary>
    public class SceneAudioStarter : MonoBehaviour
    {
        [Header("What to loop")]
        [SerializeField] private bool playMusic = true;
        [SerializeField] private MusicId music = MusicId.GameTheme4;

        [SerializeField] private bool playAmbient = true;
        [SerializeField] private AmbientId ambient = AmbientId.Ambient1;

        [Header("How long to wait for AudioManager")]
        [SerializeField, Min(0)] private int maxWaitFrames = 30;

        private void OnEnable() => StartCoroutine(StartWhenReady());

        private IEnumerator StartWhenReady()
        {
            int frames = 0;
            while (AudioManager.Instance == null && frames++ < maxWaitFrames)
                yield return null;

            var audio = AudioManager.Instance;
            if (audio == null)
            {
                Debug.LogWarning($"{nameof(SceneAudioStarter)}: No AudioManager found; scene audio not started.", this);
                yield break;
            }

            // Music: keep playing if it's already the same loop, switch if different, stop if unwanted.
            if (playMusic)
            {
                if (!audio.IsLoopPlaying(music)) audio.PlayLoop(music);
            }
            else audio.StopLoop(SoundCategory.Music);

            // Ambient: same reconcile-on-entry behaviour.
            if (playAmbient)
            {
                if (!audio.IsLoopPlaying(ambient)) audio.PlayLoop(ambient);
            }
            else audio.StopLoop(SoundCategory.Ambient);
        }
    }
}
