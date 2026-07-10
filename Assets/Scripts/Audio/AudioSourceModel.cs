using System;
using UnityEngine;

namespace Audio
{
    [Serializable]
    public class AudioSourceModel: MonoBehaviour
    {
        [SerializeField] private AudioSource _source;

        public AudioSource Source => _source;

        public bool IsPlaying => _source.isPlaying;
    }
}