using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityBaJam2026.Gameplay.Vision
{
    public class PostProcessSelector : MonoBehaviour
    {
        [SerializeField] VolumeType volumeType;
        [SerializeField] Volume volume;
        
        void Start()
        {
            PostProcessingManager manager = PostProcessingManager.inst;
            manager.onVolumeChanged.AddListener(OnVolumeChanged);
            manager.profiles.TryGetValue(volumeType, out var volumeProfile);
            volume.profile = volumeProfile;
        }

        void OnVolumeChanged(VolumeType type, VolumeProfile newVolume)
        {
            if(type != volumeType) return;
            
            volume.profile = newVolume;
        }
    }
}