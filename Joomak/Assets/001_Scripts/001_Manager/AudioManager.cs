using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // BGM 하나 + 겹쳐 재생 가능한 SFX 풀. 볼륨은 기기별 설정이라 PlayerPrefs에 저장한다.
    public sealed class AudioManager : SinManagerBase<AudioManager>
    {
        private const string MasterVolumeKey = "Audio.MasterVolume";
        private const string BgmVolumeKey = "Audio.BgmVolume";
        private const string SfxVolumeKey = "Audio.SfxVolume";

        [SerializeField] private AudioSource bgmSource;
        [SerializeField, Min(1)] private int sfxPoolSize = 8;

        private AudioSource[] sfxPool;
        private int nextSfxIndex;
        private float masterVolume = 1f;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;

        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;

        public override void Initialize()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPool.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool[i] = source;
            }

            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            ApplyBgmVolume();
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null || bgmSource == null || bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            ApplyBgmVolume();
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxPool == null || sfxPool.Length == 0)
            {
                return;
            }

            AudioSource source = sfxPool[nextSfxIndex];
            nextSfxIndex = (nextSfxIndex + 1) % sfxPool.Length;

            source.volume = Mathf.Clamp01(masterVolume * sfxVolume * volumeScale);
            source.PlayOneShot(clip);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            ApplyBgmVolume();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
            ApplyBgmVolume();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        }

        private void ApplyBgmVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = Mathf.Clamp01(masterVolume * bgmVolume);
            }
        }
    }
}
