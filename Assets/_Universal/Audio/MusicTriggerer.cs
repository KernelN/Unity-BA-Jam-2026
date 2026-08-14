using UnityEngine;

namespace Universal.Audio
{
    public class MusicTriggerer : MonoBehaviour
    {
        [SerializeField] AudioClip track;

        void Start() => MusicManager.inst?.RequestTrack(track);
    }
}