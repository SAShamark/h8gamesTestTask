using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Character
{
    public class ProjectileHitFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _hitEffect;
        [SerializeField, ColorUsage(true, true)] private Color _hitColor = new(2f, 0.18f, 0f, 1f);
        [SerializeField] private float _flashInDuration = 0.05f;
        [SerializeField] private float _flashOutDuration = 0.14f;
        [SerializeField] private float _pulseScaleMultiplier = 1.04f;
        [SerializeField] private float _pulseInDuration = 0.05f;
        [SerializeField] private float _pulseOutDuration = 0.08f;
        [SerializeField, ColorUsage(true, true)] private Color _buffColor = new(1.15f, 1.05f, 0.35f, 1f);
        [SerializeField] private float _buffColorDuration = 0.18f;

        private Material[] _materials;
        private Color[] _defaultColors;
        private int[] _colorPropertyIds;
        private ParticleSystem _hitParticleSystem;
        private Tween _flashTween;
        private Tween _pulseTween;
        private Tween _buffTween;
        private float _flashAmount;
        private float _buffAmount;
        private Vector3 _defaultVisualScale;

        private void Awake()
        {
            _defaultVisualScale = _visualRoot.localScale;

            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            List<Material> materials = new();

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] rendererMaterials = renderers[rendererIndex].materials;

                for (int materialIndex = 0; materialIndex < rendererMaterials.Length; materialIndex++)
                {
                    materials.Add(rendererMaterials[materialIndex]);
                }
            }

            _materials = materials.ToArray();
            _defaultColors = new Color[_materials.Length];
            _colorPropertyIds = new int[_materials.Length];

            for (int i = 0; i < _materials.Length; i++)
            {
                _colorPropertyIds[i] = _materials[i].HasProperty(BaseColorId) ? BaseColorId : ColorId;
                _defaultColors[i] = _materials[i].GetColor(_colorPropertyIds[i]);
            }

            _hitParticleSystem = _hitEffect.GetComponent<ParticleSystem>();
            _hitParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Play(Vector3 hitPosition)
        {
            _hitEffect.position = hitPosition;
            _hitParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _hitParticleSystem.Play(true);

            _flashTween?.Kill();
            _flashTween = DOTween.Sequence()
                .Append(DOTween.To(() => _flashAmount, SetFlashAmount, 1f, _flashInDuration)
                    .SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _flashAmount, SetFlashAmount, 0f, _flashOutDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() => _flashTween = null)
                .SetLink(gameObject);

            _pulseTween?.Kill();
            _pulseTween = DOTween.Sequence()
                .Append(_visualRoot.DOScale(
                        _defaultVisualScale * _pulseScaleMultiplier,
                        _pulseInDuration)
                    .SetEase(Ease.OutQuad))
                .Append(_visualRoot.DOScale(_defaultVisualScale, _pulseOutDuration)
                    .SetEase(Ease.InOutQuad))
                .OnComplete(() => _pulseTween = null)
                .SetLink(gameObject);
        }

        public void SetBuffed(bool isBuffed)
        {
            _buffTween?.Kill();
            _buffTween = DOTween.To(
                    () => _buffAmount,
                    SetBuffAmount,
                    isBuffed ? 1f : 0f,
                    _buffColorDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _buffTween = null)
                .SetLink(gameObject);
        }

        private void SetFlashAmount(float amount)
        {
            _flashAmount = amount;
            ApplyColors();
        }

        private void SetBuffAmount(float amount)
        {
            _buffAmount = amount;
            ApplyColors();
        }

        private void ApplyColors()
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                Color buffedColor = _defaultColors[i] * _buffColor;
                buffedColor.a = _defaultColors[i].a;
                Color baseColor = Color.Lerp(_defaultColors[i], buffedColor, _buffAmount);

                _materials[i].SetColor(
                    _colorPropertyIds[i],
                    Color.Lerp(baseColor, _hitColor, _flashAmount));
            }
        }

        private void OnDestroy()
        {
            _flashTween?.Kill();
            _pulseTween?.Kill();
            _buffTween?.Kill();

            for (int i = 0; i < _materials.Length; i++)
            {
                Destroy(_materials[i]);
            }
        }
    }
}
