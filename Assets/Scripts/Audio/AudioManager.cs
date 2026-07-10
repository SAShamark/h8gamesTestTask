using System;
using System.Collections.Generic;
using System.Linq;
using Audio.Data;
using DG.Tweening;
using Services;
using Services.Storage;
using UnityEngine;

namespace Audio
{
    public class AudioManager : MonoSingleton<AudioManager>
    {
        [SerializeField] private AudioConfig _audioConfig;
        [SerializeField] private AudioContainer _audioContainer;
        [SerializeField] private Audio3dContainer _3dAudioContainer;

        private Dictionary<AudioMixerGroups, float> _volumeSettings = new();
        private StorageService _storageService;

        private readonly Dictionary<AudioGroupType, AudioContainer> _audioGroupContainer = new();
        private Tween _volumeTween;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSoundVolumeChanged;

        private void Awake()
        {
            InitializeSingleton(false);
        }

        public void Start()
        {
            _storageService = ServicesManager.Instance.StorageService;
            _volumeSettings =
                _storageService.LoadData(StorageConstants.AUDIO_SETTINGS, new Dictionary<AudioMixerGroups, float>());

            foreach (AudioGroup audioGroup in _audioConfig.AudioGroups)
            {
                AudioContainer audioContainer = Instantiate(_audioContainer, transform);

                audioContainer.Initialize(audioGroup.GroupType, audioGroup.AudioMixerGroup,
                    audioGroup.IsOneAudioSource);
                _audioGroupContainer.Add(audioGroup.GroupType, audioContainer);
            }

            ChangeMasterVolume(GetVolume(AudioMixerGroups.Master));
            ChangeMusicVolume(GetVolume(AudioMixerGroups.Music));
            ChangeSoundVolume(GetVolume(AudioMixerGroups.Sounds));
        }

        public void Play(AudioGroupType audioGroupType, string name, Vector3 position = new())
        {
            AudioGroup audioGroup = _audioConfig.FindGroup(audioGroupType);
            AudioInfo audioInfo = audioGroup.FindAudioInfo(name);

            AudioSourceModel audioSourceModel = _audioGroupContainer[audioGroupType].GetAudioSourceModel(name);

            audioSourceModel.Source.clip = audioInfo.Clip;
            audioSourceModel.Source.volume = audioInfo.Volume;
            audioSourceModel.Source.loop = audioInfo.Loop;
            audioSourceModel.Source.mute = false;
            audioSourceModel.Source.Play();

            if (position != Vector3.zero)
            {
                audioSourceModel.transform.position = position;
            }
        }

        public void PlaySmooth(AudioGroupType audioGroupType, string name, Vector3 position = new(),
            float fadeDuration = 0.3f)
        {
            AudioGroup audioGroup = _audioConfig.FindGroup(audioGroupType);
            AudioInfo audioInfo = audioGroup.FindAudioInfo(name);

            AudioSourceModel audioSourceModel = _audioGroupContainer[audioGroupType].GetAudioSourceModel(name);
            AudioSource source = audioSourceModel.Source;

            if (source.isPlaying)
            {
                source.DOFade(0f, fadeDuration)
                    .OnComplete(() =>
                    {
                        source.Stop();
                        SetupAndPlay(source, audioInfo, position, fadeDuration);
                    });
            }
            else
            {
                SetupAndPlay(source, audioInfo, position, fadeDuration);
            }
        }

        private void SetupAndPlay(AudioSource source, AudioInfo audioInfo, Vector3 position, float fadeDuration)
        {
            source.clip = audioInfo.Clip;
            source.loop = audioInfo.Loop;
            source.mute = false;

            if (position != Vector3.zero)
                source.transform.position = position;

            source.volume = 0f;
            source.Play();
            source.DOFade(audioInfo.Volume, fadeDuration);
        }
        public void StopSmooth(AudioGroupType audioGroupType, string name, float fadeDuration = 0.5f)
        {
            ExecuteStop(audioGroupType, name, fadeDuration);
        }

        public void Stop(AudioGroupType audioGroupType, string name)
        {
            ExecuteStop(audioGroupType, name, 0f);
        }

