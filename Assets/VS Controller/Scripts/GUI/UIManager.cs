/////////////////////////////////////////////////////////////////////////////////
//
//	UIManager.cs
//
//	Description:	configure the interface and connected things.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.UI;

namespace VSController
{
    public class UIManager : MonoBehaviour
    {
        [Foldout("Buttons")]
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button sprintButton;
        [SerializeField] private Button crouchButton;
        [SerializeField] private Button pauseButton;

        [SerializeField] private Button interactButton;
        [SerializeField] private Button throwButton;
        [SerializeField] private Button releaseButton;
        [SerializeField] private Button placeButton;
        [SerializeField] private Button takeButton;

        [Foldout("Sliders/Text")]
        [SerializeField] private Text sensitivityTextX;
        [SerializeField] private Text sensitivityTextY;
        [SerializeField] private Text accelerationText;
        [SerializeField] private Slider sensitivitySliderX;
        [SerializeField] private Slider sensitivitySliderY;
        [SerializeField] private Slider accelerationSlider;

        [Foldout("Other")]
        [SerializeField] private Image pointerImage;
        [SerializeField] private RectTransform lookZone;
        [SerializeField] private RectTransform joystickZone;
        [SerializeField] private Joystick joystick;


        [SerializeField] LookController lookController;
        [SerializeField] FPSController fpsController;
        [SerializeField] HUDManager hudManager;

        // Getters 
        public Joystick GetJoystick() => joystick;

        public Button GetJumpButton() => jumpButton;
        public Button GetSprintButton() => sprintButton;
        public Button GetCrouchButton() => crouchButton;
        public Button GetPauseButton() => pauseButton;

        public Button GetInteractButton() => interactButton;
        public Button GetThrowButton() => throwButton;
        public Button GetReleaseButton() => releaseButton;
        public Button GetPlaceButton() => placeButton;
        public Button GetTakeButton() => takeButton;

        public RectTransform GetLookZone() => lookZone;
        public RectTransform GetJoystickZone() => joystickZone;

        public Image GetPointer() => pointerImage;

        private const string SensitivityXKey = "Sensitivity_X";
        private const string SensitivityYKey = "Sensitivity_Y";
        private const string AccelerationKey = "Acceleration";

        // Here we get new values ​​for sensitivity
        public float Acceleration
        {
            get => lookController.acceleration;
            set => lookController.acceleration = value;
        }

        public float Sensitivity_X
        {
            get => lookController.m_Sensitivity.x;
            set => lookController.m_Sensitivity.x = value;
        }

        public float Sensitivity_Y
        {
            get => lookController.m_Sensitivity.y;
            set => lookController.m_Sensitivity.y = value;
        }

        // Get the saved sensitivity settings
        private void Start()
        {
            if (PlayerPrefs.HasKey(SensitivityXKey))
                Sensitivity_X = PlayerPrefs.GetFloat(SensitivityXKey);

            if (PlayerPrefs.HasKey(SensitivityYKey))
                Sensitivity_Y = PlayerPrefs.GetFloat(SensitivityYKey);

            if (PlayerPrefs.HasKey(AccelerationKey))
                Acceleration = PlayerPrefs.GetFloat(AccelerationKey);

            SetupSliders();
        }

        private void Update()
        {
            UpdateSensitivityText();
        }

        // Adjust sensitivity values ​​to ui text
        private void UpdateSensitivityText()
        {
            if (accelerationText != null)
                accelerationText.text = $"Acceleration: {Acceleration:F0}";

            if (sensitivityTextX != null)
                sensitivityTextX.text = $"By X: {Sensitivity_X:F0}";

            if (sensitivityTextY != null)
                sensitivityTextY.text = $"By Y: {Sensitivity_Y:F0}";
        }

