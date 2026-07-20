using System;
using Cameras;
using Character;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class IntroCutsceneLogic
{
    [SerializeField] private Camera _cutsceneCamera;
    [SerializeField] private Camera _gameplayCamera;
    [SerializeField] private float _cutsceneDuration = 5f;
    [SerializeField] private float _pullBackDistance = 1.5f;
    [SerializeField] private float _blendToGameplayDuration = 0.8f;
    [SerializeField] private Ease _pullBackEase = Ease.InOutSine;
    [SerializeField] private Ease _blendEase = Ease.InOutSine;

    private Sequence _sequence;

    public void Play(CharacterControl characterControl, CameraControl cameraControl)
    {
        Camera gameplayCamera = GetGameplayCamera(cameraControl);

        if (_cutsceneCamera == null || gameplayCamera == null)
        {
            characterControl.SetMovementLocked(false);
            return;
        }

        characterControl.SetMovementLocked(true);
        cameraControl.SetInputLocked(true);
        SetCameraRendering(gameplayCamera, false);
        SetCameraRendering(_cutsceneCamera, true);

        Transform cutsceneTransform = _cutsceneCamera.transform;
        Vector3 startPosition = cutsceneTransform.position;
        Quaternion startRotation = cutsceneTransform.rotation;
        Vector3 pulledBackPosition = startPosition - cutsceneTransform.forward * _pullBackDistance;

        _sequence?.Kill();
        _sequence = DOTween.Sequence()
            .Append(cutsceneTransform.DOMove(pulledBackPosition, _cutsceneDuration).SetEase(_pullBackEase))
            .Append(cutsceneTransform.DOMove(gameplayCamera.transform.position, _blendToGameplayDuration).SetEase(_blendEase))
            .Join(cutsceneTransform.DORotateQuaternion(gameplayCamera.transform.rotation, _blendToGameplayDuration)
                .SetEase(_blendEase))
            .OnComplete(() => CompleteCutscene(characterControl, cameraControl, gameplayCamera,
                cutsceneTransform, startPosition, startRotation));
    }

    public void Dispose()
    {
        _sequence?.Kill();
    }

    private Camera GetGameplayCamera(CameraControl cameraControl)
    {
        if (_gameplayCamera != null)
        {
            return _gameplayCamera;
        }

        return cameraControl.GetComponent<Camera>();
    }

    private void CompleteCutscene(
        CharacterControl characterControl,
        CameraControl cameraControl,
        Camera gameplayCamera,
        Transform cutsceneTransform,
        Vector3 startPosition,
        Quaternion startRotation)
    {
        SetCameraRendering(_cutsceneCamera, false);
        SetCameraRendering(gameplayCamera, true);
        cutsceneTransform.SetPositionAndRotation(startPosition, startRotation);
        cameraControl.SetInputLocked(false);
        characterControl.SetMovementLocked(false);
    }

    private void SetCameraRendering(Camera targetCamera, bool isEnabled)
    {
        targetCamera.enabled = isEnabled;
    }
}
