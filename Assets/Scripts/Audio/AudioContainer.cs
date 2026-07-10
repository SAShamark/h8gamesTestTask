using System.Collections.Generic;
using Audio.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    public class AudioContainer : MonoBehaviour
    {
        [SerializeField] private AudioSourceModel _audioObject;

        private AudioGroupType _audioGroup;
        private AudioMixerGroup _audioMixerGroup;
        private bool _isOneAudioSource;


        public List<AudioSourceModel> AudioSources { get; private set; } = new();

        public void Initialize(AudioGroupType audioGroup, AudioMixerGroup audioMixerGroup, bool isOneAudioSource)
        {
            _audioGroup = audioGroup;
            _audioMixerGroup = audioMixerGroup;
            _isOneAudioSource = isOneAudioSource;
            name = _audioGroup.ToString();
            Create();
        }

        public AudioSourceModel GetAudioSourceModel(string name = "")
        {
            if (_isOneAudioSource)
            {
                return AudioSources[0];
            }

            if (AudioSources == null)
            {
                return Create();
            }

            if (!string.IsNullOrEmpty(name))
            {
                foreach (var audioSource in AudioSources)
                {
                    if (audioSource.Source.clip != null && audioSource.Source.clip.name == name)
                    {
                        return audioSource;
                    }
                }
            }

            return GetFirstAvailableAudioSource();
        }

        private AudioSourceModel GetFirstAvailableAudioSource()
        {
            foreach (var audioSource in AudioSources)
            {
                if (!audioSource.IsPlaying)
                {
                    return audioSource;
                }
            }

            return Create();
        }
        protected virtual AudioSourceModel Create()
        {
            AudioSourceModel audioSourceModel = Instantiate(_audioObject, transform);
            audioSourceModel.name = $"AudioSource {AudioSources.Count}";
            audioSourceModel.Source.outputAudioMixerGroup = _audioMixerGroup;

            AudioSources.Add(audioSourceModel);
            return audioSourceModel;
        }
    }
}