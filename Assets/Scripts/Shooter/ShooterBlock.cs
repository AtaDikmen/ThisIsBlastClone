using System;
using System.Threading;
using Block;
using Cysharp.Threading.Tasks;
using Data;
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
        [SerializeField] private TMP_Text _bulletLabel;

        public event Action<ShooterBlock> OnTapped;

        private Vector3 _defaultScale;
        private Vector3 _defaultLocalPos;

        private void Awake()
        {
            if(GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();

            _defaultScale    = transform.localScale;
            _defaultLocalPos = transform.localPosition;
        }

        public void Setup(BlockType type, int bulletCount)
        {
            Type        = type;
            BulletCount = bulletCount;
            IsInSlot    = false;
            IsFiring    = false;

            if(_bulletLabel == null)
                _bulletLabel = GetComponentInChildren<TMP_Text>();

            RefreshLabel();
        }

        public void SetFiringState(bool isFiring)
        {
            IsFiring = isFiring;
        }

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
                OnTapped?.Invoke(this);
        }

        private void OnMouseDown()
        {
            HandleClick();
        }

        public void FireProjectileAt(
            GridBlock                     target,
            GameObject                    projectilePrefab,
            Action<GameObject, BlockType> applyColorCallback,
            Action                        onComplete = null)
        {
            if(IsEmpty || target == null)
            {
                onComplete?.Invoke();
                return;
            }

            IsFiring = true;

            // Ateş etme anındaki "Juicy Cannon Shake / Recoil" animasyonunu paralel başlatıyoruz
            PlayFireRecoilAsync(destroyCancellationToken).Forget();

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
                    target.Explode();

                IsFiring = false;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Ateş etme anında küpün ezilip-esnemesini (Squash & Stretch) ve tepmesini (Recoil) sağlayan asenkron animasyon.
        /// </summary>
        private async UniTaskVoid PlayFireRecoilAsync(CancellationToken ct)
        {
            Vector3 baseScale = transform.localScale;
            Vector3 basePos   = transform.localPosition;

            // Phase 1: Squash & Recoil Down (Ezip geriye tepme - 0.04sn)
            Vector3 recoilScale = new Vector3(baseScale.x * 1.18f, baseScale.y * 0.82f, baseScale.z * 1.18f);
            Vector3 recoilPos   = basePos - new Vector3(0f, 0.04f, 0f);

            float elapsed   = 0f;
            float duration1 = 0.04f;

            while(elapsed < duration1)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = elapsed / duration1;

                transform.localScale    = Vector3.Lerp(baseScale, recoilScale, t);
                transform.localPosition = Vector3.Lerp(basePos, recoilPos, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // Phase 2: Stretch Up (Yaylanarak yukarı uzama - 0.06sn)
            Vector3 stretchScale = new Vector3(baseScale.x * 0.92f, baseScale.y * 1.12f, baseScale.z * 0.92f);
            elapsed = 0f;
            float duration2 = 0.06f;

            while(elapsed < duration2)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = elapsed / duration2;

                transform.localScale    = Vector3.Lerp(recoilScale, stretchScale, t);
                transform.localPosition = Vector3.Lerp(recoilPos, basePos, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // Phase 3: Elastic Settle (Eski formuna sönümlenerek oturma - 0.08sn)
            elapsed = 0f;
            float duration3 = 0.08f;

            while(elapsed < duration3)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t      = elapsed / duration3;
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.05f;

                transform.localScale = Vector3.Lerp(stretchScale, baseScale, t) + new Vector3(-bounce, bounce, -bounce);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            transform.localScale    = baseScale;
            transform.localPosition = basePos;
        }

        public async UniTask PlayMergeJuiceAsync()
        {
            Vector3 originalScale    = transform.localScale;
            Vector3 targetPunchScale = originalScale * 1.35f;

            float duration = 0.15f;
            float elapsed  = 0f;

            while(elapsed < duration)
            {
                elapsed              += Time.deltaTime;
                transform.localScale =  Vector3.Lerp(originalScale, targetPunchScale, elapsed / duration);
                await UniTask.Yield();
            }

            elapsed  = 0f;
            duration = 0.18f;
            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t      = elapsed / duration;
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.12f;
                transform.localScale = Vector3.Lerp(targetPunchScale, originalScale, t) + new Vector3(bounce, bounce, bounce);
                await UniTask.Yield();
            }

            transform.localScale = originalScale;
        }

        public void RefreshLabel()
        {
            if(_bulletLabel != null)
            {
                _bulletLabel.text = BulletCount.ToString();
            }
        }
    }
}
