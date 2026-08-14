using System;
using UnityEngine;

namespace Universal
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static T inst { get; private set; }
        /// <summary>
        /// False by default.
        /// </summary>
        protected virtual bool DoNotDestroyOnLoad => false;

        [SerializeField] bool destroyGameObject;

        protected virtual void Awake()
        {
            if (inst != null)
            {
                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroyImmediate(this);
                return;
            }

            inst = this as T;
            
            if (DoNotDestroyOnLoad && Application.isPlaying)
                DontDestroyOnLoad(this);
        }
        protected virtual void OnDestroy()
        {
            if (inst == this) inst = null;
            
            if (destroyGameObject && gameObject)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }
    }
}