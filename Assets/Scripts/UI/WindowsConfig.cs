using UI.Popups;
using UI.Screens;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "ScriptableObjects/UI/UI config", order = 1)]
    public class WindowsConfig : ScriptableObject
    {
        [SerializeField] private ScreenModelData[] _screenModels;
        [SerializeField] private PopupModelData[] _popupModels;

        public ScreenModelData[] ScreenModels => _screenModels;
        public PopupModelData[] PopupModels => _popupModels;
    }
}