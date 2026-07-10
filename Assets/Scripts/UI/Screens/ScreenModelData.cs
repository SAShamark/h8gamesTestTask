using System;
using UI.Screens.Base;
using UnityEngine;

namespace UI.Screens
{
    [Serializable]
    public class ScreenModelData
    {
        [SerializeField] private ScreenTypes _screenType;
        [SerializeField] private BaseScreen _template;
        [SerializeField] private bool _isAddToStack;
        
        public ScreenTypes ScreenType => _screenType;
        public BaseScreen Template => _template;
        public bool IsAddToStack => _isAddToStack;
    }
}