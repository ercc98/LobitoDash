using UnityEngine;
using MagicVillageDash.World;

namespace MagicVillageDash.VFX
{
    /// <summary>
    /// Drives a particle system's emission rate-over-time from the current world speed: emission is
    /// held at <see cref="baseRate"/> until speed passes <see cref="rampStartSpeedFraction"/> of max
    /// speed, then ramps to <see cref="maxRate"/> by max speed. Used by the SpeedBoost_front VFX so
    /// the speed lines kick in only when the run is genuinely fast.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpeedEmissionBySpeed : MonoBehaviour
    {
        [Header("References (auto-found if left empty)")]
        [SerializeField] private ParticleSystem targetParticles;
        [SerializeField] private MonoBehaviour speedProvider; // IGameSpeedController

        [Header("Emission range (particles / second)")]
        [Tooltip("Emission held until the ramp threshold is reached.")]
        [Min(0f)] [SerializeField] private float baseRate = 0f;
        [Tooltip("Emission when running at max speed.")]
        [Min(0f)] [SerializeField] private float maxRate = 150f;
        [Tooltip("Fraction of max speed below which emission stays at baseRate. " +
                 "0.25 = particles only start ramping past a quarter of max speed.")]
        [Range(0f, 1f)] [SerializeField] private float rampStartSpeedFraction = 0.25f;

        [Header("Feel")]
        [Tooltip("How quickly emission eases toward its speed-driven target. 0 = instant.")]
        [Min(0f)] [SerializeField] private float smooth = 4f;

        private IGameSpeedController _speed;
        private ParticleSystem.EmissionModule _emission;
        private float _currentRate;

        private void Awake()
        {
            if (targetParticles == null)
                targetParticles = GetComponent<ParticleSystem>();

            _speed = speedProvider as IGameSpeedController
                  ?? FindAnyObjectByType<GameSpeedController>(FindObjectsInactive.Exclude);

            if (targetParticles != null)
            {
                _emission = targetParticles.emission;
                _currentRate = baseRate;
            }
        }

        private void LateUpdate()
        {
            if (targetParticles == null || _speed == null)
                return;

            float hi = _speed.MaxSpeed;
            float lo = Mathf.Max(_speed.BaseSpeed, hi * rampStartSpeedFraction);
            float t = hi <= lo ? 0f : Mathf.Clamp01((_speed.CurrentSpeed - lo) / (hi - lo));

            float target = Mathf.Lerp(baseRate, maxRate, t);
            _currentRate = smooth > 0f
                ? Mathf.Lerp(_currentRate, target, Mathf.Clamp01(smooth * Time.deltaTime))
                : target;

            _emission.rateOverTime = _currentRate;
        }
    }
}
