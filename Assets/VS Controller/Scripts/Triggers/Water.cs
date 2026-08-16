/////////////////////////////////////////////////////////////////////////////////
//
//	Water.cs
//
//	Description:	create the effect of water and nongravity.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class Water : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 triggerSize = new Vector3(1, 1, 1);
        [Tooltip("Player speed in water")]
        [SerializeField] private float movementDampFactor = 1f;
        [Tooltip("Change gravity value from FPSController")]
        [SerializeField] private float gravityInWater = -1f;
        [Tooltip("The way the player behaves in water: if < 1, player sinks; if > 1, player swim on water")]
        [SerializeField] private float buoyancy = 0.5f;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private FPSController player;
        private LookController lookController;
        private BoxCollider boxCollider;
        private bool isInWater;

        private Vector3 lastSize;

        private void Awake()
        {
            SetupCollider();
        }

        private void OnValidate()
        {
            SetupCollider();
        }

        private void Update()
        {
            //  Set the collider under the trigger and edit the size
            if (!Application.isPlaying)
            {
                if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

                if (boxCollider.size != lastSize)
                {
                    triggerSize = boxCollider.size;
                    lastSize = boxCollider.size;
                }
                else if (triggerSize != lastSize)
                {
                    boxCollider.size = triggerSize;
                    lastSize = triggerSize;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isInWater || player == null) return;

            CharacterController controller = player.GetComponent<CharacterController>();

            // Player movement in water is oriented by the camera
            Transform cam = lookController.playerCamera;

            float inputX = Input.GetAxis("Horizontal");
            float inputZ = Input.GetAxis("Vertical");

            // On mobile, control with a joystick
            if (player.joystick != null)
            {
                if (Mathf.Abs(player.joystick.Horizontal) > 0.1f || Mathf.Abs(player.joystick.Vertical) > 0.1f)
                {
                    inputX += player.joystick.Horizontal;
                    inputZ += player.joystick.Vertical;
                }
            }

            Vector3 input = new Vector3(inputX, 0f, inputZ);
            input = Vector3.ClampMagnitude(input, 1f);

            Vector3 moveDirection = cam.TransformDirection(input);
            if(!player.canGoUpInWater)
                moveDirection.y = 0;

            // Multiply speed of movement and deceleration coefficient in water
            moveDirection *= player.moveSpeed * movementDampFactor;

            // Add gravity taking into account buoyancy
            moveDirection.y += gravityInWater * (1 - buoyancy);

            // Move the player using the CharacterController with fixed frame time
            controller.Move(moveDirection * Time.fixedDeltaTime);
        }

        private void SetupCollider()
        {
            boxCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();

            boxCollider.isTrigger = true;
            boxCollider.size = triggerSize;
            lastSize = triggerSize;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                player = other.GetComponent<FPSController>();
                lookController = other.GetComponent<LookController>();
                if (player != null)
                {
                    isInWater = true;
                    player.InWater = true; // Switching the status in FPSController.cs
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (player != null && other.gameObject == player.gameObject)
                {
                    isInWater = false;
                    player.InWater = false;
                    player = null;
                }
            }
        }

        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}


