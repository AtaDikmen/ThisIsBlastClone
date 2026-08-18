using System;
using Cysharp.Threading.Tasks;
using Data;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Block
{
    public class GridBlock : MonoBehaviour
    {
        public BlockType Type        { get; private set; }
        public int       ColumnIndex { get; private set; }
        public int       RowIndex    { get; private set; }

        public int  Health      { get; private set; } = 1;
        public bool IsTargeted  { get; set; }
        public bool IsExploding { get; private set; }

        public bool IsRainbow => Type == BlockType.Rainbow;
        public bool IsBomb    => Type == BlockType.Bomb;
        public bool IsArmored => Health > 1;

        [Header("Rainbow Settings")]
        [SerializeField] private float rainbowSpeed = 1f;

        [Header("UI References")]
        [SerializeField] private TMP_Text healthLabel;

        private                 Renderer              _renderer;
        private                 MaterialPropertyBlock _propBlock;
        private readonly static int                   BaseColorId = Shader.PropertyToID("_BaseColor");

        public event Action<GridBlock> OnExploded;

        private void Awake()
        {
            _renderer  = GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if(IsRainbow && !IsExploding)
                AnimateRainbowColor();
        }

        private void AnimateRainbowColor()
        {
            if(_renderer == null) return;

            float hue          = Mathf.Repeat(Time.time * rainbowSpeed, 1f);
            Color rainbowColor = Color.HSVToRGB(hue, 0.85f, 1f);

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, rainbowColor);
            _renderer.SetPropertyBlock(_propBlock);
        }

        public void Setup(BlockType type, int columnIndex, int rowIndex, int initialHealth = 1)
        {
            Type        = type;
            ColumnIndex = columnIndex;
            RowIndex    = rowIndex;
            Health      = initialHealth;
            IsExploding = false;

            UpdateHealthLabel();
        }

        public void SlideTo(Vector3 targetPos, float duration = 0.15f)
        {
            Tween.StopAll(transform);

            if(gameObject.activeInHierarchy)
                Tween.Position(transform, targetPos, duration, ease: Ease.OutQuad);
            else
                transform.position = targetPos;
        }

        public void TakeDamage(int damage = 1)
        {
            if(IsExploding) return;

            Health -= damage;
            UpdateHealthLabel();

            IsTargeted = false;

            if(Health <= 0)
                ExplodeAsync().Forget();
            else
            {
                Sequence.Create()
                        .Group(Tween.ShakeLocalPosition(transform, new Vector3(0.08f, 0.08f, 0f), duration: 0.12f, frequency: 25))
                        .Group(Tween.PunchScale(transform, new Vector3(-0.12f, -0.12f, 0f), duration: 0.12f));
            }
        }

        public async UniTask ExplodeAsync()
        {
            if(IsExploding) return;
            IsExploding = true;

            if(IsBomb)
            {
                await Tween.PunchScale(transform, new Vector3(0.35f, 0.35f, 0f), duration: 0.12f, frequency: 20).ToYieldInstruction();

                var gridManager = FindFirstObjectByType<GridManager>();
                if(gridManager != null)
                {
                    gridManager.ExplodeNeighborsAsync(ColumnIndex, RowIndex).Forget();
                }
            }

            await Tween.Scale(transform, Vector3.zero, duration: 0.08f, ease: Ease.InBack).ToYieldInstruction();

            OnExploded?.Invoke(this);
            Destroy(gameObject);
        }

        private void UpdateHealthLabel()
        {
            if(healthLabel == null) return;

            if(IsArmored)
            {
                healthLabel.gameObject.SetActive(true);
                healthLabel.text = Health.ToString();
            }
            else
                healthLabel.gameObject.SetActive(false);
        }
    }
}
