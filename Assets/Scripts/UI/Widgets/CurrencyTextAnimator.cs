using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.Widgets
{
    public static class CurrencyTextAnimator
    {
        public static Tween AnimateNumber(TMP_Text text, int from, int to, float duration,
            Func<double, string> formatter, Action<int> onValueChanged = null)
        {
            if (text == null)
            {
                return null;
            }

            if (duration <= 0f || from == to)
            {
                SetTextValue(text, to, formatter, onValueChanged);
                return null;
            }

            int currentValue = from;
            return DOTween.To(() => currentValue, value =>
                {
                    currentValue = value;
                    SetTextValue(text, value, formatter, onValueChanged);
                }, to, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(text.gameObject);
        }

        public static Tween PlayFloatingDelta(TMP_Text sourceText, int delta, float distance = 90f,
            float duration = 0.85f, float fontSizeMultiplier = 1.35f)
        {
            if (sourceText == null || sourceText.transform.parent == null)
            {
                return null;
            }

            var textObject = new GameObject("CurrencyChangeText", typeof(RectTransform));
            var rectTransform = textObject.GetComponent<RectTransform>();
            Canvas canvas = sourceText.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : sourceText.transform.parent;
            rectTransform.SetParent(parent, false);

            var changeText = textObject.AddComponent<TextMeshProUGUI>();
            changeText.font = sourceText.font;
            changeText.fontSize = sourceText.fontSize * fontSizeMultiplier;
            changeText.fontStyle = sourceText.fontStyle;
            changeText.alignment = TextAlignmentOptions.Center;
            changeText.color = sourceText.color;
            changeText.raycastTarget = false;
            changeText.enableWordWrapping = false;
            changeText.overflowMode = TextOverflowModes.Overflow;
            changeText.text = delta > 0 ? $"+{delta}" : delta.ToString();

            RectTransform sourceRect = sourceText.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = sourceRect.pivot;
            rectTransform.sizeDelta = new Vector2(Mathf.Max(sourceRect.rect.width, changeText.fontSize * 4f),
                Mathf.Max(sourceRect.rect.height, changeText.fontSize * 1.5f));
            rectTransform.position = sourceRect.position;
            rectTransform.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + distance, duration)
                .SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => changeText.alpha, value => changeText.alpha = value, 0f, duration)
                .SetEase(Ease.InQuad));
            sequence.Join(rectTransform.DOScale(1.15f, duration).SetEase(Ease.OutQuad));
            sequence.SetLink(sourceText.gameObject);
            sequence.OnComplete(() => DestroyIfNeeded(textObject));
            sequence.OnKill(() => DestroyIfNeeded(textObject));

            return sequence;
        }

        private static void SetTextValue(TMP_Text text, int value, Func<double, string> formatter,
            Action<int> onValueChanged)
        {
            text.text = formatter != null ? formatter(value) : value.ToString();
            onValueChanged?.Invoke(value);
        }

        private static void DestroyIfNeeded(GameObject target)
        {
            if (target != null)
            {
                UnityEngine.Object.Destroy(target);
            }
        }
    }
}
