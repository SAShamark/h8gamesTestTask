using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups.Variables
{
    public class ResultPopup : BasePopup
    {
        [SerializeField] private Button _button;

        public event Action OnButtonClicked;

        private void Start()
        {
            _button.onClick.AddListener(ButtonClicked);
        }

        private void ButtonClicked()
        {
            OnButtonClicked?.Invoke();
        }
    }
}