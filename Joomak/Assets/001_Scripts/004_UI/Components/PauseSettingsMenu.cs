using System.Collections.Generic;
using System.Linq;
using _001_Scripts._001_Manager;
using _001_Scripts._002_Controller;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public sealed class PauseSettingsMenu : MonoBehaviour
    {
        private const string MasterVolumeKey = "Audio.MasterVolume";
        private const string BgmVolumeKey = "Audio.BgmVolume";
        private const string SfxVolumeKey = "Audio.SfxVolume";
        private const string FullscreenKey = "Display.Fullscreen";
        private const string ResolutionWidthKey = "Display.ResolutionWidth";
        private const string ResolutionHeightKey = "Display.ResolutionHeight";

        [Header("Panels")]
        [SerializeField] private GameObject dimmedBackground;
        [SerializeField] private GameObject settingsPanel;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text masterVolumeValue;
        [SerializeField] private Text bgmVolumeValue;
        [SerializeField] private Text sfxVolumeValue;

        [Header("Display")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown resolutionDropdown;

        [Header("Actions")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private readonly List<Resolution> resolutions = new();
        private float previousTimeScale = 1f;
        private bool isOpen;

        public static bool IsPaused { get; private set; }

        private void Awake()
        {
            ConfigureSlider(masterVolumeSlider);
            ConfigureSlider(bgmVolumeSlider);
            ConfigureSlider(sfxVolumeSlider);

            masterVolumeSlider?.onValueChanged.AddListener(SetMasterVolume);
            bgmVolumeSlider?.onValueChanged.AddListener(SetBgmVolume);
            sfxVolumeSlider?.onValueChanged.AddListener(SetSfxVolume);
            fullscreenToggle?.onValueChanged.AddListener(SetFullscreen);
            resolutionDropdown?.onValueChanged.AddListener(SetResolution);
            resumeButton?.onClick.AddListener(Close);
            restartButton?.onClick.AddListener(RestartScene);
            quitButton?.onClick.AddListener(QuitGame);

            LoadSettings();
            BuildResolutionOptions();
            SetVisible(false);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (PlayerController.ShouldBlockPauseMenu)
                {
                    return;
                }

                Toggle();
            }
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                RestoreTimeScale();
            }
        }

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (isOpen)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
            IsPaused = true;
            SetVisible(true);
            resumeButton?.Select();
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            RestoreTimeScale();
            SetVisible(false);
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            isOpen = false;
            IsPaused = false;
        }

        private void SetVisible(bool visible)
        {
            dimmedBackground?.SetActive(visible);
            settingsPanel?.SetActive(visible);
        }

        private void LoadSettings()
        {
            float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float bgm = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

            masterVolumeSlider?.SetValueWithoutNotify(master);
            bgmVolumeSlider?.SetValueWithoutNotify(bgm);
            sfxVolumeSlider?.SetValueWithoutNotify(sfx);
            fullscreenToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1);

            ApplyAudio(master, bgm, sfx);
            RefreshVolumeLabels();
        }

        private void BuildResolutionOptions()
        {
            if (resolutionDropdown == null)
            {
                return;
            }

            resolutions.Clear();
            resolutions.AddRange(Screen.resolutions
                .GroupBy(resolution => (resolution.width, resolution.height))
                .Select(group => group.Last())
                .OrderBy(resolution => resolution.width)
                .ThenBy(resolution => resolution.height));

            if (resolutions.Count == 0)
            {
                resolutions.Add(Screen.currentResolution);
            }

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resolutions.Select(resolution => $"{resolution.width} x {resolution.height}").ToList());

            int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
            int selected = resolutions.FindIndex(resolution => resolution.width == savedWidth && resolution.height == savedHeight);
            if (selected < 0)
            {
                selected = resolutions.FindIndex(resolution => resolution.width == Screen.width && resolution.height == Screen.height);
            }

            resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, selected));
            resolutionDropdown.RefreshShownValue();
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private void SetMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
            AudioManager.Instance?.SetMasterVolume(value);
            RefreshVolumeLabels();
        }

        private void SetBgmVolume(float value)
        {
            PlayerPrefs.SetFloat(BgmVolumeKey, value);
            AudioManager.Instance?.SetBgmVolume(value);
            RefreshVolumeLabels();
        }

        private void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            AudioManager.Instance?.SetSfxVolume(value);
            RefreshVolumeLabels();
        }

        private static void ApplyAudio(float master, float bgm, float sfx)
        {
            AudioListener.volume = master;
            if (AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.SetMasterVolume(master);
            AudioManager.Instance.SetBgmVolume(bgm);
            AudioManager.Instance.SetSfxVolume(sfx);
        }

        private void RefreshVolumeLabels()
        {
            SetPercent(masterVolumeValue, masterVolumeSlider);
            SetPercent(bgmVolumeValue, bgmVolumeSlider);
            SetPercent(sfxVolumeValue, sfxVolumeSlider);
        }

        private static void SetPercent(Text label, Slider slider)
        {
            if (label != null && slider != null)
            {
                label.text = $"{Mathf.RoundToInt(slider.value * 100f)}%";
            }
        }

        private void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SetResolution(int index)
        {
            if (index < 0 || index >= resolutions.Count)
            {
                return;
            }

            Resolution resolution = resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, fullscreenToggle == null ? Screen.fullScreen : fullscreenToggle.isOn);
            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
            PlayerPrefs.Save();
        }

        private void RestartScene()
        {
            RestoreTimeScale();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void QuitGame()
        {
            PlayerPrefs.Save();
            RestoreTimeScale();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
