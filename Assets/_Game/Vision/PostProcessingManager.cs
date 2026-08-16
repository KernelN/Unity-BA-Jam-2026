using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityBaJam2026.Gameplay.Vision
{
    public enum VolumeType {Surface, Water, Blood}
    
    public class PostProcessingManager : Universal.Singleton<PostProcessingManager>
    {
        public Dictionary<VolumeType, VolumeProfile> profiles { get; private set; }
        
        public UnityEvent<VolumeType, VolumeProfile> onVolumeChanged;

        protected override void Awake()
        {
            base.Awake();
            if(inst != this) return;
            
            profiles = new Dictionary<VolumeType, VolumeProfile>();
        }

        public void SetVolume(VolumeProfile settingsSurfaceVision, VolumeType type)
        {
            if (profiles.TryGetValue(type, out VolumeProfile profile))
            {
                //If profile was already set, skip
                if(settingsSurfaceVision == profile) return;
                
                profiles[type] = profile;
            }
            else profiles.Add(type, settingsSurfaceVision);
            
            onVolumeChanged?.Invoke(type,settingsSurfaceVision);
        }
    }
}