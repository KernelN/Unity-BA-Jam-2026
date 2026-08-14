/////////////////////////////////////////////////////////////////////////////////
//
//	FloorButton.cs
//
//	Description:	controls the floor button, its move,
//	                values ​​and triggers. 
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VSController
{
    [ExecuteAlways]
    [RequireComponent(typeof(Transform))]
    public class FloorButton : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How much button is pushed down")]
        [SerializeField] private float pressDepth = 0.2f;
        [Tooltip("The descent speed of the button")]
        [SerializeField] private float pressSpeed = 2f;

        private MechanismManager mechanismManager;
        private readonly List<MechanismManager.Mechanism> mechanisms = new();
        private HashSet<Collider> pressingObjects = new HashSet<Collider>();
        private Vector3 startPos;
        private Coroutine moveCoroutine;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Reset()
        {
            SetupTrigger();
        }

        private void OnValidate()
        {
            SetupTrigger();
        }

        // Coordinate with Mechanism Manager.cs
        public void AddMechanism(MechanismManager manager, MechanismManager.Mechanism mech)
        {
            if (manager == null || mech == null)
                return;

            mechanismManager = manager;
            if (!mechanisms.Contains(mech))
                mechanisms.Add(mech);
        }

        private void OnTriggerEnter(Collider other)
        {
            // To work, the button must be part of the mechanism
            if (mechanismManager == null || mechanisms.Count == 0) return;

            bool validForAny = false;

            // Checking the tag 
            foreach (var mech in mechanisms)
            {
                if (mech.allowedTags.Contains(other.tag))
                {
                    validForAny = true;
                    break;
                }
            }

            if (!validForAny) return;

            // Check and add button as activated
            bool wasInactive = pressingObjects.Count == 0;
            pressingObjects.Add(other);

            if (wasInactive)
            {
                // Start the animation 
                if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(MoveButton(startPos - new Vector3(0, pressDepth, 0)));

                foreach (var mech in mechanisms)
                    mechanismManager.ButtonPressed(mech);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Here everything is the same as during activation, only the other way around
            if (mechanismManager == null || mechanisms.Count == 0) return;

            bool validForAny = false;

            // Checking the tag 
            foreach (var mech in mechanisms)
            {
                if (mech.allowedTags.Contains(other.tag))
                {
                    validForAny = true;
                    break;
                }
            }

            if (!validForAny) return;

            pressingObjects.Remove(other);

            // Activate the return animation
            if (pressingObjects.Count == 0)
            {
                if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(MoveButton(startPos));

                foreach (var mech in mechanisms)
                    mechanismManager.ButtonReleased(mech);
            }
        }

        // Automatically add trigger
        private void SetupTrigger()
        {
            BoxCollider trigger = null;

            foreach (var col in GetComponents<BoxCollider>())
            {
                if (col.isTrigger)
                {
                    trigger = col;
                    break;
                }
            }

            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
            }

            Renderer rend = GetComponent<Renderer>();
            if (rend == null) return;

            Bounds localBounds;

            if (rend is MeshRenderer)
            {
                var mf = GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) return;
                localBounds = mf.sharedMesh.bounds;
            }
            else if (rend is SkinnedMeshRenderer skinned)
            {
                localBounds = skinned.localBounds;
            }
            else
            {
                return;
            }

            // Trigger will be 2.4 larger per render
            trigger.size = new Vector3(localBounds.size.x,localBounds.size.y * 2.4f,localBounds.size.z);
            trigger.center = localBounds.center;
        }

        // Button movement
        private IEnumerator MoveButton(Vector3 targetPos)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.001f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, pressSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
        }
    }
}

