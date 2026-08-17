using UnityEngine;
using UnityEngine.Events;

namespace UnityBaJam2026.Gameplay.Circuit
{
    public class CircuitPart : MonoBehaviour
    {
        [SerializeField] internal bool canDeactivate = true;
        [SaintsField.FieldReadOnly] internal bool active;

        public System.Action<CircuitPart> Activated;
        public UnityEvent<bool> OnActivated;
        
        public float percentage { get; internal set; }
        public bool IsActive => active;

        public void TrySetActive(bool shouldActivate)
        {
            //If is active & can't deactivate, exit
            if(active && !canDeactivate) return;
            
            //If it's already in the state it should be, exit
            if(active == shouldActivate) return;
            
            //Set the state
            active = shouldActivate;
            Activated?.Invoke(this);
            OnActivated?.Invoke(shouldActivate);
        }
        public void ForceDeactivate()
        {
            active = false;
            Activated?.Invoke(this);
            OnActivated?.Invoke(false);
        }
    }
}