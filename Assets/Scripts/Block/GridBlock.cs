using System;
using System.Collections;
using Data;
using UnityEngine;

namespace Block
{
    public class GridBlock : MonoBehaviour
    {
        public BlockType Type        { get; private set; }
        public int       ColumnIndex { get; private set; }

        public int  Health     { get; private set; } = 1;
        public bool IsObstacle => Type == BlockType.Obstacle_Iron;

        public event Action<GridBlock> OnExploded;

        private Coroutine _slideRoutine;

        public void Setup(BlockType type, int columnIndex)
        {
            Type        = type;
            ColumnIndex = columnIndex;

            // Demir engel ise 3 vuruşta patlasın (örnek)
            if(Type == BlockType.Obstacle_Iron)
                Health = 3;
        }

        public void SlideTo(Vector3 targetPos, float duration = 0.15f)
        {
            if(_slideRoutine != null)
                StopCoroutine(_slideRoutine);

            if(gameObject.activeInHierarchy)
            {
                _slideRoutine = StartCoroutine(SlideRoutine(targetPos, duration));
            }
            else
            {
                transform.position = targetPos;
            }
        }

        private IEnumerator SlideRoutine(Vector3 targetPos, float duration)
        {
            Vector3 startPos = transform.position;
            float   elapsed  = 0f;

            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t                  = t * t;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
            _slideRoutine      = null;
        }

        public void TakeDamage(int damage = 1)
        {
            Health -= damage;
            if(Health <= 0)
            {
                Explode();
            }
            else
            {
                transform.SetLocalPositionAndRotation(transform.position + UnityEngine.Random.insideUnitSphere * 0.03f, Quaternion.identity);
            }
        }

        public void Explode()
        {
            OnExploded?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
