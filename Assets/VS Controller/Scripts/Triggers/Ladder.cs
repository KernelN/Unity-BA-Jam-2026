/////////////////////////////////////////////////////////////////////////////////
//
//	Ladder.cs
//
//	Description:	a trigger that acts as a ladder and disables standard
//	                move in FPSController.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class Ladder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float climbSpeed = 3f;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private CharacterController playerController;
        private FPSController playerScript;
        private Transform playerCamera;
        private Transform playerTransform;

        private bool isClimbing = false;
        private BoxCollider boxCollider;
        private Vector3 lastSize;

        private void Start()
        {
            playerCamera = Camera.main.transform;

            boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

        private void Update()
        {
            //  Set the collider under the trigger and edit the size
            if (!Application.isPlaying)
            {
                if (boxCollider == null)
                    boxCollider = GetComponent<BoxCollider>();

                if (boxCollider.size != lastSize)
                {
                    lastSize = boxCollider.size;
                }
            }

            // Before calling the method responsible for movement on the stairs,
            // we check the necessary statuses.
            if (!isClimbing || playerController == null || playerCamera == null)
                return;

            HandleClimbing();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerController = other.GetComponent<CharacterController>();
                playerScript = other.GetComponent<FPSController>();

                playerTransform = other.transform;
                isClimbing = true;

                playerScript.OnLadder = true;

                Vector3 entryPos = other.transform.position;
                playerScript.ladderPosition = new Vector3(entryPos.x, 0f, entryPos.z);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && isClimbing)
            {
                StopClimbing();
            }
        }

        private void HandleClimbing()
        {
            // Get the player current vertical and horizontal input 
            float verticalInput = Input.GetAxis("Vertical");
            float horizontalInput = Input.GetAxis("Horizontal");

            if (Mathf.Abs(verticalInput) < 0.1f && Mathf.Abs(horizontalInput) < 0.1f)
                return;

            // Calculate movement based on where the player is looking and the input data
            Vector3 climbDir = transform.up * verticalInput + playerCamera.right * horizontalInput;
            climbDir = climbDir.normalized;

            // If the player tries to exit the ladder, call the StopClimbing()
            Vector3 ladderForward = -transform.forward;
            float forwardDot = Vector3.Dot(climbDir, ladderForward);

            if (forwardDot > 0.5f)
            {
                StopClimbing();
                return;
            }

            // Move the player
            playerController.Move(climbDir * climbSpeed * Time.deltaTime);

            Vector3 fixedXZ = new Vector3(playerScript.ladderPosition.x, playerTransform.position.y, playerScript.ladderPosition.z);
            playerTransform.position = fixedXZ;
        }

        private void StopClimbing()
        {
            isClimbing = false;
            playerScript.OnLadder = false;
        }

        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = matrix;

            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            Gizmos.color = Color.grey;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}

