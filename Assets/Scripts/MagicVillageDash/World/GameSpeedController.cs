using System;
using UnityEngine;

namespace MagicVillageDash.World
{
    /// <summary>Holds the forward world speed. Everyone reads from here.</summary>
    public sealed class GameSpeedController : MonoBehaviour, IGameSpeedController 
    {
        [SerializeField] private float baseSpeed = 8f;
        [SerializeField] private float accelerationPerSecond = 0.15f;
        [SerializeField] private float maxSpeed = 30f;
        [SerializeField] private bool stop = false;
        [SerializeField] private float currentSpeed;

        public float CurrentSpeed
        {
            get => currentSpeed;
            private set => currentSpeed = value;
        }

        public float BaseSpeed => baseSpeed;

        public float MaxSpeed
        {
            get => maxSpeed;
            private set => maxSpeed = value;
        }

        void Awake() => currentSpeed = baseSpeed;

        void Update()
        {
            if (stop) return;
            if (currentSpeed < maxSpeed)
                currentSpeed = Mathf.Min(maxSpeed, currentSpeed + accelerationPerSecond * Time.deltaTime);
        }
        public void SetSpeed(float value)
        {
            if (value.Equals(0))
                stop = true;
            currentSpeed = Mathf.Max(0f, value);

        }
        public void ResetSpeed()
        {
            stop = false;
            currentSpeed = baseSpeed;
        }
    }
}
