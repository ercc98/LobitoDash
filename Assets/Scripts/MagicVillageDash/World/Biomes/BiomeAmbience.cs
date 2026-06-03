using UnityEngine;

namespace MagicVillageDash.World.Biomes
{
    /// <summary>Wraps a biome's ambient ParticleSystem so it can be toggled by the controller.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class BiomeAmbience : MonoBehaviour, IBiomeAmbience
    {
        [Tooltip("Every biome that should show this particle system.")]
        [SerializeField] private BiomeDefinition[] biomes;
        [SerializeField] private ParticleSystem particles;

        public bool Handles(BiomeDefinition biome)
        {
            if (biome == null || biomes == null) return false;
            for (int i = 0; i < biomes.Length; i++)
                if (biomes[i] == biome) return true;
            return false;
        }

        private void Reset() => particles = GetComponent<ParticleSystem>();

        private void Awake()
        {
            if (particles == null) particles = GetComponent<ParticleSystem>();
            Deactivate(); // stay off until the player is inside this biome
        }

        public void Activate()
        {
            if (particles == null || particles.isPlaying) return;
            particles.Play(true);
        }

        public void Deactivate()
        {
            if (particles == null || !particles.isPlaying) return;
            // StopEmitting → existing particles finish their lifetime, no hard cut
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
