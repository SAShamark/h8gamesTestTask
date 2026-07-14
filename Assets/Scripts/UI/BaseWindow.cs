using System.Threading.Tasks;
using Services;
using UI.Managers.Components;
using UnityEngine;

namespace UI
{
    public abstract class BaseWindow : MonoBehaviour
    {
        [SerializeField] protected Canvas _canvas;
        [SerializeField] protected CanvasGroup _canvasGroup;
        [SerializeField] protected SafeAreaFitter _safeAreaFitter;
        [SerializeField] protected Animator _animator;
        protected readonly int IsEnable = Animator.StringToHash("IsEnable");


        protected async Task HideAnimation(string clipName)
        {
            _animator.SetBool(IsEnable, false);
            int clipTime = Mathf.FloorToInt(ClipDataProvider.ClipDuration(_animator, clipName) *
                                            ValueConstants.MILLISECONDS_IN_SECOND);
            await Task.Delay(clipTime);

            try
            {
                await Task.Delay(clipTime);
            }
            catch (TaskCanceledException exception)
            {
                Debug.Log("Popup hide exception: " + exception);
            }
        }
    }
}