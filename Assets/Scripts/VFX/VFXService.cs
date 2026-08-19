using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VFX
{
    public interface IVFXService
    {
        void PlayVFX(VFXType type, Vector3 position, Quaternion rotation = default);
    }

    public class VFXService : MonoBehaviour, IVFXService
    {
        [System.Serializable]
        public struct VFXData
        {
            public VFXType        type;
            public ParticleSystem prefab;
            public int            initialPoolSize;
        }

        [Header("VFX Configurations")]
        [SerializeField] private List<VFXData> vfxConfigurations;

        private readonly Dictionary<VFXType, Queue<ParticleSystem>> _pools   = new();
        private readonly Dictionary<VFXType, ParticleSystem>        _prefabs = new();

        private void Awake()
        {
            InitializePools();
        }

        private void InitializePools()
        {
            foreach(var config in vfxConfigurations)
            {
                if(config.prefab == null) continue;

                _prefabs[config.type] = config.prefab;
                _pools[config.type]   = new Queue<ParticleSystem>();

                for(int i = 0; i < config.initialPoolSize; i++)
                {
                    var instance = Instantiate(config.prefab, transform);
                    instance.gameObject.SetActive(false);
                    _pools[config.type].Enqueue(instance);
                }
            }
        }

        public void PlayVFX(VFXType type, Vector3 position, Quaternion rotation = default)
        {
            if(!_prefabs.ContainsKey(type)) return;

            var pool = _pools[type];

            var ps = pool.Count > 0 ? pool.Dequeue() : Instantiate(_prefabs[type], transform);

            ps.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            ps.gameObject.SetActive(true);
            ps.Play();

            ReturnToPoolAsync(type, ps).Forget();
        }

        private async UniTaskVoid ReturnToPoolAsync(VFXType type, ParticleSystem ps)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());

            ps.gameObject.SetActive(false);
            _pools[type].Enqueue(ps);
        }
    }
}
