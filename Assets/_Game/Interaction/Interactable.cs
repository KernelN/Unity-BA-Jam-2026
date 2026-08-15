using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public class Interactable : MonoBehaviour
    {
        public List<InteractionTag> validInteractionTags;

        Dictionary<InteractionTag, Reaction> reactions;
        
        void Awake()
        {
            reactions = new Dictionary<InteractionTag, Reaction>();
        }
        public void SetReaction(Reaction reaction, InteractionTag interactionTag)
        {
            reactions.TryAdd(interactionTag, reaction);
        }
        public void GetInteracted(InteractionTag[] interactionTags)
        {
            for (var i = 0; i < interactionTags.Length; i++)
                if (validInteractionTags.Contains(interactionTags[i]))
                {
                    reactions.TryGetValue(interactionTags[i], out Reaction reaction);
                    if(reaction != null) reaction.Execute();
                    return;
                }
        }
    }
}