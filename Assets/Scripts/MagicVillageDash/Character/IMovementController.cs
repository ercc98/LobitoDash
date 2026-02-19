using UnityEngine;

namespace MagicVillageDash.Character
{
    public interface IMovementController
    {
        void Idle();
        void TurnLeft();
        void TurnRight();
        void MovingSpeed(float speed);
        void Crouch(bool isCrouching);
        void Jump();
        void Defend(bool isDefending);
    }
}
