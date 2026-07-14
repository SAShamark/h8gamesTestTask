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

        private Material[] _materials;
        private Color[] _defaultColors;
        private int[] _colorPropertyIds;
        private ParticleSystem _hitParticleSystem;
        private Tween _flashTween;
        private float _flashAmount;

        private void Awake()
        {
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
        }

        private void SetFlashAmount(float amount)
        {
            _flashAmount = amount;

            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i].SetColor(
                    _colorPropertyIds[i],
                    Color.Lerp(_defaultColors[i], _hitColor, amount));
            }
        }

        private void OnDestroy()
        {
            _flashTween?.Kill();

            for (int i = 0; i < _materials.Length; i++)
            {
                Destroy(_materials[i]);
            }
        }
    }
}
