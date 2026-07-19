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
    private CharacterEffectsLogic _effectsLogic;
    private Coroutine _throwRoutine;

    public void Initialize(
        CharacterControl characterControl,
        Transform characterTransform,
        CharacterController characterController,
        CameraControl cameraControl,
        CharacterRagdollLogic ragdollLogic,
        CharacterEffectsLogic effectsLogic)
    {
        _coroutineRunner = characterControl;
        _characterControl = characterControl;
        _characterTransform = characterTransform;
        _characterController = characterController;
        _cameraControl = cameraControl;
        _ragdollLogic = ragdollLogic;
        _effectsLogic = effectsLogic;
    }

    public void BeginCapture()
    {
        _effectsLogic.StopStepDust();
        _characterController.enabled = false;
        _ragdollLogic.BeginCapture();
        _cameraControl.ForceCaptureView(_ragdollLogic.HipsTransform);
        _cameraControl.SetExtraCollisionIgnoreRoot(_ragdollLogic.ModelTransform);
    }

    public void SetCapturedPose(Vector3 position, Quaternion rotation)
    {
        _characterTransform.SetPositionAndRotation(position, rotation);
        _ragdollLogic.SetCapturedPose(position, rotation);
    }

    public void UpdateCapturedRagdoll()
    {
        _ragdollLogic.UpdateCapturedPose();
    }

    public void Throw(Vector3 velocity)
    {
        _cameraControl.ForceRagdollView(_ragdollLogic.HipsTransform, velocity);
        _cameraControl.SetExtraCollisionIgnoreRoot(_ragdollLogic.ModelTransform);
        _ragdollLogic.BeginThrow();
        _ragdollLogic.Throw(velocity);
        _throwRoutine = _coroutineRunner.StartCoroutine(RecoverAfterThrow());
    }

    public void RestoreActiveState()
    {
        _ragdollLogic.CancelRagdoll();
        _cameraControl.SetExtraCollisionIgnoreRoot(null);
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
        yield return _ragdollLogic.WaitForGroundContact();
        _effectsLogic.PlayLandingSmoke();
        yield return _ragdollLogic.WaitForLanding();
        _characterControl.SetState(CharacterControl.CharacterState.Recovering);
        yield return _ragdollLogic.Recover();
        _cameraControl.SetExtraCollisionIgnoreRoot(null);
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
