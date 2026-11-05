using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Universal.UI;
using VSController;

namespace UnityBaJam2026.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] FPSController player;
        public UnityEvent onGameOver = new UnityEvent();
        
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
    }
}