/////////////////////////////////////////////////////////////////////////////////
//
//	FPSInput.cs
//
//	Description:	creates a scriptable object that stores the PC control
//	                layout.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

namespace VSController
{
    [CreateAssetMenu(menuName = "VS Controller/Input Data")]
    public class FPSInput : ScriptableObject
    {
        [Header("Movement keys")]
        public KeyCode forwardKey = KeyCode.W;
        public KeyCode backKey = KeyCode.S;
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;

        [Header("Interaction keys")]
        public KeyCode interactionKey = KeyCode.E;
        public KeyCode throwKey = KeyCode.Mouse0;
        public KeyCode placementKey = KeyCode.Q;

        [Header("Player control keys")]
        public KeyCode crouchKey = KeyCode.LeftControl;
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;

        [Header("Debug keys")]
        public KeyCode noclipKey = KeyCode.N;
        public KeyCode controlsToggle = KeyCode.O;
    }
}

