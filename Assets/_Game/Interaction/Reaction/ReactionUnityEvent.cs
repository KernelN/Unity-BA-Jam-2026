using UnityEngine;
using UnityEngine.Events;

namespace UnityBaJam2026.Gameplay.Interaction
{
    [System.Serializable]
    public class ReactionUnityEvent : Reaction
    {
        //static readonly int Break = Animator.StringToHash("Break");

        public UnityEvent unityEvent;
        
        public override void Set(params object[] _params)
        {
            // _renderer = (MeshRenderer)_params[0];
        }

        public override void Execute(params object[] _params)
        {
            unityEvent?.Invoke();
        }
    }
}