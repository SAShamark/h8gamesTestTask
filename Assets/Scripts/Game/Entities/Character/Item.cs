using UnityEngine;

namespace Game.Entities.Character
{
    public class Item : MonoBehaviour
    {
        private Collider _collider;
        private bool _isCollected;

        public Transform Transform => transform;
        public bool IsCollected => _isCollected;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void MarkCollected()
        {
            _isCollected = true;
            _collider.enabled = false;
        }
    }
}
