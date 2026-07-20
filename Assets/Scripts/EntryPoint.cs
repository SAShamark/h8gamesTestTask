using Cameras;
using Character;
using Tentacle;
using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    [SerializeField] private CameraControl _cameraControl;
    [SerializeField] private CharacterControl _characterControl;
    [SerializeField] private TentacleControl _tentacleControl;

    private void Awake()
    {
        _cameraControl.Initialize(_characterControl.transform);
        _characterControl.Initialize(_cameraControl);
        _tentacleControl.Initialize(_characterControl.transform);
    }
}
