using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Universal.UI
{
    public class FadeController : MonoBehaviour
    {
        [SerializeField] bool startOff = true;
        [SerializeField] float fadeInTime = 1f;
        [SerializeField] float fadeOutTime = 1f;
        [SerializeField] CanvasGroup canvasGroup;
        Coroutine cTask;
        bool isOn;

        /// <summary>
        /// Returns when it completed a fade.
        /// true if fades in | false if fade out
        /// </summary>
        public UnityEvent<bool> completedFade;

        void Awake()
        {
            isOn = !startOff;
            if (startOff)
                canvasGroup.alpha = 0;
        }

        public void FadeIn(bool forceFade = false)
        {
            //Debug.Log("Fader " + (isOn ? "On" : "Off") + "; fading in");
            if(!forceFade && isOn) return;
            
            gameObject.SetActive(true);
            
            if (cTask != null) StopCoroutine(cTask);
            cTask = StartCoroutine(FadeTask(false, fadeInTime));
        }
        public void FadeOut(bool forceFade = false)
        {
            //Debug.Log("Fader " + (isOn ? "On" : "Off") + "; fading out");
            if(!forceFade && !isOn) return;
            
            if (cTask != null) StopCoroutine(cTask);
            cTask = StartCoroutine(FadeTask(true, fadeOutTime));
        }

        IEnumerator FadeTask(bool fadeOut, float time, bool useTimeScale = true)
        {
            float cAlpha = canvasGroup.alpha;
            float tAlpha;
            float timer;
            
            if (fadeOut)
            {
                tAlpha = 0;
                timer = time * (1 - cAlpha); //if cAlpha is .25f, timer is already 75% completed
            }
            else
            {
                tAlpha = 1;
                timer = time * cAlpha; //if cAlpha is .25f, timer is already 25% completed 
            }
            
            do
            {
                timer += (useTimeScale ? Time.deltaTime : Time.unscaledDeltaTime);
                float t = timer / time;

                canvasGroup.alpha = Mathf.Lerp(cAlpha, tAlpha, t);
                yield return null;
            } while (timer < time);

            isOn = !fadeOut;
            completedFade?.Invoke(isOn);
            if(!isOn) gameObject.SetActive(false);
            
            cTask = null;
        }
    }
}