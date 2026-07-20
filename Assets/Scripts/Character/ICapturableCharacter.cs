using UnityEngine;

namespace Character
{
    public interface ICapturableCharacter
    {
        bool CanBeCaptured { get; }
        bool TryBeginCapture();
        void SetCapturedPose(Vector3 position, Quaternion rotation);
        void Throw(Vector3 velocity, Vector3 angularVelocity);
        void CancelCapture();
    }
}
