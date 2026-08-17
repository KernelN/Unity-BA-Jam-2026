using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    [System.Serializable]
    public class ReactionBreak : Reaction
    {
        //static readonly int Break = Animator.StringToHash("Break");
        
        [SerializeField] Collider collider;
        [SerializeField] MeshRenderer _renderer;
        [SerializeField] ParticleSystem particleSystem;
        [SerializeField] Universal.Audio.AudioPlayer audioPlayer;
        
        public override void Set(params object[] _params)
        {
            // _renderer = (MeshRenderer)_params[0];
            // collider = (Collider)_params[1];
            // audioSource = (AudioSource)_params[2];
            // particleSystem = (ParticleSystem)_params[3];
        }

        public override void Execute(params object[] _params)
        {
            collider.enabled = false;
            _renderer.enabled = false;
            
            if(particleSystem)
                particleSystem.Play();

            audioPlayer?.TryPlay();
        }
    }
}