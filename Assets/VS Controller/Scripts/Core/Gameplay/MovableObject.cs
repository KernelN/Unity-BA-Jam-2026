/////////////////////////////////////////////////////////////////////////////////
//
//	MovableObject.cs
//
//	Description:	control of movements, coordinates and other parameters
//	                of a moving object.
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSController
{
    public class MovableObject : MonoBehaviour
    {
        [Header("Position Target")]
        [SerializeField] Vector3 openPositionOffset = Vector3.zero;

        [Header("Rotation Target")]
        [SerializeField] Vector3 openRotationOffset = Vector3.zero;

        [Header("Movement Curves")]
        [SerializeField] AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Response speed")]
        [SerializeField] float speed = 2f;

        [Header("Looping")]
        [SerializeField] bool loopPosition = false;
        [SerializeField] bool loopRotation = false;

        private Vector3 closedPosition;
        private Quaternion closedRotation;

        private Vector3 openPosition;
        private Quaternion openRotation;

        private Vector3 moveStartPos;
        private Quaternion moveStartRot;

        private Vector3 moveTargetPos;
        private Quaternion moveTargetRot;

        private float moveElapsed;                       // How much time has passed since the start of current movement
        private float moveDuration;                      // Total duration of the current movement
        private float loopAngle = 0f;                    // Current accumulated rotation angle 
        private float loopReturnTarget = 0f;             // Which angle should return to (0 or 360)
        private float returnSpeedDeg = 360f;             // Return speed in degrees per second
        private bool loopDirectionForward = true;        // Direction of loop movement (forward/back)
        private bool returningLoopRotation = false;      // Is rotation now reversing (when stop moving)

        private bool isMoving;
        private bool isOpen;
        private Rigidbody rb;

        private List<Transform> carriedObjects = new List<Transform>();
        private Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();
        private HashSet<Transform> objectsInTrigger = new HashSet<Transform>();

        public bool IsOpen() => isOpen;

        private void Start()
        {
            closedPosition = transform.position;
            closedRotation = transform.rotation;

            openPosition = closedPosition + openPositionOffset;
            openRotation = closedRotation * Quaternion.Euler(openRotationOffset);

            moveStartPos = closedPosition;
            moveStartRot = closedRotation;
            moveTargetPos = closedPosition;
            moveTargetRot = closedRotation;

            // Assign isKinematic to objects that will be on Movable Object
            rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        private void Update()
        {
            // List of attached objects
            foreach (var obj in objectsInTrigger.ToList())
            {
                if (obj == null) continue;

                if (obj.CompareTag("Player"))
                {
                    // Disconnect the player if he is dead
                    Health health = obj.GetComponent<Health>();
                    if (health == null || health.CurrentHealth <= 0)
                    {
                        DetachObject(obj);
                        objectsInTrigger.Remove(obj);
                        continue;
                    }
                }

                AttachObject(obj);
            }

            if (isMoving)
            {
                // Calculate the time of movement and the status of progress
                moveElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(moveElapsed / moveDuration);

                // Apply a curve to the movement
                float posT = positionCurve.Evaluate(t);
                float rotT = rotationCurve.Evaluate(t);

                Vector3 newPos = Vector3.Lerp(moveStartPos, moveTargetPos, posT);
                Quaternion newRot = Quaternion.Slerp(moveStartRot, moveTargetRot, rotT);

                if (rb != null)
                {
                    rb.MovePosition(newPos);

                    if (!loopRotation || (isOpen && !returningLoopRotation))
                        rb.MoveRotation(newRot);
                }
                else
                {
                    transform.position = newPos;

                    if (!loopRotation || (isOpen && !returningLoopRotation))
                        transform.rotation = newRot;
                }

                if (t >= 1f)
                {
                    isMoving = false;

                    // If the movement is looped
                    if (loopPosition && isOpen)
                    {
                        // Determine the direction, change and start
                        Vector3 targetPos = loopDirectionForward ? closedPosition : openPosition;
                        loopDirectionForward = !loopDirectionForward;
                        StartMove(targetPos, transform.rotation);
                    }
                }
            }
            // With rotate
            float rotSpeed = speed * 360f;

            // If loopRotation was true 
            if (loopRotation && isOpen && openRotationOffset != Vector3.zero)
            {
                loopAngle += Time.deltaTime * rotSpeed;

                // Build final target rotation relative to closedRotation
                Vector3 loopRot = openRotationOffset.normalized * loopAngle;
                Quaternion target = closedRotation * Quaternion.Euler(loopRot);

                // Also rotate any rigbody on this object
                if (rb != null) rb.MoveRotation(target);
                else transform.rotation = target;
            }

            // Return loopRotation to start position
            else if (loopRotation && returningLoopRotation && openRotationOffset != Vector3.zero)
            {
                float remaining = Mathf.Abs(loopReturnTarget - loopAngle);

                // Smooth comeback to original position
                float t = Mathf.Clamp01(remaining / 90f); 
                float step = Mathf.Lerp(rotSpeed * 0.15f, rotSpeed, t) * Time.deltaTime;

                // Move current loop angle toward chosen return target
                loopAngle = Mathf.MoveTowards(loopAngle, loopReturnTarget, step);

                // Calculate the direction of return and distance
                float appliedAngle = loopAngle >= 360f ? 0f : loopAngle;
                Vector3 loopRot = openRotationOffset.normalized * appliedAngle;
                Quaternion target = closedRotation * Quaternion.Euler(loopRot);

                // Also rotate any rigbody on this object
                if (rb != null) rb.MoveRotation(target);
                else transform.rotation = target;

                // Finish return 
                if (Mathf.Abs(loopAngle - loopReturnTarget) < 0.01f)
                {
                    returningLoopRotation = false;
                    loopAngle = 0f;

                    if (rb != null) rb.MoveRotation(closedRotation);
                    else transform.rotation = closedRotation;
                }
            }

            // Without loopRotation
            else if (!loopRotation && !isMoving && !isOpen)
            {
                // Smooth rotation with rigbody on it
                Quaternion currentRot = (rb != null) ? rb.rotation : transform.rotation;
                Quaternion newRot = Quaternion.RotateTowards(currentRot,closedRotation,returnSpeedDeg * Time.deltaTime);

                // Apply that
                if (rb != null) rb.MoveRotation(newRot);
                else transform.rotation = newRot;
            }
        }

        // This method can be used externally to activate an object
        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            loopDirectionForward = true;
            returningLoopRotation = false;

            StartMove(openPosition, openRotation);
        }

        // This one too
        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            loopDirectionForward = false;

            if (loopRotation && openRotationOffset != Vector3.zero)
            {
                loopAngle = Mathf.Repeat(loopAngle, 360f);
                loopReturnTarget = loopAngle > 180f ? 360f : 0f;
                returningLoopRotation = true;

                // двигаем только позицию, rotation оставляем как есть
                StartMove(closedPosition, transform.rotation);
                return;
            }

            StartMove(closedPosition, closedRotation);
        }

        // This method is useful for switching states(for example, doors)
        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        private void StartMove(Vector3 targetPos, Quaternion targetRot)
        {
            // Save states
            moveStartPos = transform.position;
            moveStartRot = transform.rotation;

            moveTargetPos = targetPos;
            moveTargetRot = targetRot;

            // Reset counter
            moveElapsed = 0f;

            // Calculate the total distance between the closed and open positions (to scale the movement time)
            float totalDist = Vector3.Distance(closedPosition, openPosition);

            // Calculate the remaining distance from the current position to the target
            float remainDist = Vector3.Distance(moveStartPos, moveTargetPos);

            // If the total distance is non-zero, we adjust the movement duration proportionally
            if (totalDist > 0.001f)
                moveDuration = speed * (remainDist / totalDist);
            else
                moveDuration = speed;

            isMoving = true;
        }


        /// <summary>
        /// These methods are used to attach and detach objects and the player while the Movable Object is moving
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            AttachObject(collision.transform);
        }

        private void OnCollisionExit(Collision collision)
        {
            DetachObject(collision.transform);
        }

        private void OnTriggerEnter(Collider other)
        {
            objectsInTrigger.Add(other.transform);
        }

        private void OnTriggerExit(Collider other)
        {
            objectsInTrigger.Remove(other.transform);
            DetachObject(other.transform);
        }

        private void AttachObject(Transform obj)
        {
            if (obj == transform) return;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if ((obj.CompareTag("Player") || rb != null) && !carriedObjects.Contains(obj))
            {
                carriedObjects.Add(obj);
                originalParents[obj] = obj.parent;
                obj.SetParent(transform);
            }
        }

        private void DetachObject(Transform obj)
        {
            if (!carriedObjects.Contains(obj)) return;

            if (originalParents.ContainsKey(obj))
                obj.SetParent(originalParents[obj]);
            else
                obj.SetParent(null);

            carriedObjects.Remove(obj);
            originalParents.Remove(obj);
        }
    }
}

