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
            baseHeight = characterController.height;
            baseStepOffset = characterController.stepOffset;
            
            if(!settings)
                SetSettings(startingSettings);
        }
        public void SetSettings(MoveModifierSettings settings)
        {
            this.settings = settings;
            
            if(!settings) return;
            
            characterController.includeLayers = settings.ExtraWalkableLayers;
            fpsController.SetHeight(baseHeight+settings.heightMod);
            characterController.stepOffset = baseStepOffset + settings.StepOffsetMod;
        }
    }
}