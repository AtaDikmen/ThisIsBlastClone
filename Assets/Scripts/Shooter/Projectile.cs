using System;
using Block;
using PrimeTween;
using UnityEngine;

namespace Shooter
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float flightDuration = 0.11f;
        [SerializeField] private Ease  easeType       = Ease.Linear;

        private Tween _activeTween;

        public void Launch(GridBlock target, Action onHit)
        {
            if(target == null)
            {
                onHit?.Invoke();
                Destroy(gameObject);
                return;
            }

            var targetPosition = target.transform.position;

            _activeTween = Tween.Position(transform, endValue: targetPosition, duration: flightDuration, ease: easeType)
                                .OnComplete(() =>
                                {
                                    onHit?.Invoke();
                                    Destroy(gameObject);
                                });
        }

        private void OnDestroy()
        {
            if(_activeTween.isAlive)
                _activeTween.Stop();
        }
    }
}
