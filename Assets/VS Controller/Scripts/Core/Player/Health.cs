/////////////////////////////////////////////////////////////////////////////////
//
//	Health.cs
//
//	Description:	the script is responsible for the player's health system,
//	                including visual component and logic.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using static FoldoutAttribute;

namespace VSController
{
    public class Health : MonoBehaviour
    {
        [Foldout("Basic Settings")]
        [Header("Health")]
        [SerializeField] private float currentHealth;                     // Current health value
        [SerializeField] private float maxHealth = 100f;                  // Maximum health value

        [Header("Respawn")]
        [SerializeField] private float respawnTime = 1.5f;                // Delay before player respawns
        [SerializeField] private RespawnPoint respawnPoint;               // Assigned respawn point

        [Header("Sounds")]
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip addHeathSound;
        [SerializeField] private AudioClip respawnSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Links")]
        [SerializeField] private FPSController fpsController;
        [SerializeField] private LookController lookController;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Grabbing grabbing;

        [Foldout("Fall Damage")]
        [SerializeField] private float minDamageHeight = 4f;              // Minimum fall height to start taking damage
        [SerializeField] private float maxDamageHeight = 15f;             // Maximum fall height for full damage
        [SerializeField]
        AnimationCurve damageEffectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Curve to control landing camera effect

        [Foldout("UI")]
        [Header("Health Bar")]
        [SerializeField] private Image hpPanel;                           // UI panel for health display
        [SerializeField] private TextMeshProUGUI hpText;                  // Text showing current HP
        [SerializeField] private Sprite normalHpSprite;                   // Sprite for normal HP state
        [SerializeField] private Sprite lowHpSprite;                      // Sprite for low HP state

        [Header("Overlay")]
        [SerializeField] private bool useOverlays = true;                 // Enable blood/heal screen overlays
        [SerializeField] private Image bloodOverlay;                      // Red screen overlay when damaged
        [SerializeField] private Image healOverlay;                       // Green screen overlay when healing
        [EndFoldout]

        private float currentHealAlpha = 0f;                              // Current alpha of heal overlay
        private float targetBloodAlpha = 0f;                              // Target alpha of blood overlay
        private float currentBloodAlpha = 0f;                             // Current alpha of blood overlay

        private Transform cameraHolder;
        private Transform playerCamera;

        private bool isDead = false;                                      // Player death state

        private bool wasCanMove;                                          // Cached movement state before death
        private bool wasCanJump;                                          // Cached jump state before death
        private bool wasCanCrouch;                                        // Cached crouch state before death
        private Vector3 originalCameraPosition;                           // Camera position before death/fall
        private Quaternion originalCameraRotation;                        // Camera rotation before death/fall

        public float CurrentHealth => currentHealth;

        private void Start()
        {
            currentHealth = maxHealth;
            cameraHolder = lookController.cameraHolder;
            playerCamera = lookController.playerCamera;
            originalCameraPosition = playerCamera.localPosition;
            originalCameraRotation = cameraHolder.localRotation;
        }

        private void Update()
        {
            // Cause player death
            if (!isDead && currentHealth <= 0)
            {
                Die();
            }

            // Display a current hp value on ui
            if (hpText != null)
            {
                hpText.text = $"+{(int)currentHealth}";
            }

            // Change the color of the hp panel depending on health
            if (hpPanel != null)
            {
                float healthPercent = currentHealth / maxHealth;

                if (healthPercent < 0.25f && hpPanel.sprite != lowHpSprite)
                {
                    hpPanel.sprite = lowHpSprite;
                }
                else if (healthPercent >= 0.25f && hpPanel.sprite != normalHpSprite)
                {
                    hpPanel.sprite = normalHpSprite;
                }
            }

            // Overlay displaying hp status
            if (useOverlays)
            {
                if (bloodOverlay != null)
                {
                    float hpPercent = currentHealth / maxHealth;

                    // Overlay brightness depends on hp (the less health, the brighter the sprite)
                    if (hpPercent < 0.3f)
                    {
                        float dangerT = Mathf.InverseLerp(0.3f, 0f, hpPercent);
                        targetBloodAlpha = dangerT * 0.7f;
                    }
                    else
                    {
                        targetBloodAlpha = 0f;
                    }

                    currentBloodAlpha = Mathf.MoveTowards(currentBloodAlpha, targetBloodAlpha, Time.deltaTime * 2f);

                    // Create a pulsation effect
                    float pulse = currentBloodAlpha > 0.01f ? Mathf.Sin(Time.time * 2) * 0.1f : 0f;
                    float finalAlpha = Mathf.Clamp01(currentBloodAlpha + pulse);

                    // Turn off the object itself when it is not needed
                    bool shouldBeVisible = finalAlpha > 0.01f;
                    if (bloodOverlay.gameObject.activeSelf != shouldBeVisible)
                        bloodOverlay.gameObject.SetActive(shouldBeVisible);

                    // Apply settings
                    if (shouldBeVisible)
                    {
                        Color c = bloodOverlay.color;
                        c.a = finalAlpha;
                        bloodOverlay.color = c;
                    }
                }

                if (healOverlay != null)
                {
                    if (currentHealAlpha > 0f)
                    {
                        // Something similar here too
                        currentHealAlpha = Mathf.MoveTowards(currentHealAlpha, 0f, Time.deltaTime * 2f);

                        Color healColor = healOverlay.color;
                        healColor.a = currentHealAlpha;
                        healOverlay.color = healColor;

                        if (!healOverlay.gameObject.activeSelf)
                            healOverlay.gameObject.SetActive(true);
                    }
                    else
                    {
                        if (healOverlay.gameObject.activeSelf)
                            healOverlay.gameObject.SetActive(false);
                    }
                }
            }
        }

