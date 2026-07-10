using System;

namespace UI.Popups
{
    public interface IPopupsManager
    {
        event Action<PopupTypes> OnPopupShowed; 
        event Action<PopupTypes> OnPopupHidden; 
        void ShowPopup(PopupTypes popupType);

        void HidePopup(PopupTypes popupType);
        BasePopup GetPopup(PopupTypes popupType);

        void HideAllPopups();
    }
}