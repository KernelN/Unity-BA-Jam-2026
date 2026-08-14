/////////////////////////////////////////////////////////////////////////////////
//
//	HUDManager.cs
//
//	Description:	responsible for the menu and additional hud elements.             
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VSController
{
    public class HUDManager : MonoBehaviour
    {
        [Foldout("UI Elements")]
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI fpsCounter;
        [SerializeField] private Text inputText;
        [SerializeField] private GameObject speedCounter;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button backButton;

        [Foldout("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip hoverSound;

        [Foldout("Toggles & Sliders")]
        [SerializeField] private Toggle speedToggle;
        [SerializeField] private Toggle fpsToggle;
        [SerializeField] private Slider volumeSlider;

        private bool useMobileControl;
        private bool isPaused = false;
        private bool showFPSCounter = false;
        private float deltaTime = 0.0f;

        private void Start()
        {
            versionText.text = "ver. " + Application.version;

            InitializeFPSCounter();
            InitializeSpeedCounter();
            InitializeVolume();
            InitializePauseButton();
            InitializeBackButton();
            SetMobileControl(useMobileControl);
        }

        private void Update()
        {
            // Toggles the menu display status
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            // Responsible for displaying FPS in real time
            if (showFPSCounter && fpsCounter != null)
            {
                UpdateFPS();
                DisplayFPS();
            }
        }

        // Enable menu
        private void InitializePauseButton()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(TogglePause);
            }
        }

        // Disable menu
        private void InitializeBackButton()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(ResumeGame);
            }
        }

        private void InitializeFPSCounter()
        {
            showFPSCounter = PlayerPrefs.GetInt("ShowFPSCounter", 0) == 1;

            if (fpsCounter != null)
                fpsCounter.gameObject.SetActive(showFPSCounter);

            if (fpsToggle != null)
            {
                fpsToggle.isOn = showFPSCounter;
                fpsToggle.onValueChanged.AddListener(SetFPSCounterState);
            }
        }

        private void InitializeSpeedCounter()
        {
            bool showSpeed = PlayerPrefs.GetInt("ShowSpeedCounter", 0) == 1;

            if (speedCounter != null)
                speedCounter.SetActive(showSpeed);

            if (speedToggle != null)
            {
                speedToggle.isOn = showSpeed;
                speedToggle.onValueChanged.AddListener(SetSpeedCounterState);
            }
        }

        private void InitializeVolume()
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = volume;

            if (volumeSlider != null)
            {
                volumeSlider.value = volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
        }

        public void TogglePause()
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        // Parameters when stoping the game
        private void PauseGame()
        {
            isPaused = true;
            pauseMenu.SetActive(true);
            Time.timeScale = 0f; 
            PlayHoverSound();

            if (!useMobileControl)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // Parameters when restoring the game
        private void ResumeGame()
        {
            isPaused = false;
            pauseMenu.SetActive(false);
            Time.timeScale = 1f;
            PlayClickSound();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Update fps all time
        private void UpdateFPS()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        // Display on ui
        private void DisplayFPS()
        {
            float fps = 1.0f / deltaTime;
            fpsCounter.text = $"FPS: {fps:0}";
        }

        // On mobile control disable text help input
        public void SetMobileControl(bool value)
        {
            useMobileControl = value;

            if (inputText != null)
                inputText.gameObject.SetActive(!useMobileControl);
        }

        // Save selected parameters
        public void SetFPSCounterState(bool state)
        {
            showFPSCounter = state;
            PlayerPrefs.SetInt("ShowFPSCounter", state ? 1 : 0);
            PlayerPrefs.Save();

            if (fpsCounter != null)
                fpsCounter.gameObject.SetActive(state);
        }

        public void SetSpeedCounterState(bool state)
        {
            PlayerPrefs.SetInt("ShowSpeedCounter", state ? 1 : 0);
            PlayerPrefs.Save();

            if (speedCounter != null)
                speedCounter.SetActive(state);
        }

        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }

        // Interface sounds
        public void PlayHoverSound()
        {
            if (hoverSound != null)
                audioSource.PlayOneShot(hoverSound);
        }

        public void PlayClickSound()
        {
            if (clickSound != null)
                audioSource.PlayOneShot(clickSound);
        }

        // Auto-assigns references to fields
        public void AssignReferences()
        {
            Transform ui = GameObject.Find("UI")?.transform;
            if (ui == null)
            {
                Debug.LogWarning("HUD not found in scene.");
                return;
            }

            pauseMenu = ui.Find("PauseMenu")?.gameObject;
            versionText = ui.Find("PauseMenu/ver")?.GetComponent<TextMeshProUGUI>();
            fpsCounter = ui.Find("HUD/Fps_counter")?.GetComponent<TextMeshProUGUI>();
            inputText = ui.Find("PauseMenu/Settings/Input_text")?.GetComponent<Text>();
            speedCounter = ui.Find("HUD/Speed_counter")?.gameObject;
            pauseButton = ui.Find("Controls/Pause")?.GetComponent<Button>();
            backButton = ui.Find("PauseMenu/Back")?.GetComponent<Button>();
            fpsToggle = ui.Find("PauseMenu/Settings/FPS_toggle")?.GetComponent<Toggle>();
            speedToggle = ui.Find("PauseMenu/Settings/Speed_toggle")?.GetComponent<Toggle>();
            volumeSlider = ui.Find("PauseMenu/Settings/Sound_slider")?.GetComponent<Slider>();

#if UNITY_2022_1_OR_NEWER
            audioSource = Object.FindAnyObjectByType<AudioSource>();
#else
            audioSource = Object.FindObjectOfType<AudioSource>();
#endif
            hoverSound = Resources.Load<AudioClip>("Sounds/GUI/UI Button_2");
            clickSound = Resources.Load<AudioClip>("Sounds/GUI/UI Button_1");
        }
    }
}

