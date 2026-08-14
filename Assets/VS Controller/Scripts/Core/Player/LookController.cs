/////////////////////////////////////////////////////////////////////////////////
//
//	LookController.cs
//
//	Description:	responsible for managing all camera-related behavior, 
//					ensuring smooth and dynamic movement.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    public class LookController : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private FPSController playerController;
        [SerializeField] private UIManager UIManager;
        [SerializeField] private Health health;

        private CharacterController controller;

        private int activeTouchID = -1; // How many touches on the screen now
        private Vector2 smoothLookInput; // Vector of a smoother sense of sensation

        [Foldout("Sensitivity")]
        public Vector2 m_Sensitivity = new Vector2(250f, 250f); // X and Y sensitivity
        public float acceleration = 0f; // Acceleration factor

        private float sensitivityFactor = 9f; // Sensitivity scale factor for additional tuning

        [Foldout("Idle Effect")]
        [SerializeField] private float IdleCameraShake = 1f; // Camera shake when idle

        [Foldout("Walk Effect")]
        [SerializeField] private float bobFrequency = 7f;       // Bobbing frequency while walking
        [SerializeField] private float bobAmplitude = 0.11f;    // Bobbing amplitude
        [SerializeField] private float bobTiltAngle = 0.24f;    // Camera tilt angle during bobbing

        private float bobTimer = 0f;                            // Timer for bobbing calculation
        private Vector3 originalCameraPosition;                 // Original camera local position
        private Vector3 originalHolderPosition;                 // Original camera holder position
        private float bobIntensity = 0f;                        // Current bobbing intensity
        private float targetBobIntensity = 0f;                  // Target bobbing intensity
        private float bobLerpSpeed = 1.5f;                      // Lerp speed between bobbing states
        private float currentBobOffsetX = 0f;                   // Current horizontal bob offset
        private float currentBobOffsetY = 0f;                   // Current vertical bob offset
        private float currentTiltZ = 0f;                        // Current tilt (Z-axis) during bobbing
        private float swayBobbingBlend = 0f;                    // Blend between idle sway and walking bob

        [Foldout("Landing Effect")]
        [SerializeField] private float landingStiffness = 190f;     // Spring stiffness for landing bounce
        [SerializeField] private float landingDamping = 25f;        // Spring damping for landing
        [SerializeField] private float maxLandingOffset = 0.8f;     // Max vertical offset from landing
        [SerializeField] private float minLandingOffset = 0.15f;     // Min vertical offset from landing

        [HideInInspector] public float landingImpact = 0f;          // Impact value used for visual landing effect
        [HideInInspector] public bool isSlidingNow = false;         // Is the player currently sliding

        private float maxAirHeight = 0f;                            // Highest Y-position while airborne
        private float bobDelayTimer = 0f;                           // Delay before camera bob resumes after landing
        private bool wasSlidingLastFrame = false;                   // Was the player sliding last frame
        private bool wasGroundedLastFrame;                          // Was the player grounded last frame

        private Vector3 landingSpringOffset = Vector3.zero;         // Spring offset applied to camera on landing
        private Vector3 landingSpringVelocity = Vector3.zero;       // Spring velocity for landing bounce
        private Vector3 crouchCameraOffset = Vector3.zero;          // Camera position during crouch

        [Foldout("Camera")]
        public Transform playerCamera;                              // Reference to player camera
        public Transform cameraHolder;                              // Reference to camera holder (pivot for effects)
        [Range(0, 180f)]
        [SerializeField] private float sprintFov = 75f;             // Field of view during run
        [Range(0, 0.5f)]
        [SerializeField] private float lookSmoothness = 0.3f;      // Camera rotation smoothing (The more, the less the effect)
        [SerializeField] private bool useSmoothLook = true;         // Use smoothed look input or raw (Not recommended for shooters)

        private bool isSprinting;                                   // Sprinting status from FPSController.cs
        private bool isCrouching;                                   // Crouching status from FPSController.cs
        private bool isLooking;                                     // Looking status from FPSController.cs
        private float walkFov;                                      // Default walk field of view (from playerCamera)
        private float cameraPitch = 0f;                             // Camera pitch angle (vertical look)
        private float MobileSensitivity => UIManager.Sensitivity_X / sensitivityFactor; // Dynamic mobile sensitivity

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            walkFov = playerCamera.GetComponent<Camera>().fieldOfView;

            originalCameraPosition = playerCamera.localPosition;
            originalHolderPosition = cameraHolder.localPosition;
        }

        private void Update()
        {
            HandleLook();

            // Changing Fov while running
            float targetFOV = isSprinting ? sprintFov : walkFov;
            float fovLerpSpeed = 2f;

            Camera cam = playerCamera.GetComponent<Camera>();
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

            float moveSpeedMultiplier = isSprinting ? playerController.sprintMultiplier : 1f;
            bool isMoving = (playerController.joystick.Direction.magnitude > 0.1f) || (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0);

            // Smooth transition between the walking and "breathing" effects
            swayBobbingBlend = Mathf.Lerp(swayBobbingBlend, isMoving ? 1f : 0f, Time.deltaTime * 5f);

            acceleration = UIManager.Acceleration;

            UpdateCameraEffects(isMoving, moveSpeedMultiplier);
        }

        // Combines camera effects smoothly
        private void UpdateCameraEffects(bool isMoving, float moveSpeedMultiplier)
        {
            IdleSway(out Vector3 idleOffset, out Quaternion idleRot);
            CameraBobbing(moveSpeedMultiplier, isMoving && playerController.isGrounded, out Vector3 bobOffset, out Quaternion bobRot);

            Vector3 totalOffset = originalHolderPosition + landingSpringOffset + crouchCameraOffset + Vector3.Lerp(idleOffset, bobOffset, swayBobbingBlend);
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, totalOffset, Time.deltaTime * 10f);

            Quaternion totalEffectRotation = Quaternion.Slerp(idleRot, bobRot, swayBobbingBlend);
            cameraHolder.localRotation = Quaternion.Lerp(cameraHolder.localRotation, totalEffectRotation, Time.deltaTime * 10f);
        }

        private void LateUpdate()
        {
            UpdateLandingCameraSpring();
        }

        // "Breathing" effects
        private void IdleSway(out Vector3 swayOffset, out Quaternion swayRotation)
        {
            // Here you can change the effect strength globally
            float idleFrequency = 1f;

            // Configure in detail
            float amplitudeX = 0.025f * IdleCameraShake; float amplitudeY = 0.015f * IdleCameraShake; float amplitudeZ = 0.005f * IdleCameraShake;
            float rotAmplitudeX = 0.45f * IdleCameraShake; float rotAmplitudeY = 0.45f * IdleCameraShake; float rotAmplitudeZ = 0.20f * IdleCameraShake;

            float time = Time.time * idleFrequency;

            float swayX = Mathf.Cos(time) * amplitudeX; float rotX = Mathf.Sin(time * 1.1f) * rotAmplitudeX;
            float swayY = Mathf.Sin(time * 0.8f) * amplitudeY; float rotY = Mathf.Cos(time * 0.9f + 0.5f) * rotAmplitudeY;
            float swayZ = Mathf.Sin(time * 1.3f) * amplitudeZ; float rotZ = Mathf.Sin(time * 0.7f + Mathf.PI / 3f) * rotAmplitudeZ;

            swayOffset = new Vector3(swayX, swayY, swayZ); swayRotation = Quaternion.Euler(rotX, rotY, rotZ);
        }

        // Walking effect
        private void CameraBobbing(float moveSpeedMultiplier, bool isMoving, out Vector3 bobOffset, out Quaternion bobRotation)
        {
            bobOffset = Vector3.zero;
            bobRotation = Quaternion.identity;

            // Conditions when the effect does not work
            if (playerController.freeFly || !playerController.canMove || playerController.OnIce || playerController.InWater) return;

            if (bobDelayTimer > 0)
            {
                bobDelayTimer -= Time.deltaTime;
                return;
            }

            // For the "Mud" surface, set special settings
            float currentBobFrequency = playerController.OnMud ? 5f : bobFrequency;

            // For crouching and running, also change the parameters
            targetBobIntensity = isMoving ? (isSprinting ? 1.2f : (isCrouching ? 0.8f : 1f)) : 0f;
            bobIntensity = Mathf.Lerp(bobIntensity, targetBobIntensity, Time.deltaTime * bobLerpSpeed);

            float stepSpeedMultiplier = isCrouching ? 0.5f : (isSprinting ? 1.1f : 1f);
            bobTimer += Time.deltaTime * currentBobFrequency * moveSpeedMultiplier * stepSpeedMultiplier;

            // Detailed effect settings
            float noiseX = Mathf.PerlinNoise(Time.time * 1.5f, 0.5f) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(0.5f, Time.time * 2f) * 2f - 1f;
            float fineNoiseX = Mathf.PerlinNoise(Time.time * 10f, 0.1f) * 2f - 1f;
            float fineNoiseY = Mathf.PerlinNoise(0.1f, Time.time * 12f) * 2f - 1f;

            float targetOffsetY = Mathf.Sin(bobTimer * 1.2f) * bobAmplitude;
            float targetOffsetX = Mathf.Sin(bobTimer * 0.75f) * bobAmplitude * 0.4f;

            targetOffsetX += (noiseX * 0.01f + fineNoiseX * 0.003f);
            targetOffsetY += (noiseY * 0.01f + fineNoiseY * 0.003f);

            targetOffsetX *= bobIntensity; targetOffsetY *= bobIntensity;

            float targetTiltZ = (Mathf.Sin(bobTimer) * bobTiltAngle + noiseX * 0.4f) * bobIntensity;

            float smoothSpeed = isMoving ? 10f : 3f;

            currentBobOffsetX = Mathf.Lerp(currentBobOffsetX, targetOffsetX, Time.deltaTime * smoothSpeed);
            currentBobOffsetY = Mathf.Lerp(currentBobOffsetY, targetOffsetY, Time.deltaTime * smoothSpeed);
            currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, Time.deltaTime * smoothSpeed);

            bobOffset = new Vector3(currentBobOffsetX, currentBobOffsetY - landingImpact, 0);
            bobRotation = Quaternion.Euler(0f, 0f, currentTiltZ);
        }

        // Handles the camera's "spring" effect when landing
        private void UpdateLandingCameraSpring()
        {
            Vector3 springForce = -landingStiffness * landingSpringOffset - landingDamping * landingSpringVelocity;
            landingSpringVelocity += springForce * Time.deltaTime;
            landingSpringOffset += landingSpringVelocity * Time.deltaTime;

            // When crouching change maxLandingOffset because in another way, camera drops dowm more that we need 
            float currentMaxOffset = isCrouching ? maxLandingOffset * 0.3f : maxLandingOffset;
            landingSpringOffset.y = Mathf.Max(landingSpringOffset.y, -currentMaxOffset);

            // Calculate final camera position
            playerCamera.localPosition = originalCameraPosition + landingSpringOffset;
        }

        // Calculates the strength of the landing effect and delegates damage when falling
        public void LandingEffect()
        {
            float currentY = transform.position.y;

            // Stores the starting height when leaving the ground
            if (!controller.isGrounded && wasGroundedLastFrame)
            {
                maxAirHeight = currentY;
            }

            // Update the peak height reached in the air
            if (!controller.isGrounded)
            {
                if (currentY > maxAirHeight)
                {
                    maxAirHeight = currentY;
                }
            }

            if (controller.isGrounded && !wasGroundedLastFrame && !wasSlidingLastFrame)
            {
                float fallDistance = maxAirHeight - currentY;

                // Height at which the effect does not work
                if (fallDistance < 0.2f)
                {
                    return;
                }

                // Settings for the effect 
                float landingVelocity = Mathf.Clamp(-playerController.velocity.y, 0f, 20f);

                float minFallHeight = 1.4f;
                float normalizedFallImpact = Mathf.Max(0f, fallDistance - minFallHeight) / (10f - minFallHeight);
                float normalizedVelocityImpact = landingVelocity / 15f;

                float combinedImpact = 0.5f * normalizedFallImpact + 0.5f * normalizedVelocityImpact;
                combinedImpact = Mathf.Clamp01(combinedImpact);

                if (combinedImpact > 0.05f)
                {
                    float impactMultiplier = Mathf.Pow(combinedImpact, 1.2f);
                    float cameraDip;

                    // Small drops use minimal dip, larger drops interpolate with impact strength
                    if (fallDistance <= minFallHeight)
                    {
                        cameraDip = -minLandingOffset;
                    }
                    else
                    {
                        cameraDip = -Mathf.Lerp(minLandingOffset, maxLandingOffset, impactMultiplier);
                    }

                    // Apply dip force into landing spring system
                    landingSpringVelocity.y = 0f;
                    landingSpringVelocity.y += cameraDip * 60f;
                    bobDelayTimer = 0.3f;
                }

                // if there is a health system then we apply fall damage
                if (health != null)
                {
                    health.ApplyFallDamage(fallDistance);
                }
            }

            // Check if player is grounded and is sliding
            wasGroundedLastFrame = controller.isGrounded;
            wasSlidingLastFrame = isSlidingNow;
        }

        // Camera looks in same direction as Respawn point 
        public void SetLookRotation(float pitch, float yaw)
        {
            // Normalize pitch
            pitch = Mathf.Clamp(pitch > 180f ? pitch - 360f : pitch, -89f, 89f);

            cameraPitch = pitch;
            smoothLookInput = Vector2.zero;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Reset effects
            landingSpringOffset = Vector3.zero;
            landingSpringVelocity = Vector3.zero;

            currentBobOffsetX = 0f;
            currentBobOffsetY = 0f;
            currentTiltZ = 0f;
            swayBobbingBlend = 0f;

            // Reset position
            cameraHolder.localPosition = originalHolderPosition;
            playerCamera.localPosition = originalCameraPosition;

            // Apply to camera
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        /// <summary>
        /// Allow outside classes to access these values 
        /// </summary>
        public void SetIsLooking(bool value)
        {
            isLooking = value;
        }

        public void SetIsCrouching(bool value)
        {
            isCrouching = value;
        }

        public void SetIsSprinting(bool value)
        {
            isSprinting = value;
        }

        public void SetCrouchOffset(float y)
        {
            crouchCameraOffset.y = y;
        }

        // Auto-assigns references to fields
        public void AssignReferences()
        {
            if (UIManager == null)
            {
#if UNITY_2022_1_OR_NEWER
                UIManager = Object.FindAnyObjectByType<UIManager>();
#else
                UIManager = Object.FindObjectOfType<UIManager>();
#endif

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("UIManager");
                if (prop != null)
                {
                    prop.objectReferenceValue = UIManager;
                    so.ApplyModifiedProperties();
                    UnityEditor.Undo.RecordObject(this, "Assign UIManager");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }

            if (health == null)
            {
                health = GetComponent<Health>();

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("health");
                if (prop != null)
                {
                    prop.objectReferenceValue = health;
                    so.ApplyModifiedProperties();
                    UnityEditor.Undo.RecordObject(this, "Assign Health");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }

            if (playerController == null)
            {
                playerController = GetComponent<FPSController>();

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("fpsController");
                if (prop != null)
                {
                    prop.objectReferenceValue = playerController;
                    so.ApplyModifiedProperties();
                    UnityEditor.Undo.RecordObject(this, "Assign FPSController");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }

            if (cameraHolder == null || playerCamera == null)
            {
                Transform holder = transform.Find("CameraHolder");
                Transform cam = holder != null ? holder.Find("Camera") : null;

                if (cameraHolder == null) cameraHolder = holder;
                if (playerCamera == null && cam != null) playerCamera = cam.GetComponent<Transform>();

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var holderProp = so.FindProperty("cameraHolder");
                if (holderProp != null)
                {
                    holderProp.objectReferenceValue = cameraHolder;
                }

                var camProp = so.FindProperty("playerCamera");
                if (camProp != null)
                {
                    camProp.objectReferenceValue = playerCamera;
                }

                so.ApplyModifiedProperties();
                UnityEditor.Undo.RecordObject(this, "Assign Camera References");
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        // Required for camera and joystick control areas
        private bool IsPointerInRect(RectTransform rect, Vector2 screenPos)
        {
            if (rect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null);
        }

        // The main method in this class that controls the rotation of the camera
        public void HandleLook()
        {
            if (!playerController.canLook) return;

            Vector2 targetLook = Vector2.zero;

            RectTransform lookZone = UIManager.GetLookZone();
            RectTransform joystickZone = UIManager.GetJoystickZone();

            // On PC
            if (!playerController.useMobileControls)
            {
                float lookX = Input.GetAxis("Mouse X") * UIManager.Sensitivity_X * Mathf.Pow(1 + acceleration * 0.1f, 2) * Time.deltaTime;
                float lookY = Input.GetAxis("Mouse Y") * UIManager.Sensitivity_Y * Mathf.Pow(1 + acceleration * 0.1f, 2) * Time.deltaTime;
                targetLook = new Vector2(lookX, lookY);
            }
            else
            {
                // On editor
                if (Input.GetMouseButtonDown(0))
                {
                    if (IsPointerInRect(lookZone, Input.mousePosition) &&
                        !IsPointerInRect(joystickZone, Input.mousePosition))
                    {
                        isLooking = true;
                    }
                }

                if (Input.GetMouseButtonUp(0))
                    isLooking = false;

                if (isLooking)
                {
                    float lookX = Input.GetAxis("Mouse X") * UIManager.Sensitivity_X * Mathf.Pow(1 + acceleration * 0.1f, 2) * Time.deltaTime;
                    float lookY = Input.GetAxis("Mouse Y") * UIManager.Sensitivity_Y * Mathf.Pow(1 + acceleration * 0.1f, 2) * Time.deltaTime;
                    targetLook = new Vector2(lookX, lookY);
                }
            }

            // On mobile controls
            if (playerController.useMobileControls)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (IsPointerInRect(lookZone, touch.position) &&
                            !IsPointerInRect(joystickZone, touch.position))
                        {
                            isLooking = true;
                            activeTouchID = touch.fingerId;
                        }
                    }

                    if (isLooking && touch.fingerId == activeTouchID)
                    {
                        float lookX = touch.deltaPosition.x * MobileSensitivity * Mathf.Pow(1 + acceleration * 0.1f, 2) * Time.deltaTime;
                        float lookY = touch.deltaPosition.y * MobileSensitivity * Mathf.Pow(1 + acceleration * 0.1f, 2) * Time.deltaTime;
                        targetLook = new Vector2(lookX, lookY);
                    }

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (touch.fingerId == activeTouchID)
                        {
                            isLooking = false;
                            activeTouchID = -1;
                        }
                    }
                }
            }

            // Make smooth camera movement
            if (useSmoothLook)
                smoothLookInput = Vector2.Lerp(smoothLookInput, targetLook, lookSmoothness);
            else
                smoothLookInput = targetLook;

            cameraPitch -= smoothLookInput.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up * smoothLookInput.x);
        }
    }
}


