using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    [System.Serializable]
    public class ReactionCircuit : Reaction
    {
        //static readonly int Break = Animator.StringToHash("Break");

        [SerializeField] Circuit.CircuitPart circuit;
        [SerializeField] bool turnOn = true;
        
        public override void Set(params object[] _params)
        {
            // _renderer = (MeshRenderer)_params[0];
        }

        public override void Execute(params object[] _params)
        {
            circuit.TrySetActive(turnOn);
        }
    }
}