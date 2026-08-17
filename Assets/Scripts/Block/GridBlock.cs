using System;
using Data;
using PrimeTween;
using UnityEngine;

namespace Block
{
    public class GridBlock : MonoBehaviour
    {
        public BlockType Type        { get; private set; }
        public int       ColumnIndex { get; private set; }

        public int  Health     { get; private set; } = 1;
        public bool IsObstacle => Type == BlockType.Obstacle_Iron;

        public bool IsTargeted { get; set; }

        public event Action<GridBlock> OnExploded;

        public void Setup(BlockType type, int columnIndex)
        {
            Type        = type;
            ColumnIndex = columnIndex;

            if(Type == BlockType.Obstacle_Iron)
                Health = 3;
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
            Health -= damage;
            if(Health <= 0)
                Explode();
            else
            {
                Sequence.Create()
                        .Group(Tween.ShakeLocalPosition(transform, new Vector3(0.08f, 0.08f, 0f), duration: 0.15f, frequency: 25))
                        .Group(Tween.PunchScale(transform, new Vector3(-0.1f, -0.1f, 0f), duration: 0.15f));
            }
        }

        public void Explode()
        {
            OnExploded?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
