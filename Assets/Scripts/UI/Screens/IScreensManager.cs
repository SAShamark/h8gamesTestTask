using System;
using UI.Screens.Base;
using UnityEngine.Events;

namespace UI.Screens
{
    public interface IScreensManager
    {
        event Action<ScreenTypes> OnScreenShowed;
        void ShowScreen(ScreenTypes screenTypes, bool isPrevious = false);
        void ShowPreviousScreen();
        void RemoveAllScreens();
        BaseScreen GetScreen(ScreenTypes screenType);
    }
}