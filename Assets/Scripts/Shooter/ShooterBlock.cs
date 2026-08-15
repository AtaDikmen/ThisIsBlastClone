using System;
using Block;
using Data;
using TMPro;
using UnityEngine;

namespace Shooter
{
    public class ShooterBlock : MonoBehaviour
    {
        public BlockType Type { get; private set; }

        public int BulletCount { get; private set; }

        public bool IsEmpty => BulletCount <= 0;

        public bool IsInSlot { get; set; }

        public bool IsFiring { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text _bulletLabel;

        public event Action<ShooterBlock> OnTapped;

        public event Action<ShooterBlock> OnEmpty;

        public event Action<ShooterBlock> OnFired;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if(col == null)
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

        public void HandleClick()
        {
            if(!IsInSlot && !IsFiring)
                OnTapped?.Invoke(this);
        }

        private void OnMouseDown()
        {
            HandleClick();
        }

        public void FireProjectileAt(GridBlock target, GameObject projectilePrefab, Action<GameObject, BlockType> applyColorCallback)
        {
            if(IsEmpty || target == null) return;

            IsFiring = true;

            GameObject bulletObj;
            if(projectilePrefab != null)
                bulletObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
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
                BulletCount--;
                RefreshLabel();

                if(target != null)
                    target.Explode();

                IsFiring = false;
                OnFired?.Invoke(this);

                if(IsEmpty)
                {
                    OnEmpty?.Invoke(this);
                    Destroy(gameObject);
                }
            });
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
