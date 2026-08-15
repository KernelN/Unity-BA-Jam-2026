using UnityEngine;

namespace UnityBaJam2026.Gameplay
{
    public class SpriteBillboard : MonoBehaviour
    {
        void Update()
        {
            Vector3 camPos = Camera.main.transform.position;
            camPos.y = transform.position.y;
            transform.LookAt(camPos);
        }
    }
}