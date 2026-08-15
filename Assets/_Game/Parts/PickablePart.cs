using UnityEngine;

namespace UnityBaJam2026.Gameplay.Parts
{
    public class PickablePart : MonoBehaviour
    {
        [SerializeField] PartSettings partSettings;
        [SerializeField] SpriteRenderer spriteRenderer;
        public PartType Type => partSettings.PartType;

        public PartSettings SwapSettings(PartSettings settings)
        {
            PartSettings oldSettings = partSettings;
            partSettings = settings;
            
            spriteRenderer.sprite = settings.PartUI;
            
            return oldSettings;
        }
    }
}