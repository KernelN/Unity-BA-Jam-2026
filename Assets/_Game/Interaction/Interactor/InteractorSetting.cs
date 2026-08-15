using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    [CreateAssetMenu(fileName = "InteractorSetting", menuName = "Scriptable Objects/InteractorSetting")]
    public class InteractorSetting : Parts.PartInnerSettings
    {
        [Header("Interaction")]
        public InteractionTag tag;
        public float reach;
        public LayerMask rayLayers;
        [Header("UI")]
        public Sprite interactUI;
    }
}