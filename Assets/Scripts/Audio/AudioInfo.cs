using System;
using UnityEngine;

namespace Audio
{
    [Serializable]
    public sealed record AudioInfo
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] private string _id;
        [Range(0, 1), SerializeField] private float _volume;
        [ SerializeField] private bool _loop;

        public AudioClip Clip => _clip;

        public string ID => _id;

        public float Volume => _volume;

        public bool Loop => _loop;

        public AudioInfo()
        {
            _id = string.Empty;
            _volume = 1f;
            _loop = false;
            _clip = null;
        }

        public AudioInfo(in AudioInfo forCopy)
        {
            _id = forCopy._id;
            _volume = forCopy._volume;
            _loop = forCopy._loop;
            _clip = forCopy._clip;
        }
    }
}