using UnityEngine;
using ErccDev.Foundation.Audio;
using ErccDev.Foundation.Loader;
using MagicVillageDash.Audio;

namespace MagicVillageDash.UI
{
    /// <summary>
    /// The way back out of the den. Hooks these onto den buttons (Button.onClick) so the meta screen
    /// isn't a dead-end: <see cref="Race"/> jumps straight into a new run, <see cref="Home"/> returns to
    /// the title. Closes the run → den → run loop.
    /// </summary>
    public sealed class DenSceneNav : MonoBehaviour
    {
        [SerializeField] private string runnerSceneName = "RunnerScene";
        [SerializeField] private string introSceneName = "IntroScene";
        [SerializeField] private string collectionSceneName = "CollectionScene";
        [SerializeField] private string denSceneName  = "DenScene";

        public void Race() => Go(runnerSceneName, UIId.Continue);
        public void Home() => Go(introSceneName, UIId.Back);
        public void Colleciton() => Go(collectionSceneName, UIId.Continue);
        public void Den() => Go(denSceneName,  UIId.Continue);

        private void Go(string scene, UIId sfx)
        {
            AudioManager.Instance?.Play(sfx);
            Time.timeScale = 1f;
            SceneLoader.Instance.LoadSceneAsync(scene);
        }
    }
}
