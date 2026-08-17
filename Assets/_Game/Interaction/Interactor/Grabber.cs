using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public class Grabber : MonoBehaviour
    {
        [SerializeField] Transform holdPoint;
        Vector3 originalHoldPos;
        [SerializeField] float moveToHoldPointSpeed = 6;
        [SerializeField] float minDistToMove = 0.1f;
        [SerializeField] float slowDownDist = 0.6f;
        Transform pickedObject = null;
        Rigidbody pickedRigidbody = null;
        LayerMask oldLayerMask;
        
        public bool IsGrabbing => pickedObject;

        void Start()
        {
            originalHoldPos = holdPoint.localPosition;
        }
        void FixedUpdate()
        {
            MovePickedObjectToHoldPoint();
        }
        private void MovePickedObjectToHoldPoint()
        {
            if (pickedObject == null || pickedRigidbody == null)
                return;

            
            // Distance and vector to pick up
            Vector3 toHold = holdPoint.position - pickedRigidbody.position;
            float dist = toHold.magnitude;

            
            if (dist < minDistToMove)
            {
                pickedRigidbody.linearVelocity = Vector3.zero;
                return;
            }

            float baseSpeed = moveToHoldPointSpeed;
            Vector3 desiredVel = toHold.normalized * baseSpeed;

            // Slow down object near a player
            float k = Mathf.Clamp01(dist / slowDownDist);
            desiredVel *= k;

#if UNITY_6000_0_OR_NEWER
            pickedRigidbody.linearVelocity = desiredVel;
#else
            pickedRigidbody.velocity = desiredVel;
#endif
        }
        public void Grab(Rigidbody picked)
        {
            pickedRigidbody = picked;
            pickedRigidbody.useGravity = false;
            pickedRigidbody.freezeRotation = true;
            
            oldLayerMask = picked.gameObject.layer;
            pickedRigidbody.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            
            pickedObject = picked.transform;
            
            //Make sure to keep dist with picked obj
            holdPoint.position = pickedRigidbody.position;
            //pickedObject.parent = holdPoint;
        }
        public void DropObject()
        {
            if (pickedObject != null)
            {
                pickedRigidbody.useGravity = true;
                pickedRigidbody.freezeRotation = false;
                pickedObject.parent = null;
                pickedObject.gameObject.layer = oldLayerMask;
            }

            ClearObject();
        }
        public void ClearObject(bool destroy = false)
        {
            if(destroy) Destroy(pickedObject.gameObject);

            pickedObject = null;
            pickedRigidbody = null;
            holdPoint.localPosition = originalHoldPos;
        }
    }
}