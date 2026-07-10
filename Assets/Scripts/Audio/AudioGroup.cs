using System;
using Audio.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    [Serializable]
    public class AudioGroup
    {
        [SerializeField] private AudioGroupType _groupType;
        [SerializeField] private AudioMixerGroup _audioMixerGroup;
        [SerializeField] private bool _isOneAudioSource;
        [SerializeField] private AudioInfo[] _audios;

        public AudioGroupType GroupType => _groupType;
        public AudioMixerGroup AudioMixerGroup => _audioMixerGroup;
        public bool IsOneAudioSource => _isOneAudioSource;

        public AudioInfo FindAudioInfo(string name)
        {
            AudioInfo audioInfo = _audios[0];
            foreach (AudioInfo audio in _audios)
            {
                if (name == audio.ID)
                {
                    return audio;
                }
            }

            Debug.LogError($"Audio with name: {name} not found. Returned first audio from array");
            return audioInfo;
        }
    }
}