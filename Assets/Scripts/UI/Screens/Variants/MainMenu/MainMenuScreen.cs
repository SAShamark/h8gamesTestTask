using UI.Managers;
using UI.Screens.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens.Variants.MainMenu
{
    public class MainMenuScreen : BaseScreen
    {
        [SerializeField] private Button _battleButton;

        private void Awake()
        {
            _battleButton.onClick.AddListener(BattleButtonClicked);
        }

        private void OnDestroy()
        {
             _battleButton.onClick.RemoveListener(BattleButtonClicked);
        }

        private void BattleButtonClicked()
        {
            UIManager.Instance.ScreensManager.ShowScreen(ScreenTypes.Gameplay);
        }
    }
}