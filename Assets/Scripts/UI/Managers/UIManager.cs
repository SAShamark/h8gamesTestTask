using Services;
using UI.Popups;
using UI.Screens;
using UnityEngine;

namespace UI.Managers
{
    public class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField] private ScreensManager _screensManager;
        [SerializeField] private PopupsManager _popupsManager;
        [SerializeField] private WindowsConfig _windowsConfig;

        public IScreensManager ScreensManager => _screensManager;
        public IPopupsManager PopupsManager => _popupsManager;

        private void Awake()
        {
            InitializeSingleton(false);
        }

        public void Init()
        {
            _screensManager.Initialize();
            _popupsManager.Initialize();
            ConfigLoaded();
        }

        private void ConfigLoaded()
        {
            _screensManager.OnConfigLoaded(_windowsConfig);
            _popupsManager.OnConfigLoaded(_windowsConfig);
        }
    }
}