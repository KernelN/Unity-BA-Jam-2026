using System;
using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public class InteractableShowcase : MonoBehaviour
    {
        [SerializeField] Interactable interactable;
        [SerializeField] ReactionBreak onBreak;
        [SerializeField] ReactionBreak onGrab;
        [SerializeField] ReactionBreak onPress;

        void Awake()
        {
            interactable.SetReaction(onBreak, InteractionTag.Break);
            interactable.SetReaction(onGrab, InteractionTag.Grab);
            interactable.SetReaction(onPress, InteractionTag.Press);
        }
    }
}