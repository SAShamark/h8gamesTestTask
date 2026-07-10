using System.Threading.Tasks;
using UI.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens.Base
{
    public class BaseScreen : BaseWindow
    {
        [SerializeField] protected Button _backButton;

        private string _defaultClipName = "ScreenOn";
        public ScreenModelData ScreenData { get; set; }

        public virtual void Initialize()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();

                _backButton.onClick.AddListener(ShowPreviousScreen);
                _backButton.onClick.AddListener(CloseButtonClickedSound);
            }
        }

        protected async void ShowPreviousScreen()
        {
            CloseButtonClickedSound();
            
            UIManager.Instance.ScreensManager.ShowPreviousScreen();
        }

        protected void ShowScreen(ScreenTypes screenType)
        {
            UIManager.Instance.ScreensManager.ShowScreen(screenType);
        }

        public virtual void Show()
        {
            if (_safeAreaFitter != null)
            {
                _safeAreaFitter.FitToSafeArea();
            }

            _canvas.enabled = true;

            if (_animator != null)
            {
                _defaultClipName = _animator.runtimeAnimatorController.animationClips[0].name;
                _animator.SetBool(IsEnable, true);
                _canvasGroup.interactable = true;
            }
        }

        public virtual async Task Hide()
        {
            if (_animator != null)
            {
                _canvasGroup.interactable = false;
                await HideAnimation(_defaultClipName);
            }
            
            _canvas.enabled = false;
        }
    }
}