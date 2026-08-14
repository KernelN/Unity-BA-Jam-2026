/////////////////////////////////////////////////////////////////////////////////
//
//	EventsTrigger.cs
//
//	Description:	a trigger that uses UnityEvent to perform basic tasks.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class EventsTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private Vector3 triggerSize = new Vector3(1, 1, 1);
        [Tooltip("If true, triggers only once on the player's first entry")]
        [SerializeField] private bool triggerOnce = false;

        [Header("Delay (in seconds) before Enter Event")]
        [SerializeField] private float enterDelay = 0f;

        [Header("Delay (in seconds) before Exit Event")]
        [SerializeField] private float exitDelay = 0f;

        public UnityEvent onTriggerEnter;
        public UnityEvent onTriggerExit;

        private Coroutine enterCoroutine;
        private Coroutine exitCoroutine;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private bool hasTriggered = false;
        private BoxCollider boxCollider;
        private Vector3 lastSize;

        private void Awake()
        {
            SetupCollider();
        }

        private void Reset()
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

        private void SetupCollider()
        {
            boxCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();

            boxCollider.isTrigger = true;
            boxCollider.size = triggerSize;
            lastSize = triggerSize;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanTrigger(other))
                return;

            // onTriggerEnter starts delay
            if (enterDelay > 0f)
                enterCoroutine = StartCoroutine(EnterRoutine());
            else
                onTriggerEnter?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!CanTrigger(other))
                return;

            // onTriggerExit starts delay
            if (exitDelay > 0f)
                exitCoroutine = StartCoroutine(ExitRoutine());
            else
                InvokeExit();
        }

        private bool CanTrigger(Collider other)
        {
            if (!other.CompareTag("Player"))
                return false;

            if (!triggerOnce)
                hasTriggered = false;

            // If triggerOnce was true - trigger never would work again
            if (triggerOnce && hasTriggered)
                return false;

            // Full reset all delays
            ResetCoroutines();

            return true;
        }

        private void InvokeExit()
        {
            onTriggerExit?.Invoke();

            if (triggerOnce)
                hasTriggered = true; 
        }

        private void ResetCoroutines()
        {
            if (enterCoroutine != null)
            {
                StopCoroutine(enterCoroutine);
                enterCoroutine = null;
            }

            if (exitCoroutine != null)
            {
                StopCoroutine(exitCoroutine);
                exitCoroutine = null;
            }
        }

        private IEnumerator EnterRoutine()
        {
            yield return new WaitForSeconds(enterDelay);

            onTriggerEnter?.Invoke();

            enterCoroutine = null;
        }

        private IEnumerator ExitRoutine()
        {
            yield return new WaitForSeconds(exitDelay);

            InvokeExit(); 

            exitCoroutine = null;
        }

        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;

            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = matrix;

            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}

