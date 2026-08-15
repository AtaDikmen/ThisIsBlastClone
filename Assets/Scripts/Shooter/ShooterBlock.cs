using System;
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

        private void Awake()
        {
            if(GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();
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

        /// <summary>
        /// Mermi fırlatır ve atış/patlama tamamlandığında onComplete callback'ini tetikler.
        /// </summary>
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
        /// Triple Merge anında bloğun 1.35x büyüyüp yaylanarak geri esnemesini sağlayan Juicy Pop efekti.
        /// </summary>
        public async UniTask PlayMergeJuiceAsync()
        {
            Vector3 originalScale    = transform.localScale;
            Vector3 targetPunchScale = originalScale * 1.35f;

            float duration = 0.15f;
            float elapsed  = 0f;

            // 1. Hızlı Büyüme (Punch Out)
            while(elapsed < duration)
            {
                elapsed              += Time.deltaTime;
                transform.localScale =  Vector3.Lerp(originalScale, targetPunchScale, elapsed / duration);
                await UniTask.Yield();
            }

            // 2. Yaylanarak Eski Boyuta Dönüş (Elastic Ease Out)
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