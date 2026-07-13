using DG.Tweening;
using Game.Entities.Character;
using Services;
using Services.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Widgets
{
    public class CurrencyView : MonoBehaviour
    {
        [SerializeField] private CurrencyType _type;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _image;
        [SerializeField] private float _currencyTweenDuration = 0.35f;
        [SerializeField] private float _floatingDeltaDistance = 90f;
        [SerializeField] private Vector3 _spendPulseStrength = new(0.18f, 0.18f, 0f);
        [SerializeField] private float _spendPulseDuration = 0.25f;

        private Inventory _inventory;
        private int _lastCurrencyValue;
        private int _displayedCurrencyValue;
        private Tween _currencyTween;
        private Tween _spendPulseTween;
        private Vector3 _textDefaultScale;

        public void Bind(Inventory inventory)
        {
            if (_inventory != null)
            {
                _inventory.OnItemsCountChanged -= HandleItemsCountChanged;
            }

            _inventory = inventory;
            _textDefaultScale = _text.transform.localScale;
            _inventory.OnItemsCountChanged += HandleItemsCountChanged;

            int itemsCount = _inventory.GetItemsCount(_type);
            _lastCurrencyValue = itemsCount;
            _displayedCurrencyValue = itemsCount;
            _text.text = NumberFormatter.FormatBalance(itemsCount);
            _image.sprite = ServicesManager.Instance.CurrencyService.CurrencyCollection.GetSprite(_type);
        }
        
        protected virtual void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnItemsCountChanged -= HandleItemsCountChanged;
            }

            _currencyTween?.Kill();
            _spendPulseTween?.Kill();
        }

        private void HandleItemsCountChanged(CurrencyType currencyType, int value)
        {
            if (currencyType == _type)
            {
                SetCurrencyText(value);
            }
        }

        private void SetCurrencyText(int value)
        {
            int delta = value - _lastCurrencyValue;
            _currencyTween?.Kill();

            if (gameObject.activeInHierarchy)
            {
                _currencyTween = CurrencyTextAnimator.AnimateNumber(_text, _displayedCurrencyValue, value,
                    _currencyTweenDuration, NumberFormatter.FormatBalance, displayedValue =>
                    {
                        _displayedCurrencyValue = displayedValue;
                    });
            }
            else
            {
                _displayedCurrencyValue = value;
                _text.text = NumberFormatter.FormatBalance(value);
            }

            if (delta < 0 && gameObject.activeInHierarchy)
            {
                PlaySpendPulse();
            }

            _lastCurrencyValue = value;
        }

        private void PlaySpendPulse()
        {
            _spendPulseTween?.Kill();
            _text.transform.localScale = _textDefaultScale;
            _spendPulseTween = _text.transform
                .DOPunchScale(_spendPulseStrength, _spendPulseDuration, 8, 0.8f)
                .SetLink(gameObject)
                .OnKill(() =>
                {
                    if (_text != null)
                    {
                        _text.transform.localScale = _textDefaultScale;
                    }
                });
        }
    }
}
