/////////////////////////////////////////////////////////////////////////////////
//
//	Mud.cs
//
//	Description:	adds swampy effect to the surface.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public class Mud : MonoBehaviour
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

            controller.SetMud(true);
        }

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponent<FPSController>();
            if (!controller) return;

            controller.SetMud(false);
        }

        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            var col = GetComponent<BoxCollider>();
            if (!col) return;

            Gizmos.color = new Color(0.4f, 0.1f, 0.1f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(1f, 0.1f, 0.1f);
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}