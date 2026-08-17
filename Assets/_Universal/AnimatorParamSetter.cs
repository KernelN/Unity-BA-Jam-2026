using UnityEngine;

namespace Universal.Animation
{
    public class AnimatorParamSetter : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] string paramName;
        
        public void SetParameterName(string parameterName) => paramName = parameterName;
        public void SetBool(bool value) => animator.SetBool(paramName, value);
        public void SetBoolInverted(bool value) => animator.SetBool(paramName, value);
    }
}