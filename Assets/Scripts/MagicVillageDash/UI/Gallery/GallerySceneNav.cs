using UnityEngine;
using ErccDev.Foundation.Audio;
using ErccDev.Foundation.Loader;
using MagicVillageDash.Audio;

namespace MagicVillageDash.UI.Gallery
{
    /// <summary>
    /// In/out routing for the rewards gallery so it isn't a dead-end. Hook <see cref="Open"/> onto a
    /// button in the intro/den/pause menus, and <see cref="Back"/> onto the gallery's back button to
    /// return to whichever screen opened it (defaults to the title). Mirrors <see cref="DenSceneNav"/>.
    /// </summary>
    public sealed class GallerySceneNav : MonoBehaviour
    {
        [SerializeField] private string gallerySceneName = "RewardsGalleryScene";
        [SerializeField] private string returnSceneName  = "IntroScene";

        public void Open() => Go(gallerySceneName, UIId.Accept);
        public void Back() => Go(returnSceneName,  UIId.Back);

        private void Go(string scene, UIId sfx)
        {
            AudioManager.Instance?.Play(sfx);
            Time.timeScale = 1f;
            SceneLoader.Instance.LoadSceneAsync(scene);
        }
    }
}