        private void ExecuteStop(AudioGroupType audioGroupType, string name, float fadeDuration)
        {
            if (!_audioGroupContainer.TryGetValue(audioGroupType, out var container)) return;

            AudioGroup audioGroup = _audioConfig.FindGroup(audioGroupType);
            AudioInfo audioInfo = audioGroup?.FindAudioInfo(name);
    
            if (audioInfo == null || audioInfo.Clip == null) return;

            foreach (var audioSourceModel in container.AudioSources)
            {
                if (audioSourceModel == null || audioSourceModel.Source == null) continue;

                AudioSource source = audioSourceModel.Source;

                if (source.isPlaying && source.clip == audioInfo.Clip)
                {
                    source.DOKill();
                    source.loop = false;

                    if (fadeDuration <= 0f)
                    {
                        source.Stop();
                        source.clip = null;
                    }
                    else
                    {
                        source.DOFade(0f, fadeDuration).OnComplete(() =>
                        {
                            source.Stop();
                            source.clip = null;
                        });
                    }
                }
            }
        }
        public void ResumeSmooth(AudioGroupType audioGroupType, string name, float fadeDuration = 0.5f)
        {
            if (!_audioGroupContainer.ContainsKey(audioGroupType)) return;

            AudioGroup audioGroup = _audioConfig.FindGroup(audioGroupType);
            AudioInfo audioInfo = audioGroup.FindAudioInfo(name);
            AudioSource source = _audioGroupContainer[audioGroupType].GetAudioSourceModel(name).Source;

            source.UnPause();

            if (!source.isPlaying)
            {
                source.Play();
            }

            source.DOFade(audioInfo.Volume, fadeDuration);
        }

        public void MuteSwitcher(AudioGroupType audioGroupType, string name, bool isMute)
        {
            AudioSourceModel audioSourceModel = _audioGroupContainer[audioGroupType].GetAudioSourceModel(name);

            if (audioSourceModel.Source.clip == null)
            {
                AudioGroup audioGroup = _audioConfig.FindGroup(audioGroupType);
                AudioInfo audioInfo = audioGroup.FindAudioInfo(name);
                audioSourceModel.Source.clip = audioInfo.Clip;
                audioSourceModel.Source.volume = audioInfo.Volume;
                audioSourceModel.Source.loop = audioInfo.Loop;
                audioSourceModel.Source.mute = false;
                audioSourceModel.Source.Play();
            }

            audioSourceModel.Source.mute = isMute;
        }

        public void MuteAllEffectSounds()
        {
            var groupsToMute = new[] { AudioGroupType.EffectSounds };

            foreach (AudioGroupType group in groupsToMute)
            {
                foreach (AudioSourceModel audioSource in _audioGroupContainer[group].AudioSources
                             .Where(audioSource => audioSource.Source != null))
                {
                    audioSource.Source.mute = true;
                }
            }
        }

        public float GetVolume(AudioMixerGroups audioMixerGroup) =>
            _volumeSettings.GetValueOrDefault(audioMixerGroup, 1f);

        public void MasterSwitcher(bool state)
        {
            float volume = state ? 1 : 0;
            ChangeVolume(AudioMixerGroups.Master, volume);
            OnMasterVolumeChanged?.Invoke(volume);
        }

        public void MusicSwitcher(bool state)
        {
            float volume = state ? 1 : 0;
            ChangeVolume(AudioMixerGroups.Music, volume);
            OnMusicVolumeChanged?.Invoke(volume);
        }

        public void SoundSwitcher(bool state)
        {
            float volume = state ? 1 : 0;
            ChangeVolume(AudioMixerGroups.Sounds, volume);
            ChangeVolume(AudioMixerGroups.UI, volume);
            OnSoundVolumeChanged?.Invoke(volume);
        }

        public void ChangeMasterVolume(float volume)
        {
            ChangeVolume(AudioMixerGroups.Master, volume);
            OnMasterVolumeChanged?.Invoke(volume);
        }

        public void ChangeMusicVolume(float volume)
        {
            ChangeVolume(AudioMixerGroups.Music, volume);
            OnMusicVolumeChanged?.Invoke(volume);
        }

        public void ChangeSoundVolume(float volume)
        {
            ChangeVolume(AudioMixerGroups.Sounds, volume);
            ChangeVolume(AudioMixerGroups.UI, volume);
            OnSoundVolumeChanged?.Invoke(volume);
        }

        private void ChangeVolume(AudioMixerGroups type, float volume)
        {
            _audioConfig.AudioMixer.SetFloat(type.ToString(), SqrtToDecibel(volume));
            _volumeSettings[type] = volume;
        }

        private float SqrtToDecibel(float volume)
        {
            return Mathf.Lerp(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME, Mathf.Sqrt(volume));
        }

        public void SaveAudioSettings()
        {
            _storageService.SaveData(StorageConstants.AUDIO_SETTINGS, _volumeSettings);
        }
    }
}