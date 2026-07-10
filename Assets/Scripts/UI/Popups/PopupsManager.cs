using System;
using System.Collections.Generic;
using System.Linq;
using UI.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UI.Popups
{
    [Serializable]
    public class PopupsManager : BaseWindowsManager<BasePopup>, IPopupsManager
    {
        [SerializeField] private GameObject _popupFader;

        private Dictionary<PopupTypes, PopupModelData> _popupModelsParsed = new();
        private List<BasePopup> _currentPopups = new();
        public event Action<PopupTypes> OnPopupShowed;
        public event Action<PopupTypes> OnPopupHidden;

        public override void OnConfigLoaded(WindowsConfig windowConfig)
        {
            base.OnConfigLoaded(windowConfig);

            foreach (PopupModelData pmd in WindowConfig.PopupModels)
            {
                if (!_popupModelsParsed.TryAdd(pmd.PopupType, pmd))
                {
                    Debug.LogError("There is already setup up " + pmd.PopupType + " cant fill it twice!");
                }
            }
        }


        public void ShowPopup(PopupTypes popupType)
        {
            if (!_popupModelsParsed.ContainsKey(popupType))
            {
                Debug.LogError("Not filled page by type: " + popupType);
                return;
            }

            PopupModelData popupModelData = _popupModelsParsed[popupType];
            var basePopup = Object.Instantiate(popupModelData.Template, _container);

            if (basePopup == null)
            {
                Debug.LogError(
                    "There is no BasePopup attached to : " + basePopup.gameObject.name + " of type " + popupType);
                return;
            }

            basePopup.PopupData = popupModelData;

            AddPopup(basePopup);
            OnPopupShowed?.Invoke(popupType);
        }

        public void HidePopup(PopupTypes popupType)
        {
            BasePopup currentPopup = _currentPopups.FirstOrDefault(r => r.PopupData.PopupType == popupType);
            if (currentPopup != null)
            {
                RemovePopup(currentPopup);
                OnPopupHidden?.Invoke(popupType);
            }
            else
            {
                Debug.LogError("There is no popup by type: " + popupType + " active now!");
            }
        }

        private void AddPopup(BasePopup popup)
        {
            _currentPopups.Add(popup);

            if (_currentPopups.Count >= 1)
            {
                bool anyUseTotalFader = _currentPopups.Any(currentPopup => currentPopup.PopupData.UseTotalFader);
                if (anyUseTotalFader)
                {
                    _popupFader.SetActive(true);
                }
            }

            popup.Show();
        }

        private void RemovePopup(BasePopup popup)
        {
            if (_currentPopups.Contains(popup))
            {
                _currentPopups.Remove(popup);
                bool anyUseTotalFader = _currentPopups.Any(currentPopup => currentPopup.PopupData.UseTotalFader);

                if (!anyUseTotalFader || _currentPopups.Count <= 0)
                {
                    _popupFader.SetActive(false);
                }
                else
                {
                    ShowNextPopup();
                }
            }

            Object.Destroy(popup.gameObject);
        }

        private void ShowNextPopup()
        {
            BasePopup newTopPopup = GetTopPopup();
            if (newTopPopup != null)
            {
                newTopPopup.Show();
            }
        }

        public BasePopup GetPopup(PopupTypes type)
        {
            var popup = _currentPopups.FirstOrDefault(popup => type == popup.PopupData.PopupType);
            if (popup == null)
            {
                Debug.LogError("There is no popup by type: " + type + " active now!");
                return null;
            }
            return popup;
        }

        private BasePopup GetTopPopup() => _currentPopups[^1];

        public void HideAllPopups()
        {
            if (_currentPopups is { Count: > 0 })
            {
                foreach (BasePopup popup in _currentPopups)
                {
                    RemovePopup(popup);
                }
            }
        }
    }
}