using UnityEngine;
using ErccDev.Foundation.Loader;
using PixeLadder.EasyTransition;

namespace MagicVillageDash.Loader
{
    /// <summary>
    /// Drops the Easy Transition effect into Foundation's <see cref="SceneLoader"/> at its single
    /// chokepoint, so every existing nav call (<c>SceneLoader.Instance.LoadSceneAsync(scene)</c>) fades
    /// out → loads → fades in without touching a single nav script.
    ///
    /// Safety first: if the transition can't run (no <see cref="SceneTransitioner"/> in the scene, one
    /// already mid-transition, or additive/addressable loads which the asset can't drive) we fall straight
    /// back to the vanilla instant load. The scene ALWAYS loads — a missing/busy transitioner degrades to a
    /// hard cut instead of a soft-lock or NRE.
    /// </summary>
    public sealed class TransitionSceneLoader : SceneLoader
    {
        [Tooltip("Effect to play on scene change. Leave null to use the SceneTransitioner's own default.")]
        [SerializeField] private TransitionEffect transitionEffect;

        public override void LoadSceneAsync(string sceneName, bool additive = false)
        {
            var transitioner = SceneTransitioner.Instance;

            // The asset only drives single-scene-by-name loads. Anything else, or no/busy transitioner,
            // takes the plain Foundation path so the load never gets dropped.
            if (additive || transitioner == null || transitioner.IsTransitioning)
            {
                base.LoadSceneAsync(sceneName, additive);
                return;
            }

            transitioner.LoadScene(sceneName, transitionEffect);
        }
    }
}
