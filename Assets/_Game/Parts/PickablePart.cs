using System;
using UnityEngine;

namespace UnityBaJam2026.Gameplay.Parts
{
    public class PickablePart : MonoBehaviour
    {
        [SerializeField] PartSettings partSettings;
        [SerializeField] SpriteRenderer spriteRenderer;
        public PartType Type => partSettings.PartType;

        void Awake()
        {
            spriteRenderer.sprite = partSettings.PartUI;
        }

        public PartSettings SwapSettings(PartSettings settings)
        {
            if (!settings && partSettings)
            {
                Destroy(gameObject);
                return partSettings;
            }
            
            PartSettings oldSettings = partSettings;
            partSettings = settings;
            
            spriteRenderer.sprite = settings.PartUI;
            
            return oldSettings;
        }
    }
}