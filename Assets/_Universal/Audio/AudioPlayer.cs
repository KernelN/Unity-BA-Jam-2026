using UnityEngine;

namespace Universal.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip[] clips;

        public void TryPlay()
        {
            if(audioSource.isPlaying) return;
            Play();
        }
        public void Play() => audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        public void SetLooping(bool play)
        {
            audioSource.loop = play;
            audioSource.clip = clips[Random.Range(0, clips.Length)];
            
            if(play)
                audioSource.Play();
            else 
                audioSource.Stop();
        }
        public void Stop() => audioSource.Stop();
    }
}