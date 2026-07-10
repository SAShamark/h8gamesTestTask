namespace UI.Screens.Base
{
    public class BaseSubscreen : BaseWindow
    {
        public virtual void Show()
        {
            _canvas.enabled = true;
        }

        public virtual void Hide() => _canvas.enabled = false;
    }
}