/////////////////////////////////////////////////////////////////////////////////
//
//	SoundData.cs
//
//	Description:	creates a scriptable object that stores the sounds of
//	                steps and jumps.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

namespace VSController
{
    [CreateAssetMenu(fileName = "NewSoundData", menuName = "VS Controller/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [System.Serializable]
        public class SurfaceSound
        {
            [Header("Surface Settings (not all fields are required)")]
            [Tooltip("Surface tag (optional)")]
            public string surfaceTag;

            [Tooltip("Surface materials (optional)")]
            public Material[] surfaceMaterials;

            [Tooltip("Surface textures (optional)")]
            public Texture[] surfaceTextures;

            [Header("Step Sounds")]
            [Tooltip("Array of step sounds for this surface")]
            public AudioClip[] stepSounds;
        }

        [Header("Default Step Sounds (used if no surface matches)")]
        public AudioClip[] defaultSteps;

        [Header("Jump Sounds")]
        public AudioClip[] jumpSounds;

        public List<SurfaceSound> surfaceSounds;

        public AudioClip GetRandomStepSound(string surfaceTag, Material surfaceMaterial, Texture surfaceTexture, ref int lastIndex)
        {
            AudioClip[] clips = GetStepSoundsForSurface(surfaceTag, surfaceMaterial, surfaceTexture);
            if (clips.Length == 0) return null;

            int index;
            do
            {
                index = Random.Range(0, clips.Length);
            }
            while (clips.Length > 1 && index == lastIndex);

            lastIndex = index;
            return clips[index];
        }

        public AudioClip GetRandomJumpSound()
        {
            if (jumpSounds == null || jumpSounds.Length == 0) return null;
            return jumpSounds[Random.Range(0, jumpSounds.Length)];
        }

        private AudioClip[] GetStepSoundsForSurface(string tag, Material material, Texture texture)
        {
            // 1) Priority: Surface Tag
            if (!string.IsNullOrEmpty(tag) && tag != "Untagged")
            {
                foreach (var surface in surfaceSounds)
                {
                    if (!string.IsNullOrEmpty(surface.surfaceTag) && surface.surfaceTag == tag && surface.stepSounds.Length > 0)
                    {
                        return surface.stepSounds;
                    }
                }
            }

            // 2) Priority: Surface Material
            if (material != null)
            {
                foreach (var surface in surfaceSounds)
                {
                    if (surface.surfaceMaterials != null && surface.surfaceMaterials.Length > 0)
                    {
                        foreach (var mat in surface.surfaceMaterials)
                        {
                            if (mat != null && mat == material && surface.stepSounds.Length > 0)
                            {
                                return surface.stepSounds;
                            }
                        }
                    }
                }
            }

            // 3) Priority: Surface Texture
            if (texture != null)
            {
                foreach (var surface in surfaceSounds)
                {
                    if (surface.surfaceTextures != null && surface.surfaceTextures.Length > 0)
                    {
                        foreach (var tex in surface.surfaceTextures)
                        {
                            if (tex != null && tex == texture && surface.stepSounds.Length > 0)
                            {
                                return surface.stepSounds;
                            }
                        }
                    }
                }
            }

            // Default step sounds if nothing matches
            return defaultSteps;
        }
    }
}

