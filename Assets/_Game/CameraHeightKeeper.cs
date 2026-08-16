using System;
using UnityEngine;

namespace UnityBaJam2026.Gameplay
{
    public class CameraHeightKeeper : MonoBehaviour
    {
        [SerializeField] CharacterController characterController;
        float originalOffset;
        
        void Awake()
        {
            originalOffset = transform.position.y - characterController.bounds.max.y;
        }

        void Update()
        {
            Vector3 cameraTarget = characterController.transform.position;
            cameraTarget.y = characterController.bounds.max.y + originalOffset;
            transform.position = cameraTarget;
        }
    }
}