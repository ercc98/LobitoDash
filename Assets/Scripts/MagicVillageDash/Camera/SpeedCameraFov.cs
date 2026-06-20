using Unity.Cinemachine;
using UnityEngine;
using MagicVillageDash.World;

namespace MagicVillageDash.Cameras
{
    /// <summary>
    /// Drives the runner camera's field of view from the current world speed: the FOV widens as
    /// the run accelerates. Maps <see cref="GameSpeedController"/>'s BaseSpeed→MaxSpeed range onto
    /// <see cref="baseFov"/>→<see cref="maxFov"/>, all tunable in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpeedCameraFov : MonoBehaviour
    {
        [Header("References (auto-found if left empty)")]
        [SerializeField] private CinemachineCamera targetCamera;
        [SerializeField] private MonoBehaviour speedProvider; // IGameSpeedController

        [Header("FOV range")]
        [Tooltip("Field of view held until the ramp threshold is reached.")]
        [SerializeField] private float baseFov = 45f;
        [Tooltip("Field of view when running at max speed.")]
        [SerializeField] private float maxFov = 80f;
        [Tooltip("Fraction of max speed below which the FOV stays at baseFov. " +
                 "0.5 = FOV only starts widening past half of max speed.")]
        [Range(0f, 1f)] [SerializeField] private float rampStartSpeedFraction = 0.5f;

        [Header("Feel")]
        [Tooltip("How quickly the FOV eases toward its speed-driven target. Higher = snappier.")]
        [Min(0f)] [SerializeField] private float smooth = 4f;

        private IGameSpeedController _speed;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<CinemachineCamera>()
                            ?? FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);

            _speed = speedProvider as IGameSpeedController
                  ?? FindAnyObjectByType<GameSpeedController>(FindObjectsInactive.Exclude);
        }

        private void LateUpdate()
        {
            if (targetCamera == null || _speed == null)
                return;

            float hi = _speed.MaxSpeed;
            // FOV stays at baseFov until speed crosses the threshold, then ramps to maxFov by max speed.
            float lo = Mathf.Max(_speed.BaseSpeed, hi * rampStartSpeedFraction);
            float t = hi <= lo
                ? 0f
                : Mathf.Clamp01((_speed.CurrentSpeed - lo) / (hi - lo));

            float targetFov = Mathf.Lerp(baseFov, maxFov, t);

            float current = targetCamera.Lens.FieldOfView;
            float next = smooth > 0f
                ? Mathf.Lerp(current, targetFov, Mathf.Clamp01(smooth * Time.deltaTime))
                : targetFov;

            targetCamera.Lens.FieldOfView = next;
        }
    }
}
