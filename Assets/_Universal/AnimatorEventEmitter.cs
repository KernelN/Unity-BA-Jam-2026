using UnityEngine;
using UnityEngine.Events;

namespace Universal.Animation
{
    public class AnimatorEventEmitter : MonoBehaviour
    {
        public UnityEvent<string> AnimationEventPlayed;
        
        public void OnAnimationEvent(string eventName)
        {
            AnimationEventPlayed?.Invoke(eventName);
        }
    }
}