using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Entities.Character
{
    [RequireComponent(typeof(Canvas))]
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundFill;
        [SerializeField] private Image _damageFill;
        [SerializeField] private Image _healthFill;
        [SerializeField] private GameObject _buffIcon;

        [SerializeField] private Color _backgroundColor = new(1f, 0f, 0f);
        [SerializeField] private Color _damageColor = Color.white;
        [SerializeField] private Color _healthColor = new(0.15f, 1f, 0.35f);

        [Header("Damage Feel")]
        [SerializeField] private float _healthFillDuration = 0.12f;
        [SerializeField] private float _damageDelay = 0.1f;
        [SerializeField] private float _damageFillDuration = 0.18f;
        [SerializeField] private float _hitPunchScale = 1.18f;
        [SerializeField] private float _hitPunchDuration = 0.18f;

        private Transform _cameraTransform;
        private Canvas _canvas;
        private Sequence _healthSequence;
        private Vector3 _healthFillDefaultScale;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            TryBindCamera();
            _healthFillDefaultScale = _healthFill.transform.localScale;

            _backgroundFill.color = _backgroundColor;
            _damageFill.color = _damageColor;
            _healthFill.color = _healthColor;
        }

        private bool TryBindCamera()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return false;
            }

            _cameraTransform = mainCamera.transform;
            _canvas.worldCamera = mainCamera;
            return true;
        }

        public void Init(float healthFill)
        {
            gameObject.SetActive(true);
            SetFillAmount(healthFill);
            _buffIcon.SetActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowBuff(bool isVisible)
        {
            _buffIcon.SetActive(isVisible);
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null && !TryBindCamera())
            {
                return;
            }

            transform.rotation = _cameraTransform.rotation;
        }

        private void OnDestroy()
        {
            _healthSequence?.Kill();
        }

        public void ShowDamage(float targetFill)
        {
            AnimateDamage(targetFill);
        }

        public void ShowHeal(float targetFill)
        {
            AnimateHeal(targetFill);
        }

        private void AnimateDamage(float targetFill)
        {
            _healthSequence?.Kill();
            _healthFill.transform.localScale = _healthFillDefaultScale;

            _healthSequence = DOTween.Sequence();
            _healthSequence.Insert(0f,
                _healthFill.DOFillAmount(targetFill, _healthFillDuration).SetEase(Ease.OutQuad));
            _healthSequence.Insert(0f, _healthFill.transform.DOScale(_healthFillDefaultScale * _hitPunchScale,
                _hitPunchDuration * 0.5f).SetEase(Ease.OutBack));
            _healthSequence.Insert(_hitPunchDuration * 0.5f, _healthFill.transform.DOScale(_healthFillDefaultScale,
                _hitPunchDuration * 0.5f).SetEase(Ease.OutQuad));
            _healthSequence.Insert(_damageDelay,
                _damageFill.DOFillAmount(targetFill, _damageFillDuration).SetEase(Ease.OutCubic));
            _healthSequence.SetLink(gameObject);
        }

        private void AnimateHeal(float targetFill)
        {
            _healthSequence?.Kill();
            _healthFill.transform.localScale = _healthFillDefaultScale;

            _healthSequence = DOTween.Sequence();
            _healthSequence.Join(_healthFill.DOFillAmount(targetFill, _healthFillDuration).SetEase(Ease.OutQuad));
            _healthSequence.Join(_damageFill.DOFillAmount(targetFill, _healthFillDuration).SetEase(Ease.OutQuad));
            _healthSequence.Join(_healthFill.transform.DOScale(_healthFillDefaultScale, _hitPunchDuration)
                .SetEase(Ease.OutQuad));
            _healthSequence.SetLink(gameObject);
        }

        private void SetFillAmount(float fillAmount)
        {
            _damageFill.fillAmount = fillAmount;
            _healthFill.fillAmount = fillAmount;
        }
    }
}
