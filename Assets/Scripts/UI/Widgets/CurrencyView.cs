using System.Linq;
using DG.Tweening;
using Services;
using Services.Currency;
using TMPro;
using UI.Managers;
using UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Widgets
{
    public class CurrencyView : MonoBehaviour
    {
        [SerializeField] private CurrencyType _type;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _image;
        [SerializeField] private float _currencyTweenDuration = 0.35f;
        [SerializeField] private float _floatingDeltaDistance = 90f;
        [SerializeField] private Vector3 _spendPulseStrength = new(0.18f, 0.18f, 0f);
        [SerializeField] private float _spendPulseDuration = 0.25f;

        private CurrencyService _currencyService;

        private UIManager _uiManager;
        private int _lastCurrencyValue;
        private int _displayedCurrencyValue;
        private Tween _currencyTween;
        private Tween _spendPulseTween;
        private Vector3 _textDefaultScale;
        protected IBank bank;
        protected CurrencyCollection CurrencyCollection => _currencyService.CurrencyCollection;

        protected virtual void Start()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(ShowShopScreen);
            }

            _currencyService = ServicesManager.Instance.CurrencyService;
            bank = _currencyService.GetCurrencyByType(_type);
            _uiManager = UIManager.Instance;
            _textDefaultScale = _text.transform.localScale;
            bank.OnCurrencyChanged += SetCurrencyText;
            _lastCurrencyValue = bank.Currency;
            _displayedCurrencyValue = bank.Currency;
            SetCurrencyText(bank.Currency);
            var data = _currencyService.CurrencyCollection.CurrencySprites.FirstOrDefault(item => item.Type == _type);
            _image.sprite = data?.Value;
        }

        private void ShowShopScreen()
        {
            _uiManager.ScreensManager.ShowScreen(ScreenTypes.Shop);
        }

        protected virtual void OnDestroy()
        {
            _button?.onClick.RemoveListener(ShowShopScreen);

            if (bank != null)
            {
                bank.OnCurrencyChanged -= SetCurrencyText;
            }

            _currencyTween?.Kill();
            _spendPulseTween?.Kill();
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
