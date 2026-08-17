using UnityEngine;

namespace UnityBaJam2026.Gameplay.Circuit
{
    public class CircuitPartTimer : MonoBehaviour
    {
        [SerializeField] CircuitPart circuitPart;
        [SerializeField] float timeToTurnOff = 10f;
        float timer;
        
        void Start()
        {
            circuitPart.Activated += OnCircuitActivated;
            timer = timeToTurnOff;
        }
        void Update()
        {
            if(timer >= timeToTurnOff) return;
            
            timer += Time.deltaTime;
            
            if(timer >= timeToTurnOff) circuitPart.TrySetActive(false);
        }
        void OnCircuitActivated(CircuitPart circuit)
        {
            if (!circuit.IsActive)
            {
                timer = timeToTurnOff;
                return;
            }
            
            timer = 0;
        }
    }
}