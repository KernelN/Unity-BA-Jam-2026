using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Universal.UI
{
    public class GameFlowButton : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] SceneAsset sceneToChange;
        void OnValidate()
        {
            if(sceneToChange)
                sceneName = sceneToChange.name;
        }
#endif
        
        [SerializeField] string sceneName;
        [Tooltip("Only applies to 'Change with Delay'")]
        [SerializeField] float sceneLoadDelay = 1;
        public void ChangeScene()
        {
            if(Time.timeScale == 0) Time.timeScale = 1;
            SceneManager.LoadSceneAsync(sceneName);
        }
        public void ChangeSceneWithDelay() 
            => Invoke(nameof(ChangeScene), sceneLoadDelay);

        public void Quit() => Application.Quit();

        public void Pause() => Time.timeScale = Time.timeScale == 0 ? 1 : 0;
    }
}