        // A method to TAKES DAMAGE
        public void TakeDamage(float amount)
        {
            // Player must be alive
            if (isDead) return;

            // Sound of taking damage
            if (respawnSound != null && audioSource != null)
                audioSource.PlayOneShot(damageSound);

            currentHealth -= amount;
            currentHealth = Mathf.Round(currentHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            // If less than 0 hp then we cause death
            if (currentHealth <= 0f)
            {
                Die();
            }

            // If not, we apple the effect of taking damage
            if (!isDead)
            {
                StartCoroutine(AnimateCameraKickback(amount));
            }
        }

        // A method to ADD HEALTH
        public void AddHealth(float addHealth)
        {
            // Play the sound of treatment
            if (respawnSound != null && audioSource != null)
                audioSource.PlayOneShot(addHeathSound);

            currentHealth += addHealth;
            currentHealth = Mathf.Round(currentHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            // Visual overlay
            currentHealAlpha = 0.8f;
        }

        public void ApplyFallDamage(float fallHeight)
        {
            // The height must be higher than the minimum
            if (fallHeight < minDamageHeight) return;

            // Calculate how high the point you fall to, taking into account the minimum and maximum values
            float t = (fallHeight - minDamageHeight) / (maxDamageHeight - minDamageHeight);
            t = Mathf.Clamp01(t);

            // Apply damage
            float damage = maxHealth * t;
            TakeDamage(damage);
        }

        // Start of respawn
        public void Respawn(Vector3 position)
        {
            fpsController.transform.position = position;
            StartCoroutine(DelayedRespawn());
        }

        // Auto-assigns references to fields
        public void AssignReferences()
        {
            fpsController = GetComponent<FPSController>();
            lookController = GetComponentInChildren<LookController>();
            controller = GetComponent<CharacterController>();
            grabbing = GetComponent<Grabbing>();

#if UNITY_2022_1_OR_NEWER
            respawnPoint = Object.FindAnyObjectByType<RespawnPoint>();
#else
            respawnPoint = Object.FindObjectOfType<RespawnPoint>();
#endif

            hpPanel = GameObject.Find("UI/HUD/HP_bar")?.GetComponent<Image>();
            hpText = GameObject.Find("UI/HUD/HP_bar/HP_text")?.GetComponent<TextMeshProUGUI>();
            bloodOverlay = GameObject.Find("UI/HUD/Blood")?.GetComponent<Image>();
            healOverlay = GameObject.Find("UI/HUD/Health")?.GetComponent<Image>();

            GameObject audioObj = GameObject.Find("Audio Source");
            if (audioObj != null)
            {
                audioSource = audioObj.GetComponent<AudioSource>();
            }

            normalHpSprite = Resources.Load<Sprite>("Text2D/UI/MenyShadow");
            lowHpSprite = Resources.Load<Sprite>("Text2D/UI/hp_red");
            respawnSound = Resources.Load<AudioClip>("Sounds/Movement/Spawn");
            addHeathSound = Resources.Load<AudioClip>("Sounds/Movement/Health");
            damageSound = Resources.Load<AudioClip>("Sounds/Movement/Damage");

            damageEffectCurve = new AnimationCurve(new Keyframe(0f, 0f, 3f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        }

        private void Die()
        {
            isDead = true;

            // Save values ​​from controller
            wasCanMove = fpsController.canMove;
            wasCanJump = fpsController.canJump;
            wasCanCrouch = fpsController.canCrouch;

            // Turn off everything that we don’t need during death((
            fpsController.canMove = false;
            fpsController.canJump = false;
            fpsController.canCrouch = false;
            lookController.enabled = false;
            grabbing.ResetPickedObject();
            grabbing.enabled = false;

            // By throwing the camera away we create the effect of free fall during death
            playerCamera.SetParent(null);

            // To do this, we add physics to it
            if (playerCamera.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = playerCamera.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.mass = 1f;

#if UNITY_6000_0_OR_NEWER
                rb.angularDamping = 0.05f;
#else
                rb.angularDrag = 0.05f;
#endif
            }

            // And collision
            if (playerCamera.GetComponent<CapsuleCollider>() == null)
            {
                CapsuleCollider sc = playerCamera.gameObject.AddComponent<CapsuleCollider>();
                sc.radius = 0.4f;
                sc.height = 1.3f;
                sc.center = Vector3.zero;
            }

            // Start respawning
            StartCoroutine(HandleRespawnAfterDeath());
        }

        private IEnumerator HandleRespawnAfterDeath()
        {
            yield return new WaitForSeconds(0.1f);
            controller.enabled = false;

            yield return new WaitForSeconds(respawnTime);

            // Respawn only starts if a RespawnPoint is assigned.
            if (respawnPoint != null)
            {
                Respawn(respawnPoint.Position);
                fpsController.transform.rotation = respawnPoint.Rotation;
            }
            else
            {
                Debug.LogWarning("RespawnPoint not assigned to inspector!");
            }
        }

        // This method reproduces technical already after respawn
        // and returns all values ​​that were before death
        private IEnumerator DelayedRespawn()
        {
            yield return new WaitForSeconds(0.1f);

            // Playing the respawn sound
            if (respawnSound != null && audioSource != null)
                audioSource.PlayOneShot(respawnSound);

            isDead = false;
            currentHealth = maxHealth;

            // Return the cached values
            fpsController.canMove = wasCanMove;
            fpsController.canJump = wasCanJump;
            fpsController.canCrouch = wasCanCrouch;
            controller.enabled = true;
            grabbing.enabled = true;

            // Remove physics and collision from the camera
            Rigidbody rb = playerCamera.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            CapsuleCollider sc = playerCamera.GetComponent<CapsuleCollider>();
            if (sc != null) Destroy(sc);

            // And put back in place
            playerCamera.SetParent(cameraHolder.transform);
            playerCamera.localPosition = originalCameraPosition;

            // Camera looks in same direction as Respawn point
            Quaternion rot = respawnPoint.Rotation;
            fpsController.transform.rotation = rot;
            Vector3 euler = rot.eulerAngles;
            lookController.SetLookRotation(euler.x, euler.y);
            lookController.enabled = true;
        }

        // Damage Received Effect
        private IEnumerator AnimateCameraKickback(float damage)
        {
            // Here calculate the effect limits
            float damagePercent = Mathf.Clamp01(damage / maxHealth);
            float kickAngle = Mathf.Lerp(7.5f, 20f, damagePercent);

            Quaternion targetRotation = Quaternion.Euler(originalCameraRotation.eulerAngles + new Vector3(0f, 0f, kickAngle));

            float applyTime = 0.1f;    // Duration to apply kick
            float restoreTime = 0.5f;  // Duration to restore camera to original

            float elapsedApply = 0f;
            while (elapsedApply < applyTime)
            {
                // Use a curve to create animation.
                elapsedApply += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedApply / applyTime);
                float curveT = damageEffectCurve.Evaluate(t);

                // Apply kick rotation smoothly
                cameraHolder.localRotation = Quaternion.Slerp(originalCameraRotation, targetRotation, curveT);
                yield return null;
            }

            float elapsedRestore = 0f;
            while (elapsedRestore < restoreTime)
            {
                elapsedRestore += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedRestore / restoreTime);
                float curveT = damageEffectCurve.Evaluate(t);

                // Smoothly restore rotation back to original
                cameraHolder.localRotation = Quaternion.Slerp(targetRotation, originalCameraRotation, curveT);
                yield return null;
            }

            // Make a sure camera is exactly at original rotation
            cameraHolder.localRotation = originalCameraRotation;
        }
    }
}


