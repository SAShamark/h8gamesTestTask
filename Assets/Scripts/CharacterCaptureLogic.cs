using System.Collections;
using UnityEngine;

[System.Serializable]
public class CharacterCaptureLogic
{
    private MonoBehaviour _coroutineRunner;
    private CharacterControl _characterControl;
    private Transform _characterTransform;
    private CharacterController _characterController;
    private CameraControl _cameraControl;
    private CharacterRagdollLogic _ragdollLogic;
    private Coroutine _throwRoutine;

    public void Initialize(
        CharacterControl characterControl,
        Transform characterTransform,
        CharacterController characterController,
        CameraControl cameraControl,
        CharacterRagdollLogic ragdollLogic)
    {
        _coroutineRunner = characterControl;
        _characterControl = characterControl;
        _characterTransform = characterTransform;
        _characterController = characterController;
        _cameraControl = cameraControl;
        _ragdollLogic = ragdollLogic;
    }

    public void BeginCapture()
    {
        _characterController.enabled = false;
    }

    public void SetCapturedPose(Vector3 position, Quaternion rotation)
    {
        _characterTransform.SetPositionAndRotation(position, rotation);
    }

    public void Throw(Vector3 velocity)
    {
        _cameraControl.ForceThirdPersonView();
        _ragdollLogic.BeginThrow();
        _ragdollLogic.Throw(velocity);
        _throwRoutine = _coroutineRunner.StartCoroutine(RecoverAfterThrow());
    }

    public void RestoreActiveState()
    {
        _ragdollLogic.CancelRagdoll();
        _cameraControl.RestoreForcedViewMode();
        EnableCharacter();
    }

    public void Dispose()
    {
        if (_throwRoutine != null)
        {
            _coroutineRunner.StopCoroutine(_throwRoutine);
        }
    }

    private IEnumerator RecoverAfterThrow()
    {
        yield return _ragdollLogic.WaitForLanding();
        _characterControl.SetState(CharacterControl.CharacterState.Recovering);
        yield return _ragdollLogic.Recover();
        _cameraControl.RestoreForcedViewMode();
        EnableCharacter();
        _throwRoutine = null;
    }

    private void EnableCharacter()
    {
        _characterController.enabled = true;
        _characterControl.SetState(CharacterControl.CharacterState.Active);
    }
}
