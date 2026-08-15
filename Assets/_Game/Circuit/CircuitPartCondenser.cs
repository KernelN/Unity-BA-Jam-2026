using UnityEngine;

namespace UnityBaJam2026.Gameplay.Circuit
{
    public class CircuitPartCondenser : CircuitPart
    {
        [SerializeField] CircuitPart[] parts;
        //[Header("Runtime Values")]
        int activeCount;

        //Unity Events
        void Awake()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Activated += (b) =>
                {
                    if (b.isActive)
                    {
                        activeCount++;
                        if (activeCount > parts.Length) activeCount = parts.Length;
                    }
                    else
                    {
                        activeCount--;
                        if (activeCount < 0) activeCount = 0;
                    }
                    
                    TrySetActive(activeCount == parts.Length);
                };
            }
        }
    }
}