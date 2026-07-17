using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 파티클 프리팹 + 위치를 넘기면 그 자리에서 재생해주는 창구.
    // 루프가 아닌 이펙트는 재생 시간이 끝나면 자동으로 파괴된다.
    public sealed class VFXManager : SinManagerBase<VFXManager>
    {
        public override void Initialize()
        {
        }

        public ParticleSystem RequestVfx(ParticleSystem effectPrefab, Vector3 position, Quaternion rotation = default)
        {
            if (effectPrefab == null)
            {
                return null;
            }

            ParticleSystem instance = Instantiate(effectPrefab, position, rotation);
            instance.Play();

            // 루프 이펙트는 언제 멈출지 호출한 쪽이 알아서 판단해야 하므로 자동 파괴하지 않는다.
            if (!instance.main.loop)
            {
                float lifetime = instance.main.duration + instance.main.startLifetime.constantMax;
                Destroy(instance.gameObject, lifetime);
            }

            return instance;
        }
    }
}
