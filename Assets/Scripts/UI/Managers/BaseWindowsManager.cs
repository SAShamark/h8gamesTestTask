using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UI.Managers
{
    [Serializable]
    public abstract class BaseWindowsManager<T> where T : BaseWindow
    {
        [SerializeField] protected Transform _container;
        
        protected WindowsConfig WindowConfig;
        
        public T ActiveWindow { get; protected set; }
        public virtual void Initialize()
        {
        }

        public virtual void OnConfigLoaded(WindowsConfig windowConfig)
        {
            WindowConfig = windowConfig;
        }

        protected void RemoveScreen(T window)
        {
            Object.Destroy(window.gameObject);
        }
    }
}