        // Change parameters depending on the control mode (taken from FPSController.cs)
        public void ApplyControlMode(bool enableMobile)
        {
            if (hudManager != null) hudManager?.SetMobileControl(enableMobile);

            if (joystick != null) joystick.gameObject.SetActive(enableMobile);
            if (pauseButton != null) pauseButton.gameObject.SetActive(enableMobile);

            if (jumpButton != null) jumpButton.gameObject.SetActive(enableMobile && fpsController.canJump);
            if (sprintButton != null) sprintButton.gameObject.SetActive(enableMobile && fpsController.canSprint);
            if (crouchButton != null) crouchButton.gameObject.SetActive(enableMobile && fpsController.canCrouch);

            if (interactButton != null) interactButton.gameObject.SetActive(enableMobile);
            if (throwButton != null) throwButton.gameObject.SetActive(enableMobile);
            if (releaseButton != null) releaseButton.gameObject.SetActive(enableMobile);
            if (placeButton != null) placeButton.gameObject.SetActive(enableMobile);
            if (takeButton != null) takeButton.gameObject.SetActive(enableMobile);

            Cursor.lockState = enableMobile ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = enableMobile;

            // On mobile control reduce maximum sensitivity values
            float maxSensitivity = enableMobile ? 500f : 1000f;

            if (sensitivitySliderX != null)
                sensitivitySliderX.maxValue = maxSensitivity;

            if (sensitivitySliderY != null)
                sensitivitySliderY.maxValue = maxSensitivity;
        }

        // Set interact keys depending on control and holding status
        public void UpdateInteractionButtons(bool isMobile, bool isHolding)
        {
            if (interactButton != null) interactButton.gameObject.SetActive(isMobile && !isHolding);
            if (throwButton != null) throwButton.gameObject.SetActive(isMobile && isHolding);
            if (releaseButton != null) releaseButton.gameObject.SetActive(isMobile && isHolding);
            if (placeButton != null) placeButton.gameObject.SetActive(isMobile && isHolding);
            if (takeButton != null) takeButton.gameObject.SetActive(isMobile && isHolding);
        }

