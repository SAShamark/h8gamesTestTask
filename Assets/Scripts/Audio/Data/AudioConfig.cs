using UnityEngine;
using UnityEngine.Audio;

namespace Audio.Data
{
    [CreateAssetMenu(fileName = nameof(AudioConfig), menuName = "ScriptableObjects/" + nameof(AudioConfig))]
    public sealed class AudioConfig : ScriptableObject
    {
        [SerializeField] private AudioMixer _audioMixer;
        [Space, SerializeField] private AudioGroup[] _audioGroups;

        public AudioMixer AudioMixer => _audioMixer;
        public AudioGroup[] AudioGroups => _audioGroups;

        public AudioGroup FindGroup(AudioGroupType groupType)
        {
            AudioGroup group = _audioGroups[0];
            foreach (AudioGroup audioGroup in _audioGroups)
            {
                if (groupType == audioGroup.GroupType)
                {
                    return audioGroup;
                }
            }

            Debug.LogError($"Group with type: {groupType.ToString()} not found. Returned first group from array");
            return group;
        }
    }
}