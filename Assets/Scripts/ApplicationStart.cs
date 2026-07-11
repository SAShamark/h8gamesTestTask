using Services;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationStart : MonoBehaviour
{
    [SerializeField] private ServicesManager _servicesManager;

    private const string TARGET_SCENE_NAME = "Game";

    private void Awake()
    {
        Application.targetFrameRate = ValueConstants.TARGET_FRAME_RATE;
        _servicesManager.Initialize();
        SceneManager.LoadSceneAsync(TARGET_SCENE_NAME, LoadSceneMode.Additive);
    }
}