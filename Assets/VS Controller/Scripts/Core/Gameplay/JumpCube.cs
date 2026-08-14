/////////////////////////////////////////////////////////////////////////////////
//
//	JumpCube.cs
//
//	Description:	a bonus script that adds Jump Cube functionality from
//	                the test scene.             
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.UI;

namespace VSController
{
    public class JumpCube : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool isActive = true;

        [Space(10)]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float raisedHeight = 1f;
        [SerializeField] private float returnSpeed = 2f;

        [Header("Sounds")]
        [SerializeField] private AudioClip jumpSound;

        private AudioSource audioSource;
        private Vector3 initialPosition;
        private bool isRaised;

        private void Start()
        {
            initialPosition = transform.position;
            audioSource = GetComponent<AudioSource>();
            isRaised = !isActive;

            // Find button in scene for mobile control
            Button takeJumpButton = null;
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();

            foreach (var btn in buttons)
            {
                if (btn.name == "TakeJump")
                {
                    takeJumpButton = btn;
                    break;
                }
            }

            if (takeJumpButton == null)
                return;

            takeJumpButton.onClick.AddListener(TryLowerCube);
        }

        private void Update()
        {
            UpdateCubePosition();

            if (Input.GetKeyDown(KeyCode.E))
                TryLowerCube();
        }

        private void LowerCube()
        {
            if (!isRaised) return;
            isRaised = false;
            isActive = true;
        }

        private void TryLowerCube()
        {
            if (!isRaised) return;

            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 5f))
            {
                JumpCube cube = hit.collider.GetComponent<JumpCube>();
                if (cube != null)
                    cube.LowerCube();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isRaised && other.CompareTag("Player") && isActive)
            {
                FPSController player = other.GetComponent<FPSController>();
                if (player != null)
                {
                    player.velocity.y = player.AddJumpForce(jumpForce);
                    player.isGrounded = false;
                    isRaised = true;
                    isActive = false;

                    if (audioSource != null && jumpSound != null)
                        audioSource.PlayOneShot(jumpSound);
                }
            }
        }

        private void UpdateCubePosition()
        {
            Vector3 targetPosition = isRaised ? initialPosition + Vector3.up * raisedHeight : initialPosition;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * returnSpeed);
        }
    }
}
