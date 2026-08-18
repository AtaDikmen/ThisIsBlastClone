using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Services;
using UnityEngine;
using VContainer;

namespace Audio
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Configurations")]
        [SerializeField] private AudioLibrarySO audioLibrary;
        [SerializeField] private int sfxPoolSize = 16;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;

        private readonly List<AudioSource>            _sfxPool         = new List<AudioSource>();
        private readonly Dictionary<SoundType, float> _lastPlayedTimes = new Dictionary<SoundType, float>();

        private ISaveService _saveService;

        public bool IsSFXMuted   { get; private set; }
        public bool IsMusicMuted { get; private set; }

        [Inject]
        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
        }

        private void Awake()
        {
            InitializePool();
            if(audioLibrary != null)
                audioLibrary.Initialize();
        }

        private void InitializePool()
        {
            for(int i = 0; i < sfxPoolSize; i++)
            {
                var sourceObj = new GameObject($"SFX_Source_{i}");
                sourceObj.transform.SetParent(transform);

                var source = sourceObj.AddComponent<AudioSource>();
                source.playOnAwake  = false;
                source.spatialBlend = 0f;

                _sfxPool.Add(source);
            }

            if(musicSource == null)
            {
                var musicObj = new GameObject("Music_Source");
                musicObj.transform.SetParent(transform);

                musicSource              = musicObj.AddComponent<AudioSource>();
                musicSource.loop         = true;
                musicSource.playOnAwake  = false;
                musicSource.spatialBlend = 0f;
            }
        }

        public void PlaySFX(SoundType type, float pitchMultiplier = 1f)
        {
            if(IsSFXMuted || type == SoundType.None || audioLibrary == null) return;

            var config = audioLibrary.GetConfig(type);
            if(config == null || config.Clip == null) return;

            if(_lastPlayedTimes.TryGetValue(type, out float lastTime))
                if(Time.time - lastTime < config.Cooldown)
                    return;
            _lastPlayedTimes[type] = Time.time;

            var source = GetAvailableSFXSource();
            if(source == null) return;

            source.pitch  = config.Pitch * pitchMultiplier;
            source.volume = config.Volume;
            source.clip   = config.Clip;
            source.Play();
        }

        public void PlayMusic(SoundType type, bool loop = true)
        {
            if(type == SoundType.None || audioLibrary == null) return;

            var config = audioLibrary.GetConfig(type);
            if(config == null || config.Clip == null) return;

            musicSource.clip   = config.Clip;
            musicSource.volume = IsMusicMuted ? 0f : config.Volume;
            musicSource.pitch  = config.Pitch;
            musicSource.loop   = loop;
            musicSource.Play();
        }

        public async UniTask CrossFadeMusicAsync(SoundType newMusicType, float duration = 0.5f)
        {
            if(audioLibrary == null) return;

            var config = audioLibrary.GetConfig(newMusicType);
            if(config == null || config.Clip == null) return;

            float targetVolume = IsMusicMuted ? 0f : config.Volume;

            if(musicSource.isPlaying)
                await Tween.AudioVolume(musicSource, endValue: 0f, duration: duration * 0.5f).ToYieldInstruction();

            musicSource.clip  = config.Clip;
            musicSource.pitch = config.Pitch;
            musicSource.Play();

            await Tween.AudioVolume(musicSource, endValue: targetVolume, duration: duration * 0.5f).ToYieldInstruction();
        }

        public void SetSFXState(bool isMuted)
        {
            IsSFXMuted = isMuted;
        }

        public void SetMusicState(bool isMuted)
        {
            IsMusicMuted = isMuted;
            if(musicSource != null)
            {
                musicSource.mute = isMuted;
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            for(int i = 0; i < _sfxPool.Count; i++)
            {
                if(!_sfxPool[i].isPlaying)
                    return _sfxPool[i];
            }
            return null;
        }
    }
}
