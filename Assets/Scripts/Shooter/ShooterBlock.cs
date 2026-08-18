using System;
using Audio;
using Block;
using Cysharp.Threading.Tasks;
using Data;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Shooter
{
    public class ShooterBlock : MonoBehaviour
    {
        public BlockType Type        { get; private set; }
        public int       BulletCount { get; private set; }
        public bool      IsEmpty     => BulletCount <= 0;
        public bool      IsInSlot    { get; set; }
        public bool      IsFiring    { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text bulletLabel;

        [Header("Hop Settings")]
        [SerializeField] private float hopHeight = 0.50f;
        [SerializeField] private int   totalHops          = 4;
        [SerializeField] private float escapeDistanceX    = -7.5f;
        [SerializeField] private float singleHopDuration  = 0.20f;
        [SerializeField] private float impactSquashAmount = 0.30f;

        private Vector3       _defaultScale = Vector3.one * 0.25f;
        private Quaternion    _defaultRotation;
        private IAudioService _audioService;

        private Sequence _activeRecoilSequence;
        private Sequence _activeMergeSequence;
        private Sequence _activeRunAwaySequence;

        public event Action<ShooterBlock> OnTapped;

        private void Awake()
        {
            if(GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();

            _defaultScale    = transform.localScale;
            _defaultRotation = transform.localRotation;
        }

        private void OnMouseDown()
        {
            HandleClick();
        }

        private void OnDestroy()
        {
            if(_activeRecoilSequence.isAlive) _activeRecoilSequence.Stop();
            if(_activeMergeSequence.isAlive) _activeMergeSequence.Stop();
            if(_activeRunAwaySequence.isAlive) _activeRunAwaySequence.Stop();
        }

        public void Setup(BlockType type, int bulletCount, IAudioService audioService = null)
        {
            Type          = type;
            BulletCount   = bulletCount;
            IsInSlot      = false;
            IsFiring      = false;
            _audioService = audioService;

            if(bulletLabel == null)
                bulletLabel = GetComponentInChildren<TMP_Text>();

            RefreshLabel();
        }

        public void SetFiringState(bool isFiring) => IsFiring = isFiring;

        public void DecreaseBulletCount()
        {
            BulletCount--;
            RefreshLabel();
        }

        public void SetBulletCount(int newAmount)
        {
            BulletCount = newAmount;
            RefreshLabel();
        }

        public void HandleClick()
        {
            if(!IsInSlot && !IsFiring)
            {
                _audioService?.PlaySFX(SoundType.ShooterTap);
                OnTapped?.Invoke(this);
            }
        }

        public void FireProjectileAt(GridBlock target, GameObject projectilePrefab, Action<GameObject, BlockType> applyColorCallback, Action onComplete = null)
        {
            if(IsEmpty || target == null)
            {
                onComplete?.Invoke();
                return;
            }

            IsFiring = true;
            _audioService?.PlaySFX(SoundType.ShooterFire);
            PlayFireRecoil(target.transform.position).Forget();

            GameObject bulletObj;
            if(projectilePrefab != null)
            {
                bulletObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                bulletObj                      = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulletObj.transform.position   = transform.position;
                bulletObj.transform.localScale = Vector3.one * 0.12f;

                var sphereCol = bulletObj.GetComponent<Collider>();
                if(sphereCol != null) Destroy(sphereCol);
            }

            bulletObj.name = $"Bullet_{Type}";
            applyColorCallback?.Invoke(bulletObj, Type);

            var proj = bulletObj.GetComponent<Projectile>();
            if(proj == null)
                proj = bulletObj.AddComponent<Projectile>();

            proj.Launch(target, () =>
            {
                if(target != null)
                    target.TakeDamage();

                IsFiring = false;
                onComplete?.Invoke();
            });
        }

        private async UniTaskVoid PlayFireRecoil(Vector3 targetWorldPosition)
        {
            if(_activeRecoilSequence.isAlive)
                _activeRecoilSequence.Stop();

            transform.localScale = _defaultScale;

            Vector3 direction                       = (targetWorldPosition - transform.position).normalized;
            if(direction == Vector3.zero) direction = Vector3.forward;

            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);

            Vector3 recoilScale       = new Vector3(_defaultScale.x * 1.22f, _defaultScale.y * 0.78f, _defaultScale.z * 1.22f);
            Vector3 stretchScale      = new Vector3(_defaultScale.x * 0.90f, _defaultScale.y * 1.12f, _defaultScale.z * 0.90f);
            Vector3 recoilPunchVector = -direction * 0.12f;

            var seq = Sequence.Create();

            if(Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                seq = seq.Group(Tween.Rotation(transform, endValue: targetRotation, duration: 0.05f, ease: Ease.OutQuad));

            seq = seq.Group(Tween.Scale(transform, recoilScale, duration: 0.04f, ease: Ease.OutQuad))
                     .Group(Tween.PunchLocalPosition(transform, recoilPunchVector, duration: 0.10f, frequency: 1))
                     .Chain(Tween.Scale(transform, stretchScale, duration: 0.05f, ease: Ease.OutQuad))
                     .Chain(Tween.Scale(transform, _defaultScale, duration: 0.08f, ease: Ease.OutElastic));

            if(Quaternion.Angle(targetRotation, _defaultRotation) > 0.1f)
                seq = seq.Chain(Tween.Rotation(transform, endValue: _defaultRotation, duration: 0.15f, ease: Ease.OutQuad));

            _activeRecoilSequence = seq;
            await _activeRecoilSequence.ToYieldInstruction();
        }

        public async UniTask PlayMergeJuiceAsync()
        {
            if(_activeMergeSequence.isAlive) _activeMergeSequence.Stop();

            _audioService?.PlaySFX(SoundType.ShooterMerge);

            Vector3 anticipationScale = _defaultScale * 0.88f;
            Vector3 peakScale         = _defaultScale * 1.32f;

            _activeMergeSequence = Sequence.Create()
                                           .Chain(Tween.Scale(transform, anticipationScale, duration: 0.10f, ease: Ease.OutSine))
                                           .Chain(Tween.Scale(transform, peakScale, duration: 0.22f, ease: Ease.OutCubic))
                                           .Chain(Tween.Scale(transform, _defaultScale, duration: 0.20f, ease: Ease.OutQuad));

            await _activeMergeSequence.ToYieldInstruction();
        }

        public async UniTask PlayRunAwayAndDestroyAsync()
        {
            if(_activeRecoilSequence.isAlive) _activeRecoilSequence.Stop();
            if(_activeMergeSequence.isAlive) _activeMergeSequence.Stop();

            _audioService?.PlaySFX(SoundType.ShooterRunAway);

            transform.SetParent(null);

            Vector3 startPos           = transform.position;
            Vector3 clearSlotHeightPos = startPos + new Vector3(0f, 0.70f, 0f);

            _activeRunAwaySequence = Sequence.Create()
                                             .Group(Tween.Rotation(transform, endValue: _defaultRotation, duration: 0.10f, ease: Ease.OutQuad))
                                             .Group(Tween.Position(transform, clearSlotHeightPos, duration: 0.14f, ease: Ease.OutQuad))
                                             .Group(Tween.PunchScale(transform, new Vector3(-0.20f, 0.25f, 0f), duration: 0.14f, frequency: 1));

            float stepDistanceX = escapeDistanceX / totalHops;

            for(int i = 0; i < totalHops; i++)
            {
                Vector3 currentHopStart = clearSlotHeightPos + new Vector3(stepDistanceX * i, 0f, 0f);
                Vector3 currentHopEnd   = clearSlotHeightPos + new Vector3(stepDistanceX * (i + 1), 0f, 0f);

                _activeRunAwaySequence = _activeRunAwaySequence.Chain(
                    Tween.Custom(0f, 1f, duration: singleHopDuration, ease: Ease.Linear, onValueChange: t =>
                    {
                        if(transform == null) return;

                        float currentX = Mathf.Lerp(currentHopStart.x, currentHopEnd.x, t);
                        float hopY     = Mathf.Sin(t * Mathf.PI) * hopHeight;
                        float currentY = currentHopStart.y + hopY;

                        transform.position = new Vector3(currentX, currentY, startPos.z);

                        float scaleYFactor = 1f + (Mathf.Sin(t * Mathf.PI) * impactSquashAmount);
                        float scaleXFactor = 1f - (Mathf.Sin(t * Mathf.PI) * (impactSquashAmount * 0.5f));

                        transform.localScale = new Vector3(
                            _defaultScale.x * scaleXFactor,
                            _defaultScale.y * scaleYFactor,
                            _defaultScale.z * scaleXFactor
                        );
                    })
                );
            }

            _activeRunAwaySequence = _activeRunAwaySequence.Chain(Tween.Scale(transform, Vector3.zero, duration: 0.06f, ease: Ease.InCubic));

            await _activeRunAwaySequence.ToYieldInstruction();

            if(this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        public void RefreshLabel()
        {
            if(bulletLabel != null)
                bulletLabel.text = BulletCount.ToString();
        }
    }
}
