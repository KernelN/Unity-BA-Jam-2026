using UnityEngine;

namespace UnityBaJam2026.Gameplay.Props
{
    public class FloorButton : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent<bool> Activated;
        public UnityEngine.Events.UnityEvent<bool> ActivatedInverted;
        int objectsOnTop;

        void OnTriggerEnter(Collider other)
        {
            if(objectsOnTop == 0) OnActivated(true);
            
            objectsOnTop++;
        }
        void OnTriggerExit(Collider other)
        {
            objectsOnTop--;
            
            if(objectsOnTop == 0) OnActivated(false);
        }
        void OnActivated(bool isActivated)
        {
            Activated?.Invoke(isActivated);
            ActivatedInverted?.Invoke(!isActivated);
        }
    }
}