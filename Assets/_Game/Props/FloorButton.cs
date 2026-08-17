using UnityEngine;

namespace UnityBaJam2026.Gameplay.Props
{
    public class FloorButton : MonoBehaviour
    {
        public UnityEngine.Events.UnityEvent<bool> Activated;
        int objectsOnTop;

        void OnTriggerStay(Collider other)
        {
            if(objectsOnTop == 0) Activated?.Invoke(true);
            
            objectsOnTop++;
        }
        void OnTriggerExit(Collider other)
        {
            objectsOnTop--;
            
            if(objectsOnTop == 0) Activated?.Invoke(false);
        }
    }
}