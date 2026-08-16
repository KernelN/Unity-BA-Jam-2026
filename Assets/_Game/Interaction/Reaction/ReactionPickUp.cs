using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    [System.Serializable]
    public class ReactionPickUp : Reaction
    {
        //static readonly int Break = Animator.StringToHash("Break");

        [SerializeField] InteractorSetting pickableInteractorSettings;
        [SerializeField] Rigidbody objectRigidbody;
        
        public override void Set(params object[] _params)
        {
            // _renderer = (MeshRenderer)_params[0];
        }

        public override void Execute(params object[] _params)
        {
            ((Interactor)_params[0]).PickUp(objectRigidbody, pickableInteractorSettings);
        }
    }
}