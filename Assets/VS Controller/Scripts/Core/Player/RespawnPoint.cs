/////////////////////////////////////////////////////////////////////////////////
//
//	RespawnPoint.cs
//
//	Description:	stores coordinates and passes them to Heath.cs.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    public class RespawnPoint : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        // Display the respawn mark
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = Color.green;
            Gizmos.DrawCube(transform.position, Vector3.one * 0.2f);

            Gizmos.color = Color.cyan;
            Vector3 dir = transform.forward * 0.5f;
            Gizmos.DrawRay(transform.position, dir);

            DrawArrowHead(transform.position + dir, dir);
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

