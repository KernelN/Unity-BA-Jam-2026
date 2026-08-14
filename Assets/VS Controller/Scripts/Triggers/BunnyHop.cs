/////////////////////////////////////////////////////////////////////////////////
//
//	BunnyHop.cs
//
//	Description:	bunnyhop trigger that enables auto-jumping or
//	                acceleration in FPSController.cs.       
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class BunnyHop : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoBunnyHop = true;
        [SerializeField] private Vector3 triggerSize = new Vector3(1, 1, 1);

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private BoxCollider boxCollider;
        private Vector3 lastSize;

        public Vector3 TriggerSize
        {
            get => triggerSize;
            set
            {
                if (triggerSize != value)
                {
                    triggerSize = value;
                    if (boxCollider != null)
                    {
                        boxCollider.size = triggerSize;
                        lastSize = triggerSize;
                    }
                }
            }
        }

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
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();

            if (boxCollider == null)
                boxCollider = gameObject.AddComponent<BoxCollider>();

            boxCollider.isTrigger = true;
            boxCollider.size = triggerSize;
            lastSize = triggerSize;
        }

        // Assign data with controller
        private void OnTriggerEnter(Collider other)
        {
            FPSController controller = other.GetComponent<FPSController>();
            if (controller != null)
            {
                controller.SetBunnyHopState(autoBunnyHop, !autoBunnyHop);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            FPSController controller = other.GetComponent<FPSController>();
            if (controller != null)
            {
                controller.SetBunnyHopState(false, false);
            }
        }

        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;

            Color solidPurple = new Color(0.8f, 0.2f, 1f, 0.25f);
            Color wirePurple = new Color(1f, 0f, 1f, 1f);

            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.color = solidPurple;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            Gizmos.color = wirePurple;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);

            Gizmos.matrix = prev;
        }
    }
}

