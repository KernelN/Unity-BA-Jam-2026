using UnityBaJam2026.Gameplay.Parts;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityBaJam2026.Gameplay.Vision
{
    public class VisionModifier : MonoBehaviour
    {
        [SerializeField] VisionModifierSettings startingSettings;
        VisionModifierSettings currentSettings;
        
        [Header("References")]
        [SerializeField] Camera playerCamera;
        PostProcessingManager postProcessingManager;
        
        [Header("Blind Effect")]
        [SerializeField] Volume blindVolume;
        [SerializeField] float goBlindTime = .1f;
        [SerializeField] float blindTime = .1f;
        [SerializeField] float exitBlindTime = .1f;

        void Start()
        {
            if(currentSettings == null) SetSettings(startingSettings);
        }
        public async void SetSettings(VisionModifierSettings settings, bool smoothSet = true)
        {
            currentSettings = settings;
            
            if(!currentSettings) return;
            
            if(!postProcessingManager)
                postProcessingManager = PostProcessingManager.inst;

            if(smoothSet)
                await GoBlind();
            
            playerCamera.cullingMask = settings.RenderingLayers;
            postProcessingManager.SetVolume(settings.SurfaceVision, VolumeType.Surface);
            postProcessingManager.SetVolume(settings.WaterVision, VolumeType.Water);
            postProcessingManager.SetVolume(settings.BloodVision, VolumeType.Blood);

            if(smoothSet)
                await ExitBlind();
        }
        async Awaitable GoBlind()
        {
            float timer = 0;
            while (timer < goBlindTime)
            {
                timer += Time.deltaTime;
                blindVolume.weight = Mathf.Lerp(0, 1, timer / goBlindTime);
                await Awaitable.NextFrameAsync();
            }
        }
        async Awaitable ExitBlind()
        {
            await Awaitable.WaitForSecondsAsync(blindTime);

            float timer = 0;
            while (timer < exitBlindTime)
            {
                timer += Time.deltaTime;
                blindVolume.weight = Mathf.Lerp(1, 0, timer / exitBlindTime);
                await Awaitable.NextFrameAsync();
            }
        }
    }
}