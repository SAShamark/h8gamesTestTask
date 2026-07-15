using UI.Screens.Base;
using System;
using Game.Entities.Units.Character.Parts.Inventory;
using UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens.Variants.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private FloatingJoystick _floatingJoystick;
        [SerializeField] private Button _chargeButton;
        [SerializeField] private Animator _chargeButtonAnimator;
        [SerializeField] private CurrencyView[] _currencyViews;

        private Action _chargeAction;
        protected readonly int IsEnable = Animator.StringToHash("IsEnable");

        public FloatingJoystick Joystick => _floatingJoystick;

        public void BindInventory(Inventory inventory)
        {
            foreach (CurrencyView currencyView in _currencyViews)
            {
                currencyView.Bind(inventory);
            }
        }

        public void ShowChargeButton(Action chargeAction)
        {
            _chargeButton.gameObject.SetActive(true);
            _chargeAction = chargeAction;
            _chargeButton.interactable = true;
            _chargeButton.onClick.RemoveListener(HandleChargeClicked);
            _chargeButton.onClick.AddListener(HandleChargeClicked);
            _chargeButtonAnimator.SetBool(IsEnable, true);
        }

        public void HideChargeButton()
        {
            _chargeButton.gameObject.SetActive(false);
            _chargeButtonAnimator.SetBool(IsEnable, false);
            _chargeButton.onClick.RemoveListener(HandleChargeClicked);
            _chargeAction = null;
        }

        private void HandleChargeClicked()
        {
            _chargeButton.gameObject.SetActive(false);
            _chargeButton.interactable = false;
            _chargeButton.onClick.RemoveListener(HandleChargeClicked);
            _chargeAction.Invoke();
        }
    }
}
