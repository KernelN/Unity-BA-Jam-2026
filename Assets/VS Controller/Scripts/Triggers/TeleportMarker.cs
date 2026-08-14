/////////////////////////////////////////////////////////////////////////////////
//
//	TeleportMarker.cs
//
//	Description:	teleports player and objects from multiple starting
//	                points to one destination.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    public class TeleportMarker : MonoBehaviour
    {
        [Header("Teleport Points")]
        public Transform[] fromPoints;
        public Transform toPoint;

        [Header("Teleport Settings")]
        [SerializeField] private bool onlyPlayer = true;
        [SerializeField] private bool rotateToTarget = true;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private Vector3 triggerSize = Vector3.one;
        private bool hasTeleported = false;

        private void Update()
        {
            if (!Application.isPlaying || fromPoints == null || toPoint == null)
                return;

            foreach (var point in fromPoints)
            {
                if (point == null) continue;

                Collider trigger = point.GetComponent<Collider>();
                if (trigger == null) continue;

                if (trigger is BoxCollider box)
                {
                    Vector3 center = point.TransformPoint(box.center);
                    Quaternion rotation = box.transform.rotation;
                    Vector3 halfExtents = box.size * 0.5f;

                    Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);
                    Hits(hits, point);
                }
                else
                {
                    Bounds bounds = trigger.bounds;
                    Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, trigger.transform.rotation);
                    Hits(hits, point);
                }
            }
        }

        private void Hits(Collider[] hits, Transform sourcePoint)
        {
            foreach (var hit in hits)
            {
                // If onlyPlayer = true is enable then only the player is teleported
                if (onlyPlayer && !hit.CompareTag("Player")) continue;

                // Teleport all objects except the objects of teleporter itself (trigger)
                if (!onlyPlayer && (hit.transform == sourcePoint || hit.transform.IsChildOf(sourcePoint))) continue;

                if (!hasTeleported)
                {
                    Teleport(hit.transform);
                    hasTeleported = true;
                    Invoke(nameof(ResetTeleport), 0.5f);
                }
            }
        }

        private void Teleport(Transform target)
        {
            Vector3 fromPosition = target.position;
            Vector3 toPosition = toPoint.position;

            // Move and disable the Character Controller to avoid errors
            CharacterController controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                target.position = toPosition;
                controller.enabled = true;
            }
            else
            {
                target.position = toPosition;
            }

            if (rotateToTarget)
                target.rotation = toPoint.rotation;
        }

        private void ResetTeleport()
        {
            hasTeleported = false;
        }


        // Displaying the trigger
        private void OnDrawGizmos()
        {
            if (!drawGizmos || fromPoints == null || toPoint == null)
                return;

            foreach (var point in fromPoints)
            {
                if (point == null) continue;
                Collider col = point.GetComponent<Collider>();
                if (col == null) continue;

                if (col is BoxCollider box)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.3f);
                    Matrix4x4 prev = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(point.position, point.rotation, Vector3.one);
                    Gizmos.DrawCube(Vector3.zero, box.size);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(Vector3.zero, box.size);
                    Gizmos.matrix = prev;
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, 0.15f);
                    Gizmos.DrawCube(col.bounds.center, col.bounds.size);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(point.position, toPoint.position);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawCube(toPoint.position, Vector3.one * 0.2f);

            Gizmos.color = Color.cyan;
            Vector3 dir = toPoint.forward * 0.5f;
            Gizmos.DrawRay(toPoint.position, dir);
            DrawArrowHead(toPoint.position + dir, dir);
        }

        private void DrawArrowHead(Vector3 position, Vector3 direction)
        {
            float size = 0.1f;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;

            Gizmos.DrawRay(position, right * size);
            Gizmos.DrawRay(position, left * size);
        }
    }
}

