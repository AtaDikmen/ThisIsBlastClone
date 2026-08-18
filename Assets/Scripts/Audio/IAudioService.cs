using Cysharp.Threading.Tasks;

namespace Audio
{
    public interface IAudioService
    {
        void    PlaySFX(SoundType             type,         float pitchMultiplier = 1f);
        void    PlayMusic(SoundType           type,         bool  loop            = true);
        UniTask CrossFadeMusicAsync(SoundType newMusicType, float duration        = 0.5f);

        void SetSFXState(bool   isMuted);
        void SetMusicState(bool isMuted);

        bool IsSFXMuted   { get; }
        bool IsMusicMuted { get; }
    }
}
