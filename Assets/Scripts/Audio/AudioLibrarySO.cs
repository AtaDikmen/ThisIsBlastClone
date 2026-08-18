using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    [Serializable]
    public class SoundConfig
    {
        public SoundType Type;
        public AudioClip Clip;

        [Range(0f, 1f)]
        public float Volume = 1f;

        [Range(0.5f, 2f)]
        public float Pitch = 1f;

        public float Cooldown = 0.05f;
    }

    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "SO/Audio Library")]
    public class AudioLibrarySO : ScriptableObject
    {
        [SerializeField] private List<SoundConfig> soundConfigs = new List<SoundConfig>();

        private Dictionary<SoundType, SoundConfig> _configLookup;

        public void Initialize()
        {
            _configLookup = new Dictionary<SoundType, SoundConfig>();
            if(soundConfigs == null) return;

            foreach(var config in soundConfigs)
            {
                if(config != null && config.Clip != null)
                    _configLookup.TryAdd(config.Type, config);
            }
        }

        public SoundConfig GetConfig(SoundType type)
        {
            if(_configLookup == null) Initialize();

            return _configLookup.GetValueOrDefault(type);
        }
    }
}
