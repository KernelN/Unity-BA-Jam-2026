using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Universal.UI;
using VSController;

namespace UnityBaJam2026.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] FPSController player;
        public UnityEvent onGameOver = new UnityEvent();

        [SerializeField] PlayerInput playerInput;
        [SerializeField] GameObject debugConsole;

        void Awake()
        {
#if  UNITY_EDITOR || DEVELOPMENT_BUILD
            InputAction debug = playerInput.actions.FindAction("Debug");
            if (debug != null)
                debug.performed += ctx => OnDebugConsole();
            else Debug.LogWarning("Debug action not found");
#endif
        }

        public void OnGameOver()
        {
            player.enabled = false;
            onGameOver?.Invoke();
        }

        public void OnExitMenuReady()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        
#if  UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnDebugConsole() => debugConsole.SetActive(!debugConsole.activeSelf);
#endif
    }
}