        // Auto-assigns references to fields
        public void AssignUI()
        {
            GameObject ui = GameObject.Find("UI");
            if (ui == null)
            {
                Debug.LogWarning("UI GameObject not found.");
                return;
            }

            Transform controls = ui.transform.Find("Controls");
            if (controls == null)
            {
                Debug.LogWarning("Controls panel not found in UI.");
            }
            else
            {
                jumpButton = controls.Find("Jump")?.GetComponent<Button>();
                sprintButton = controls.Find("Sprint")?.GetComponent<Button>();
                crouchButton = controls.Find("Crouch")?.GetComponent<Button>();
                pauseButton = controls.Find("Pause")?.GetComponent<Button>();

                interactButton = controls.Find("Interact")?.GetComponent<Button>();
                throwButton = controls.Find("Throw")?.GetComponent<Button>();
                releaseButton = controls.Find("Release")?.GetComponent<Button>();
                placeButton = controls.Find("Place")?.GetComponent<Button>();
                takeButton = controls.Find("Take")?.GetComponent<Button>();
                joystick = controls.Find("Joystick")?.GetComponent<Joystick>();
                lookZone = controls.Find("LookZone")?.GetComponent<RectTransform>();
                joystickZone = controls.Find("JoystickZone")?.GetComponent<RectTransform>();

#if UNITY_EDITOR
                AssignEditorRef("jumpButton", jumpButton);
                AssignEditorRef("sprintButton", sprintButton);
                AssignEditorRef("crouchButton", crouchButton);
                AssignEditorRef("pauseButton", pauseButton);
                AssignEditorRef("interactButton", interactButton);
                AssignEditorRef("throwButton", throwButton);
                AssignEditorRef("releaseButton", releaseButton);
                AssignEditorRef("placeButton", placeButton);
                AssignEditorRef("takeButton", takeButton);
                AssignEditorRef("joystick", joystick);
                AssignEditorRef("lookZone", lookZone);
                AssignEditorRef("joystickZone", joystickZone);
#endif
            }

            Transform settings = ui.transform.Find("PauseMenu/Settings");
            if (settings == null)
            {
                Debug.LogWarning("Settings panel not found in UI.");
            }
            else
            {
                sensitivityTextX = settings.Find("SensitiveX_text")?.GetComponent<Text>();
                sensitivityTextY = settings.Find("SensitiveY_text")?.GetComponent<Text>();
                accelerationText = settings.Find("Acceleration_text")?.GetComponent<Text>();

                sensitivitySliderX = settings.Find("SensitiveX_slider")?.GetComponent<Slider>();
                sensitivitySliderY = settings.Find("SensitiveY_slider")?.GetComponent<Slider>();
                accelerationSlider = settings.Find("Acceleration_slider")?.GetComponent<Slider>();

#if UNITY_EDITOR
                AssignEditorRef("sensitivityTextX", sensitivityTextX);
                AssignEditorRef("sensitivityTextY", sensitivityTextY);
                AssignEditorRef("accelerationText", accelerationText);
                AssignEditorRef("sensitivitySliderX", sensitivitySliderX);
                AssignEditorRef("sensitivitySliderY", sensitivitySliderY);
                AssignEditorRef("accelerationSlider", accelerationSlider);
#endif
            }

            Transform hud = ui.transform.Find("HUD");
            if (hud == null)
            {
                Debug.LogWarning("HUD panel not found in UI.");
            }
            else
            {
                pointerImage = hud.Find("Pointer")?.GetComponent<Image>();

#if UNITY_EDITOR
                AssignEditorRef("pointerImage", pointerImage);
#endif
            }

#if UNITY_2022_1_OR_NEWER
            hudManager = Object.FindAnyObjectByType<HUDManager>();
#else
            hudManager = Object.FindObjectOfType<HUDManager>();
#endif
            if (hudManager == null)
            {
                Debug.LogWarning("HUDManager not found in scene.");
            }
            else
            {
#if UNITY_EDITOR
                AssignEditorRef("hudManager", hudManager);
#endif
            }

#if UNITY_2022_1_OR_NEWER
            lookController = Object.FindAnyObjectByType<LookController>();
            fpsController = Object.FindAnyObjectByType<FPSController>();
#else
            lookController = Object.FindObjectOfType<LookController>();
            fpsController = Object.FindObjectOfType<FPSController>();
#endif

#if UNITY_EDITOR
            AssignEditorRef("lookController", lookController);
            AssignEditorRef("fpsController", fpsController);
#endif
        }

#if UNITY_EDITOR
        private void AssignEditorRef(string propertyName, UnityEngine.Object reference)
        {
            var so = new UnityEditor.SerializedObject(this);
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = reference;
                so.ApplyModifiedProperties();
                UnityEditor.Undo.RecordObject(this, $"Assign {propertyName}");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        // Assing values with sliders
        private void SetupSliders()
        {
            if (sensitivitySliderX != null)
            {
                sensitivitySliderX.onValueChanged.RemoveAllListeners();
                sensitivitySliderX.value = Sensitivity_X;
                sensitivitySliderX.onValueChanged.AddListener(SetSensitivityX);
            }

            if (sensitivitySliderY != null)
            {
                sensitivitySliderY.onValueChanged.RemoveAllListeners();
                sensitivitySliderY.value = Sensitivity_Y;
                sensitivitySliderY.onValueChanged.AddListener(SetSensitivityY);
            }

            if (accelerationSlider != null)
            {
                accelerationSlider.onValueChanged.RemoveAllListeners();
                accelerationSlider.value = Acceleration;
                accelerationSlider.onValueChanged.AddListener(SetAcceleration);
            }
        }

        public void SetSensitivityX(float _sensitivity_X)
        {
            Sensitivity_X = _sensitivity_X;
            Save();
        }

        public void SetSensitivityY(float _sensitivity_Y)
        {
            Sensitivity_Y = _sensitivity_Y;
            Save();
        }

        public void SetAcceleration(float value)
        {
            Acceleration = Mathf.Clamp(value, 0f, 100f);
            Save();
        }

        // Save the sensitivity values 
        private void Save()
        {
            PlayerPrefs.SetFloat(SensitivityXKey, Sensitivity_X);
            PlayerPrefs.SetFloat(SensitivityYKey, Sensitivity_Y);
            PlayerPrefs.SetFloat(AccelerationKey, Acceleration);
            PlayerPrefs.Save();
        }
    }
}

