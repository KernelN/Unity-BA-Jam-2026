/////////////////////////////////////////////////////////////////////////////////
//
//	FPSController.cs
//
//	Description:	сore movement class: all player movement actions are
//	                handled here.
//					
/////////////////////////////////////////////////////////////////////////////////

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VSController
{
    public class FPSController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Base Settings")]
        public bool canMove = true;                    // Allow movement
        public bool canLook = true;                    // Allow looking around
        public bool canSprint = true;                  // Allow sprinting
        public bool canJump = true;                    // Allow jumping
        public bool canCrouch = true;                  // Allow crouching
        public bool freeFly = false;                   // Allow fly mode
        public bool canGoUpInWater = true;

        internal bool isGrounded;                      // Is player currently grounded
        internal Vector3 velocity;                     // Current movement velocity

        private CharacterController controller;        // Reference to CharacterController
        private Vector3 previousPosition;              // Last frame's position for movement analysis

        [Foldout("Movement")]
        [Header("Walk")]
        public float moveSpeed = 7f;                   // Base movement speed

        private float acceleration = 5f;               // Smooth start speed of walking
        private float deceleration = 5f;               // Walking smooth stop speed
        private float currentSpeedMultiplier = 1f;     // Multiplier for dynamic speed changes
        private Vector2 currentMoveInput = Vector2.zero; // Smoothed movement input

        [Header("Sprint")]
        public float sprintMultiplier = 1.7f;          // Speed multiplier during sprint
        private bool sprintButtonHeld = false;         // Sprint button state
        private bool isSprinting = false;              // Currently sprinting flag

        [Foldout("Jump and BunnyHop")]
        [Header("Jump")]
        [SerializeField] private float jumpForce = 8f; // Jump impulse force
        [SerializeField] private float gravity = 25f; // Gravity force

        [Header("BunnyHop")]
        [SerializeField] private float bunnyHopSpeedMultiplier = 1.1f; // Speed boost per hop
        [SerializeField] private float maxBunnyHopSpeed = 15f;         // Max speed from bunnyhop

        private bool bunnyHopIdle = false;             // Allow bunnyhop without auto jump
        private bool autoBunnyHop = false;             // Enable automatic bunnyhop               
        private bool canSlopeJump = false;             // Does the surface slope allow jumping

        [Foldout("Crouch")]
        [SerializeField] private float crouchHeight = 1f;              // Target height when crouching
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;   // Movement speed while crouching
        [SerializeField] private float crouchTransitionSpeed = 6f;     // Speed of crouch transition

        private float standingHeight;                  // Full standing height (from Character Controller)
        private bool isCrouching = false;              // Is player crouching
        private bool crouchButtonPressed = false;      // Is crouch input held

        [Foldout("Steps and Surfaces")]
        [SerializeField] private float baseStepInterval = 0.5f;        // Time between footsteps
        [SerializeField] private AudioSource audioSource;              // Step sound audio source
        [SerializeField] private SoundData soundData;                  // Surface-based sound data

        private float stepTimer = 0f;                  // Time since last step
        private float lastJumpSoundTime = -1f;         // Time of last jump sound
        private int lastStepIndex = -1;                // Last played footstep index

        private string currentSurfaceTag = "Default";  // Tag of surface currently underfoot
        private Material currentSurfaceMaterial;       // Material underfoot
        private Texture currentSurfaceTexture;         // Texture underfoot

        [Foldout("Controls (Mobile and PC)")]
        [Tooltip("ON/OFF Global Mobile/PC Controls (works for all scripts and UI in package)")]
        public bool useMobileControls;
        [SerializeField] private FPSInput InputData;   // Input configuration
        [SerializeField] private TextMeshProUGUI speedText; // Speed display text

        [Header("Links")]
        public UIManager UIManager;
        public LookController lookController;
        public Joystick joystick;

        private Transform playerCamera;
        private float initialCameraHeight;

        // Ladder climbing
        private bool isClimbing;                       // Сlimbing status 
        public Vector3 ladderPosition { get; set; }    // Current ladder position

        // Public properties for external read/write access
        public bool OnIce { get; private set; }
        public bool OnMud { get; private set; }
        public bool InWater { get; set; }
        public bool OnLadder { get; set; }

        private void Start()
        {
            controller = GetComponent<CharacterController>();

            standingHeight = controller.height;
            playerCamera = lookController.playerCamera;
            initialCameraHeight = playerCamera.localPosition.y;

            UIManager.GetJumpButton()?.onClick.AddListener(Jump);

            SetupButtonHold(UIManager.GetSprintButton(),
              () => sprintButtonHeld = true,
              () => sprintButtonHeld = false);

            SetupButtonHold(UIManager.GetCrouchButton(),
                () => crouchButtonPressed = true,
                () => crouchButtonPressed = false);

            // Assign the control mode with the interface in UIManager.cs
            if (UIManager != null)
            {
                UIManager.ApplyControlMode(useMobileControls);
                UIManager.UpdateInteractionButtons(useMobileControls, false);
            }
        }

        private void Update()
        {
            // Toggle between PC and mobile control (default key: "O").
            // You can delete these lines.
            if (Input.GetKeyDown(InputData.controlsToggle))
            {
                useMobileControls = !useMobileControls;
                UIManager.ApplyControlMode(useMobileControls);
            }

            RaycastHit hit;
            Vector3 groundNormal = Vector3.up;

            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f))
            {
                groundNormal = hit.normal;
                float angle = Vector3.Angle(hit.normal, Vector3.up);

                // Checks the tilt angle
                canSlopeJump = canJump && angle <= controller.slopeLimit;
            }

            ProcessSteps();
            Crouch();

            if (InWater)
                return;

            MovePlayer();

            // Text displaying the player's speed.
            // If not needed, you can delete these lines.
            if (speedText != null)
            {
                float currentSpeed = Vector3.Distance(transform.position, previousPosition) / Time.deltaTime;
                speedText.text = $"Speed: {currentSpeed:F2} m/s";
            }

            previousPosition = transform.position;
        }

        /// <summary>
        /// Allow outside classes to access these values 
        /// </summary>
        public void SetBunnyHopState(bool autoBunnyHopEnabled, bool bunnyHopIdleEnable)
        {
            autoBunnyHop = autoBunnyHopEnabled;
            bunnyHopIdle = bunnyHopIdleEnable;
        }

        public void SetClimbing(bool state)
        {
            isClimbing = state;
            if (isClimbing)
            {
                velocity = Vector3.zero;
            }
        }

        public float AddJumpForce(float additionalForce)
        {
            return jumpForce + additionalForce;
        }

        public void SetIce(bool value)
        {
            OnIce = value;
        }

        public void SetMud(bool value)
        {
            OnMud = value;
        }

        // Auto-assigns references to fields
        public void AssignUI()
        {
            GameObject ui = GameObject.Find("UI");
            if (ui == null) return;

            var speedTransform = ui.transform.Find("HUD/Speed_counter");
            if (speedTransform != null)
            {
                speedText = speedTransform.GetComponent<TextMeshProUGUI>();

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("speedText");
                if (prop != null)
                {
                    prop.objectReferenceValue = speedText;
                    so.ApplyModifiedProperties();
                    UnityEditor.Undo.RecordObject(this, "Assign UI");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }

            var joystickTransform = ui.transform.Find("Controls/Joystick");
            if (joystickTransform != null)
            {
                joystick = joystickTransform.GetComponent<Joystick>();
            }

            GameObject audioObj = GameObject.Find("Audio Source");
            if (audioObj != null)
            {
                audioSource = audioObj.GetComponent<AudioSource>();
            }
#if UNITY_2022_1_OR_NEWER
            UIManager uiManagerFound = Object.FindAnyObjectByType<UIManager>();
#else
            UIManager uiManagerFound = Object.FindObjectOfType<UIManager>();
#endif
            if (uiManagerFound != null)
            {
                UIManager = uiManagerFound;

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("uiManager");
                if (prop != null)
                {
                    prop.objectReferenceValue = UIManager;
                    so.ApplyModifiedProperties();
                    UnityEditor.Undo.RecordObject(this, "Assign UIManager");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }

#if UNITY_2022_1_OR_NEWER
            LookController lookControllerFound = Object.FindAnyObjectByType<LookController>();
#else
            LookController lookControllerFound = Object.FindObjectOfType<LookController>();
#endif
            if (lookControllerFound != null)
            {
                lookController = lookControllerFound;

#if UNITY_EDITOR
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("lookController");
                if (prop != null)
                {
                    prop.objectReferenceValue = lookController;
                    so.ApplyModifiedProperties();
                    UnityEditor.Undo.RecordObject(this, "Assign LookController");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }
        }

        // Detect pressing on the screen
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == UIManager.GetSprintButton().gameObject)
            {
                sprintButtonHeld = true;
            }
            else if (eventData.pointerCurrentRaycast.gameObject == UIManager.GetCrouchButton().gameObject)
            {
                crouchButtonPressed = true;
            }
            else
            {
                lookController.SetIsLooking(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == UIManager.GetSprintButton().gameObject)
            {
                sprintButtonHeld = false;
            }
            else if (eventData.pointerCurrentRaycast.gameObject == UIManager.GetCrouchButton().gameObject)
            {
                crouchButtonPressed = false;
            }
            else
            {
                lookController.SetIsLooking(false);
            }
        }

        private void MovePlayer()
        {
            float x = 0f, z = 0f;

            FreeFly();

            // When fly mode is enable, further logic stops working
            if (freeFly) return;

            // With a ladder the same
            if (OnLadder)
            {
                HandleLadder();
                return;
            }

            // Check agreement to move
            if (!canMove) return;

            isGrounded = controller.isGrounded;

            // Set the force of attraction
            if (!isGrounded)
            {
                velocity.y -= gravity * Time.deltaTime;
            }
            else if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Transfer the direction of movement to the joystick
            if (useMobileControls)
            {
                Vector2 input = joystick.IsInputAllowed ? joystick.Direction : Vector2.zero;
                x = input.x;
                z = input.y;
            }

            // If the PC control, then we transfer it to the keyboard
            if (!useMobileControls)
            {
                if (Input.GetKey(InputData.forwardKey)) z += 1f;
                if (Input.GetKey(InputData.backKey)) z -= 1f;
                if (Input.GetKey(InputData.leftKey)) x -= 1f;
                if (Input.GetKey(InputData.rightKey)) x += 1f;
            }

            // Smoothly interpolate movement input based on surface type ice, mud or normal 
            if (OnIce)
                currentMoveInput = Vector2.Lerp(currentMoveInput, new Vector2(x, z), Time.deltaTime * 2f);
            else if (OnMud)
                currentMoveInput = Vector2.Lerp(currentMoveInput, new Vector2(x, z), Time.deltaTime * 3f);
            else
            {
                if (new Vector2(x, z).magnitude > 0.1f)
                    currentMoveInput = Vector2.Lerp(currentMoveInput, new Vector2(x, z), Time.deltaTime * acceleration);
                else
                    currentMoveInput = Vector2.Lerp(currentMoveInput, Vector2.zero, Time.deltaTime * deceleration);
            }

            x = currentMoveInput.x;
            z = currentMoveInput.y;

            // Defines and applies running
            bool shiftHeld = Input.GetKey(InputData.sprintKey);
            isSprinting = canSprint && !isCrouching && (shiftHeld || sprintButtonHeld) && (new Vector2(x, z).magnitude > 0.25f);
            lookController.SetIsSprinting(isSprinting);

            // Changes global speed depending on the situation
            float speedMultiplier = isSprinting ? sprintMultiplier : (isCrouching ? crouchSpeedMultiplier : 1f);
            if (OnIce) speedMultiplier *= 3f;
            else if (OnMud) speedMultiplier *= 0.2f;

            if (new Vector2(x, z).magnitude < 0.1f) currentSpeedMultiplier = 1f;

            Vector3 move = (transform.right * x + transform.forward * z) * (moveSpeed * speedMultiplier * currentSpeedMultiplier);
            controller.Move(move * Time.deltaTime);

            // Sliding setup
            if (!freeFly)
            {
                if (PerformGroundCheck(GroundCheckType.Sliding, out RaycastHit slopeHit))
                {
                    // Values ​​at which sliding begins
                    Vector3 groundNormal = slopeHit.normal;
                    float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
                    float slideThreshold = 35f;

                    // Apply sliding on steep slopes based on slope angle and gravity direction
                    if (isGrounded && slopeAngle > slideThreshold)
                    {
                        Vector3 slideDir = Vector3.ProjectOnPlane(Physics.gravity, groundNormal).normalized;
                        float slideSpeed = Mathf.Lerp(0f, 50f, Mathf.Clamp((slopeAngle - slideThreshold) * 2f, 0f, 1f));

                        if (slopeAngle > 70f)
                        {
                            slideSpeed *= 2f;
                        }

                        velocity += slideDir * slideSpeed * Time.deltaTime;
                        lookController.isSlidingNow = true;
                    }
                    else
                    {
                        if (lookController.isSlidingNow)
                        {
                            velocity.x *= 0.5f;
                            velocity.z *= 0.5f;
                        }

                        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
                        horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * 5f);
                        velocity.x = horizontalVelocity.x;
                        velocity.z = horizontalVelocity.z;

                        lookController.isSlidingNow = false;
                    }
                }

                controller.Move(velocity * Time.deltaTime);
            }

            // Check and applying the landing effect
            if (!isGrounded && PerformGroundCheck(GroundCheckType.JumpEffect, out RaycastHit hiit))
            {
                // Don't apply
            }
            else
            {
                lookController.LandingEffect();
            }

            // On surfaces (MovableObject.cs) we define and become dependent to them
            if (PerformGroundCheck(GroundCheckType.JumpEffect, out RaycastHit groundHit))
            {
                MovableObject movable = groundHit.collider.GetComponent<MovableObject>();
                if (movable != null)
                {
                    if (transform.parent != movable.transform)
                    {
                        transform.SetParent(movable.transform);
                    }
                }
                else if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
            }
            else if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            // Apply the BunnyHop
            if (autoBunnyHop && isGrounded)
            {
                Jump();
            }

            // Apply the Jump in PC control
            if (!useMobileControls && Input.GetKeyDown(InputData.jumpKey))
            {
                if (isGrounded)
                {
                    Jump();
                }
            }
        }

        private (bool isMoving, float moveSpeedMultiplier) GetMovementState()
        {
            bool isMoving = false;
            float moveSpeedMultiplier = 1.1f;

            // Depending on the position of the joystick handle, the player's speed changes
            if (useMobileControls)
            {
                float joystickMagnitude = joystick.Direction.magnitude;
                if (joystickMagnitude > 0.1f)
                {
                    isMoving = true;
                    moveSpeedMultiplier = Mathf.Lerp(0.8f, 1.1f, joystickMagnitude);
                }
            }
            else
            {
                if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
                {
                    isMoving = true;
                }
            }

            // Change the speed during running or crouching
            if (isMoving)
            {
                if (isSprinting)
                {
                    moveSpeedMultiplier = 1.5f;
                }
                else if (isCrouching)
                {
                    moveSpeedMultiplier *= 0.7f;
                }
            }
            else
            {
                moveSpeedMultiplier = 1f;
            }

            return (isMoving, moveSpeedMultiplier);
        }

        // Behavior if the player is in the ladder trigger
        private void HandleLadder()
        {
            // Control by joystick
            if (useMobileControls)
            {
                Vector2 input = joystick.Direction;
                Vector3 verticalMove = Vector3.up * input.y;
                Vector3 horizontalMove = Vector3.ProjectOnPlane(lookController.playerCamera.right, Vector3.up) * input.x;
                Vector3 finalMove = verticalMove + horizontalMove;

                controller.Move(finalMove.normalized * moveSpeed * Time.deltaTime);
            }

            // Control by keyboard
            else
            {
                float climbSpeed = moveSpeed * 1.5f;
                Vector3 climbDir = Vector3.zero;

                if (Input.GetKey(InputData.forwardKey))
                    climbDir = Vector3.up;
                else if (Input.GetKey(InputData.backKey))
                    climbDir = Vector3.down;

                Vector3 verticalMove = climbDir * climbSpeed * Time.deltaTime;

                Vector3 targetXZ = new Vector3(ladderPosition.x, transform.position.y, ladderPosition.z);
                Vector3 diffXZ = targetXZ - transform.position;
                diffXZ.y = 0f;
                Vector3 horizontalMove = Vector3.Lerp(Vector3.zero, diffXZ, Time.deltaTime * 10f);

                Vector3 finalMove = verticalMove + horizontalMove;
                controller.Move(finalMove);
            }

            velocity = Vector3.zero;
        }

        // Behavior during noclip
        private void FreeFly()
        {
            if (Input.GetKeyDown(InputData.noclipKey))
            {
                freeFly = !freeFly;
                controller.enabled = !freeFly;
                velocity = Vector3.zero;

                if (!freeFly)
                {
                    isGrounded = controller.isGrounded;
                }
            }

            if (!freeFly) return;

            float speed = moveSpeed * (Input.GetKey(InputData.sprintKey) ? sprintMultiplier : 1f);
            Vector3 moveDirection = Vector3.zero;

            // Setting up controls during flight
            if (useMobileControls)
            {
                Vector2 moveInput = joystick.Direction;
                moveDirection += lookController.playerCamera.transform.right * moveInput.x;
                moveDirection += lookController.playerCamera.transform.forward * moveInput.y;
            }
            else
            {
                float x = 0f, z = 0f;

                if (Input.GetKey(InputData.forwardKey)) z += 1f;
                if (Input.GetKey(InputData.backKey)) z -= 1f;
                if (Input.GetKey(InputData.leftKey)) x -= 1f;
                if (Input.GetKey(InputData.rightKey)) x += 1f;

                Vector3 camForward = lookController.playerCamera.transform.forward.normalized;
                Vector3 camRight = lookController.playerCamera.transform.right.normalized;

                moveDirection = camRight * x + camForward * z;
            }

            transform.position += moveDirection.normalized * speed * Time.deltaTime;
        }

        private void Jump()
        {
            // Checking permission to jump
            if (!canMove || !canJump) return;

            // The player must be on the ground and not on a platform that is too steep
            if (isGrounded && canSlopeJump)
            {
                // On "Mud" the jump will be lower
                float jumpPower = OnMud ? jumpForce * 0.6f : jumpForce;
                velocity.y = jumpPower;
                isGrounded = false;

                // Disabling the ability to crouch during BunnyHop
                if ((autoBunnyHop || bunnyHopIdle) && isCrouching)
                {
                    isCrouching = false;
                }

                // Increasing the speed of jumping during BunnyHop
                if (autoBunnyHop || bunnyHopIdle)
                {
                    currentSpeedMultiplier *= bunnyHopSpeedMultiplier;
                    currentSpeedMultiplier = Mathf.Clamp(currentSpeedMultiplier, 1f, maxBunnyHopSpeed / moveSpeed);
                }

                // Turn on the jump sound
                if (soundData != null && audioSource != null && Time.time - lastJumpSoundTime >= 0.2f)
                {
                    AudioClip jumpClip = soundData.GetRandomJumpSound();
                    if (jumpClip != null)
                    {
                        audioSource.PlayOneShot(jumpClip);
                        lastJumpSoundTime = Time.time;
                    }
                }

                if (lookController.landingImpact > 0f)
                {
                    lookController.landingImpact = Mathf.Lerp(lookController.landingImpact, 0f, Time.deltaTime * 5);
                }
            }
        }

        // Conditions and intervals between footstep sounds
        private void ProcessSteps()
        {
            if (freeFly || !canMove || OnIce) return;

            var (isMoving, moveSpeedMultiplier) = GetMovementState();

            if (OnMud)
                moveSpeedMultiplier *= 0.6f;

            if (!isMoving || !isGrounded)
            {
                stepTimer = baseStepInterval;
                return;
            }

            stepTimer -= Time.deltaTime * moveSpeedMultiplier;

            if (stepTimer <= 0)
            {
                PlayStepSound();
                stepTimer = baseStepInterval / moveSpeedMultiplier;
            }
        }

        // Playing sound
        private void PlayStepSound()
        {
            if (soundData == null || audioSource == null) return;

            AudioClip clip = soundData.GetRandomStepSound(currentSurfaceTag, currentSurfaceMaterial, currentSurfaceTexture, ref lastStepIndex);
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void Crouch()
        {
            // Permission to crouch
            if (!canMove) return;

            if (canCrouch)
            {
                if (!useMobileControls && Input.GetKeyDown(InputData.crouchKey))
                    ToggleCrouch();

                // For mobile control
                if (useMobileControls && crouchButtonPressed)
                {
                    ToggleCrouch();
                    crouchButtonPressed = false;
                }
            }

            // Maximum height when standing up and permissions for this
            float targetHeight = isCrouching ? crouchHeight : standingHeight;
            if (!isCrouching && !CanStandUp()) return;

            // Crouch effect
            float prevHeight = controller.height;

            // Change height
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            // Fixate center of capsule at the bottom
            float delta = controller.height - prevHeight;
            Vector3 center = controller.center;
            center.y += delta * 0.5f;
            controller.center = center;

            if(!canCrouch) return;
            
            // Change camera height
            float cameraTarget = controller.height - (standingHeight - initialCameraHeight);
            float crouchPercent = Mathf.InverseLerp(standingHeight, crouchHeight, controller.height);
            float bonus = crouchPercent * 0.3f; // Reducing camera roll 
            float newY = cameraTarget - initialCameraHeight + bonus;

            lookController.SetCrouchOffset(newY);
        }

        // Check permission to stand up
        private bool CanStandUp()
        {
            float checkHeight = standingHeight - crouchHeight;
            Vector3 checkPosition = transform.position + controller.center + Vector3.up * (controller.height / 2); 

            if (Physics.Raycast(checkPosition, Vector3.up, out RaycastHit hit, checkHeight))
            {
                Debug.DrawRay(checkPosition, Vector3.up * checkHeight, Color.red);
                return false;
            }
            return true;
        }

        private void ToggleCrouch()
        {
            if (isCrouching)
            {
                if (CanStandUp())
                {
                    isCrouching = false;
                    lookController.SetIsCrouching(false);
                }
            }
            else
            {
                isCrouching = true;
                lookController.SetIsCrouching(true);
            }
        }

        // Configures a button to detect hold behavior by invoking callbacks on pointer down and pointer up events
        private void SetupButtonHold(Button button, UnityAction onDown, UnityAction onUp)
        {
            if (button == null) return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryDown = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            entryDown.callback.AddListener((data) => onDown());

            EventTrigger.Entry entryUp = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            entryUp.callback.AddListener((data) => onUp());

            trigger.triggers.Add(entryDown);
            trigger.triggers.Add(entryUp);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            currentSurfaceTag = hit.collider.tag;

            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = hit.collider.GetComponentInParent<Renderer>();
            }

            if (renderer != null)
            {
                currentSurfaceMaterial = renderer.sharedMaterial;

                if (renderer.sharedMaterial != null)
                {
                    currentSurfaceTexture = renderer.sharedMaterial.mainTexture;
                }
                else
                {
                    currentSurfaceTexture = null;
                }
            }
            else
            {
                currentSurfaceMaterial = null;
                currentSurfaceTexture = null;
            }
        }

        // Here you can add our type
        private enum GroundCheckType
        {
            JumpEffect,
            Sliding,
        }

        // Performs a configurable ground check using raycasts in multiple directions
        private bool PerformGroundCheck(GroundCheckType type, out RaycastHit hit)
        {
            hit = default;

            var dirs = type == GroundCheckType.Sliding ? slidingDirections : singleDown;
            float len = controller.height * 0.5f + 0.5f;
            Vector3 origin = transform.position;

            foreach (var d in dirs)
            {
                Debug.DrawRay(origin, d * len, Color.green, 0.1f);

                if (Physics.Raycast(origin, d, out hit, len, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    (type != GroundCheckType.Sliding || hit.normal.y >= 0.5f))
                    return true;
            }

            return false;
        }

        // You can specify the type of check by statics
        private static readonly Vector3[] slidingDirections =
        {
            Vector3.down,
             new Vector3(1f, -1f, 0f).normalized,
             new Vector3(-1f, -1f, 0f).normalized,
             new Vector3(0f, -1f, 1f).normalized,
             new Vector3(0f, -1f, -1f).normalized,
             new Vector3(0.5f, -1f, 0.5f).normalized,
             new Vector3(-0.5f, -1f, 0.5f).normalized,
             new Vector3(0.5f, -1f, -0.5f).normalized,
             new Vector3(-0.5f, -1f, -0.5f).normalized
        };

        private static readonly Vector3[] singleDown = { Vector3.down };

        public void SetHeight(float newHeight)
        {
            standingHeight = newHeight;
        }
    }
}

