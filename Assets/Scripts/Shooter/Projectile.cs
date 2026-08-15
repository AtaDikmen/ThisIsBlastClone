using System;
using Block;
using UnityEngine;

namespace Shooter
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 15f;

        private GridBlock _targetBlock;
        private Vector3   _targetPosition;
        private Action    _onHitCallback;
        private bool      _isFlying = false;

        public void Launch(GridBlock target, Action onHit)
        {
            _targetBlock    = target;
            _targetPosition = target != null ? target.transform.position : transform.position;
            _onHitCallback  = onHit;
            _isFlying       = true;
        }

        private void Update()
        {
            if(!_isFlying) return;

            if(_targetBlock != null)
            {
                _targetPosition = _targetBlock.transform.position;
            }

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime
            );

            if(Vector3.Distance(transform.position, _targetPosition) <= 0.05f)
            {
                _isFlying = false;
                _onHitCallback?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
