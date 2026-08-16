using UnityEngine;
using UnityEngine.Rendering;

namespace UnityBaJam2026.Gameplay.Vision
{
    [CreateAssetMenu(fileName = "VisionModifierSettings", menuName = "Scriptable Objects/VisionModifierSettings")]
    public class VisionModifierSettings : Parts.PartInnerSettings
    {
        [SerializeField] LayerMask renderingLayers = -1;
        [SerializeField] VolumeProfile surfaceVision;
        [SerializeField] VolumeProfile waterVision;
        [SerializeField] VolumeProfile bloodVision;
        
        public LayerMask RenderingLayers => renderingLayers;
        public VolumeProfile SurfaceVision => surfaceVision;
        public VolumeProfile WaterVision => waterVision;
        public VolumeProfile BloodVision => bloodVision;
    }
}