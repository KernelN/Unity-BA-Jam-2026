using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityBaJam2026.Gameplay.Circuit
{
    public class CircuitTarget : MonoBehaviour
    {
        [Serializable] 
        enum ActivationModes { AnyActive, AllActive, AtLeast, AtMost }
        
        [SerializeField] List<CircuitPart> circuitParts;
        List<CircuitPart> activeCircuitParts;

        [SaintsField.Separator]
        
        [SerializeField] ActivationModes mode;
        bool UsePartsCount =>  mode is ActivationModes.AtLeast or ActivationModes.AtMost;
        [SaintsField.FieldShowIf("UsePartsCount")]
        [SerializeField, Min(1)] int partsNeeded = 1; 
        
        [SaintsField.Separator]
        
        [Tooltip("If time <= 0, it'll never be locked active")] 
        [SerializeField] float timeToLockActive = -1;
        float lockActiveTimer;
        bool activeLocked;
        
        [SaintsField.Separator]
        
        [SerializeField] bool canDeactivate = true;
        bool isComplete;
        bool isDisabled;

        public UnityEvent<float> CircuitPercentageUpdated;
        public UnityEvent<bool> CircuitCompleted;
        public UnityEvent CircuitLocked;
        public UnityEvent<bool> ForcedActive;
        
        public float Percent { get; private set; }
        
        public bool IsComplete => isComplete;
        public bool CanBeLocked => timeToLockActive >= 0;

        //Unity Events
        private void Awake()
        {
            activeCircuitParts = new List<CircuitPart>();
            for (int i = 0; i < circuitParts.Count; i++)
                circuitParts[i].Activated += OnCircuitPartActivated;
        }
        void Update()
        {
            if(isDisabled) return;
            if (ShouldLockActive())
                LockActivation();
        }

        //Methods
        public void ForceActive()
        {
            ForceActive(false);
        }
        public void ForceActive(bool forceLocked, bool canLock = true)
        {
            isComplete = true;
            
            if (forceLocked)
            {
                activeLocked = true;
                CircuitLocked?.Invoke();
            }
            
            ForcedActive?.Invoke(isComplete);

            if (!canLock)
                activeLocked = true; //it's set as locked, but no one knows about it (no event called)
        }
        public void ForcePercent(float percent)
        {
            if(percent > 1) percent = 1;
            
            Percent = percent;
            
            if(percent < 1) return;
            if(isComplete) return;
            
            isComplete = true;
            CircuitCompleted?.Invoke(isComplete);
        }
        /// <summary>
        /// Forces all the circuits to be checked
        /// </summary>
        /// <param name="sendEvent">call the CompleteActions on state change?</param>
        public void ForceCheck(bool sendEvent = true)
        {
            for (int i = 0; i < circuitParts.Count; i++)
                CheckPartActivation(circuitParts[i], sendEvent);
        }
        public void Deactivate()
        {
            isComplete = false;
            CircuitCompleted?.Invoke(false);
        }
        public void Enable()
        {
            isDisabled = false;
        }
        public void Disable()
        {
            isDisabled = true;
        }
        bool ShouldLockActive()
        {
            if (!CanBeLocked) return false; //It can't be locked active, so exit
            if (!isComplete)
            {
                lockActiveTimer = 0;
                return false; 
            } //It's not active yet, so reset timer & exit
            if(activeLocked) return false; //It's already locked, so exit

            lockActiveTimer += Time.deltaTime;
            return lockActiveTimer >= timeToLockActive;
        }
        void LockActivation()
        {
            activeLocked = true;
            CircuitLocked?.Invoke();
        }
        void CheckPartActivation(CircuitPart part, bool sendEvent = true)
        {
            if(isDisabled) return;
            if(activeLocked) return;
            if(isComplete && !canDeactivate) return;

            //If list update failed, exit
            if(!UpdatePartList(part)) return;
            
            bool wasComplete = isComplete;
            switch (mode)
            {
                case ActivationModes.AllActive:
                    isComplete = activeCircuitParts.Count >= circuitParts.Count;
                    Percent = activeCircuitParts.Count / (float)circuitParts.Count;
                    break;
                case ActivationModes.AnyActive:
                    isComplete = activeCircuitParts.Count > 0;
                    Percent = isComplete ? 1 : 0;
                    break;
                case ActivationModes.AtLeast:
                    isComplete = activeCircuitParts.Count >= partsNeeded;
                    Percent = activeCircuitParts.Count / (float)partsNeeded;
                    break;
                
                case ActivationModes.AtMost:
                    isComplete = activeCircuitParts.Count <= partsNeeded;
                    Percent = (partsNeeded - activeCircuitParts.Count) / (float)partsNeeded;
                    break;
            }
            
            if(!sendEvent) return;
            
            CircuitPercentageUpdated?.Invoke(Percent);

            if (wasComplete == isComplete) return;
            CircuitCompleted?.Invoke(isComplete);
        }
        bool UpdatePartList(CircuitPart part)
        {
            bool partWasInList = activeCircuitParts.Contains(part);
                    
            //If part is active, but it was already in list, exit
            if (part.active)
            {
                if (partWasInList) return false;
                activeCircuitParts.Add(part);
            }
            else if (partWasInList)
            {
                activeCircuitParts.Remove(part);
            }

            return true;
        }

        //Event Receivers
        void OnCircuitPartActivated(CircuitPart part)
        {
            if(!enabled) return;
            
            CheckPartActivation(part);
        }
    }
}