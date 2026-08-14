/////////////////////////////////////////////////////////////////////////////////
//
//	Ice.cs
//
//	Description:	adds ice effect to the surface.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class Ice : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private BoxCollider boxCollider;

        private void Start()
        {
            //  Set the collider under the trigger
            if (!Application.isPlaying)
            {
                if (boxCollider == null)
                    boxCollider = GetComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var controller = other.GetComponent<FPSController>();
            if (!controller) return;

            controller.SetIce(true);
        }

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponent<FPSController>();
            if (!controller) return;

            controller.SetIce(false);
        }

        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            var col = GetComponent<BoxCollider>();
            if (!col) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(0f, 0.5f, 1f, 1f);
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}