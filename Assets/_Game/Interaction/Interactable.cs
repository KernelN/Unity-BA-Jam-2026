using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    //Depends on "https://github.com/mackysoft/Unity-SerializeReferenceExtensions" for reactions
    public class Interactable : MonoBehaviour
    {
        [System.Serializable]
        public class InteractionReactions
        {
            [HideInInspector] public string name;
            public InteractionTag tag;
            [SerializeReference, SubclassSelector]
            public List<Reaction> reactions;
        }
        
        public List<InteractionReactions> interactions;

        Dictionary<InteractionTag, List<Reaction>> reactionsPerInteraction;
        
        void Awake()
        {
            reactionsPerInteraction = new Dictionary<InteractionTag, List<Reaction>>();
        }

        void OnValidate()
        {
            foreach(var interaction in interactions)
                interaction.name = interaction.tag.ToString();
        }

        public void GetInteracted(InteractionTag[] interactionTags)
        {
            for (var i = 0; i < interactionTags.Length; i++)
                if (interactions.Any(interaction => interaction.tag == interactionTags[i]))
                {
                    reactionsPerInteraction.TryGetValue(interactionTags[i], 
                                                            out List<Reaction> reactionList);
                    if(reactionList != null) 
                        foreach(var reaction in reactionList)
                            reaction.Execute();
                    return;
                }
        }
        public void SetReaction(InteractionTag interactionTag, Reaction reaction)
        {
            if(reactionsPerInteraction.TryGetValue(interactionTag, out List<Reaction> reactionList))
                reactionList.Add(reaction);
            else
                reactionsPerInteraction.Add(interactionTag, new List<Reaction> {reaction});
        }
    }
}