using System;
using UnityEngine;

namespace UI.Popups
{
    [Serializable]
    public class PopupModelData
    {
        [SerializeField] private PopupTypes _popupType;
        [SerializeField] private BasePopup _template;
        [SerializeField] private bool _useTotalFader;
        
        public PopupTypes PopupType => _popupType;
        public BasePopup Template => _template;
        public bool UseTotalFader => _useTotalFader;
    } 
}