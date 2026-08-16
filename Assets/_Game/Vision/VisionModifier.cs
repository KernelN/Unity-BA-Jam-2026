using System;
using UnityBaJam2026.Gameplay.Vision;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityBaJam2026.Gameplay
{
    public class VisionModifier : MonoBehaviour
    {
        [SerializeField] VisionModifierSettings startingSettings;
        VisionModifierSettings currentSettings;
        
        [Header("References")]
        [SerializeField] Camera playerCamera;
        [SerializeField] PostProcessingManager postProcessingManager;
        
        [Header("Blind Effect")]
        [SerializeField] Volume surfaceVision;
        [SerializeField] float minVignette;
        float maxVignette;

        public async void SetSettings(VisionModifierSettings settings)
        {
            currentSettings = settings;
            
            if(!currentSettings) return;

            await GoBlind();
            
            playerCamera.cullingMask = settings.RenderingLayers;
            postProcessingManager.SetVolume(settings.SurfaceVision, VolumeType.Surface);
        }
        async Awaitable GoBlind()
        {
            
        }
    }
}