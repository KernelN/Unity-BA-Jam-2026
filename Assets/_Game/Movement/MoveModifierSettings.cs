using UnityEngine;

namespace UnityBaJam2026.Gameplay.Movement
{
    [CreateAssetMenu(fileName = "MoveModifierSettings", menuName = "Scriptable Objects/MoveModifierSettings")]
    public class MoveModifierSettings : Parts.PartInnerSettings
    {
        [SerializeField] public float heightMod = 0;
        [SerializeField] public float stepOffsetMod = 0;
        [SerializeField] public LayerMask extraWalkableLayers;
        
        public float HeightMod => heightMod;
        public float StepOffsetMod => stepOffsetMod;
        public LayerMask ExtraWalkableLayers => extraWalkableLayers;
    }
}