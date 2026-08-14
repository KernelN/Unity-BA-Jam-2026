/////////////////////////////////////////////////////////////////////////////////
//
//	ManualButton.cs
//
//	Description:	controls the manual button, its move,
//	                values. 
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSController
{
    public class ManualButton : MonoBehaviour
    {
        [Header("Settings")]

        [Header("Position Target")]
        [Tooltip("The local offset applied when the button is pressed (e.g., (-1, 0, 0))")]
        [SerializeField] private Vector3 pressOffset = new Vector3(-1, 0, 0);

        [Header("Rotate Target")]
        [Tooltip("The local rotation applied when the button is pressed")]
        [SerializeField] private Vector3 pressRotation = Vector3.zero;

        [Tooltip("The speed of button movement")]
        [SerializeField] private float pressSpeed = 2f;

        private MechanismManager mechanismManager;
        private readonly List<MechanismManager.Mechanism> mechanisms = new();

        private Vector3 startLocalPos;
        private Quaternion startLocalRot;

        private Coroutine moveCoroutine;
        private bool isPressed = false;

        public bool IsActive => isPressed;

        // Adds a "Mechanism" tag for button to work - the tag can be different,
        // the main is assign in Grabbing in Object type
        private void Reset()
        {
#if UNITY_EDITOR
            if (UnityEditorInternal.InternalEditorUtility.tags.Contains("Mechanism"))
            {
                gameObject.tag = "Mechanism";
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Warning",
                    $"[{name}] To interact with the Manual Button,\nassign its tag in Grabbing → Object Types.",
                    "OK"
                );
            }
#endif
        }

        private void Start()
        {
            startLocalPos = transform.localPosition;
            startLocalRot = transform.localRotation;
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

        public void Toggle()
        {
            // To work, the button must be part of the mechanism
            if (mechanismManager == null || mechanisms.Count == 0) return;
            isPressed = !isPressed;

            // If the previous animation has not finished and we press it again, we stop it
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);

            Vector3 targetLocalPos = isPressed ? startLocalPos + pressOffset : startLocalPos;
            Quaternion targetLocalRot = isPressed ? Quaternion.Euler(startLocalRot.eulerAngles + pressRotation) : startLocalRot;

            // Start the animation
            moveCoroutine = StartCoroutine(MoveButton(targetLocalPos, targetLocalRot));

            // Switch the status in mechanism
            foreach (var mech in mechanisms)
            {
                if (isPressed)
                    mechanismManager.ButtonPressed(mech);
                else
                    mechanismManager.ButtonReleased(mech);
            }
        }

        // Button motion animation
        private IEnumerator MoveButton(Vector3 targetLocalPos, Quaternion targetLocalRot)
        {
            while (Vector3.Distance(transform.localPosition, targetLocalPos) > 0.001f ||
                   Quaternion.Angle(transform.localRotation, targetLocalRot) > 0.1f)
            {
                // Smoothly move the button
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, pressSpeed * Time.deltaTime);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, targetLocalRot, pressSpeed * Time.deltaTime);
                yield return null;
            }

            transform.localPosition = targetLocalPos;
            transform.localRotation = targetLocalRot;
        }
    }
}


