using MagicVillageDash.Audio;
using UnityEngine;

namespace MagicVillageDash
{
    public class CharacterSoundEventController : MonoBehaviour
    {
        
        public void ScratchEvent()
        {
            //AudioManager.Instance.Play(VoiceId.Scratch);
            
        }
        public void YawmEvent()
        {
            //AudioManager.Instance.Play(VoiceId.Yawn);
        }
        public void BarkEvent()
        {
            AudioManager.Instance.Play(VoiceId.Bark);
        }
        public void HowlEvent()
        {
            AudioManager.Instance.Play(VoiceId.Howl);
        }
        public void DigEvent()
        {
            //AudioManager.Instance.Play(VoiceId.Dig);
        }
    }
}
