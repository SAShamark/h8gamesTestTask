using UI.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups
{
    public class BasePopup : BaseWindow
    {
        [SerializeField] private Button _closeButton;
        
        private string _defaultClipName = "PopupOn";

        public PopupModelData PopupData { get; set; }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (_animator != null)
            {
                _defaultClipName = _animator.runtimeAnimatorController.animationClips[0].name;
                _animator.SetBool(IsEnable, true);
            }

            if (_safeAreaFitter != null)
            {
                _safeAreaFitter.FitToSafeArea();
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();

                _closeButton.onClick.AddListener(CloseTrigger);
            }

            Debug.Log($"{gameObject.name} popup showed");
        }

        public virtual async void CloseTrigger()
        {
            CloseButtonClickedSound();
            if (_animator != null)
            {
                _canvasGroup.interactable = false;
                await HideAnimation(_defaultClipName);
            }

            UIManager.Instance.PopupsManager.HidePopup(PopupData.PopupType);
        }
    }
}