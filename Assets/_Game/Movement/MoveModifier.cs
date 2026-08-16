using System;
using UnityEngine;
using VSController;

namespace UnityBaJam2026.Gameplay.Movement
{
    public class MoveModifier : MonoBehaviour
    {
        [SerializeField] MoveModifierSettings startingSettings;
        
        [SerializeField] CharacterController characterController;
        [SerializeField] FPSController fpsController;
        MoveModifierSettings settings;
        float baseHeight;
        float baseStepOffset;
        
        void Awake()
        {
            if(!settings)
                SetSettings(startingSettings);
            
            baseHeight = characterController.height;
            baseStepOffset = characterController.stepOffset;
        }
        public void SetSettings(MoveModifierSettings settings)
        {
            this.settings = settings;
            
            if(!settings) return;
            
            characterController.includeLayers = settings.ExtraWalkableLayers;
            characterController.stepOffset = baseStepOffset + settings.StepOffsetMod;
            fpsController.SetHeight(baseHeight+settings.heightMod);
        }
    }
}