using UnityEngine;
using ErccDev.Foundation.Pause;
using System;
using MagicVillageDash.Audio;

namespace MagicVillageDash.Pause
{

    public sealed class PauseServiceBehaviour : MonoBehaviour, IPauseService
    {
        private readonly PauseService service = new PauseService();

        public bool IsPaused => service.IsPaused;

        public event Action<bool, string> Changed
        {
            add    { service.Changed += value; }
            remove { service.Changed -= value; }
        }

        public void Pause(string reason = "Pause")
        {
            service.Pause(reason);
            AudioManager.Instance.Play(UIId.Accept);

        }
        public void Resume(string reason = "Resume")
        {
            service.Resume(reason);
            AudioManager.Instance.Play(UIId.Accept);
        }
        public void Toggle(string reason = "Toggle")
        {
            service.Toggle(reason);
          AudioManager.Instance.Play(UIId.Accept); 

        } 
    }
}
