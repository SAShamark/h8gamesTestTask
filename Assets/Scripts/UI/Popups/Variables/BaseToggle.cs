using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups.Variables
{
    [RequireComponent(typeof(Toggle))]
    public class BaseToggle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Image _image;
        [SerializeField] private bool _isColor;
        [SerializeField] private Color _activeColor;
        [SerializeField] private Color _inactiveColor;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private TMP_Text _text;

        public Toggle Toggle => _toggle;
        public TMP_Text Text => _text;

        private void Start()
        {
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }

        public void OnValueChanged(bool value)
        {
            UpdateVisualState(value);
            _toggle.isOn = value;
        }

        public virtual void UpdateVisualState(bool value)
        {
            if (_isColor)
            {
                _image.color = value ? _activeColor : _inactiveColor;
            }
            else
            {
                _image.sprite = value ? _activeSprite : _inactiveSprite;
            }
        }
    }
}