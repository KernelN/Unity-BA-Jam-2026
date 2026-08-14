/////////////////////////////////////////////////////////////////////////////////
//
//	PickupIteam.cs
//
//	Description:	setting up object that can take and place.
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace VSController
{
    public class CollectableItem : MonoBehaviour
    {
        [Header("Prefab that will spawn when placing")]
        public GameObject prefabToSpawn;

        [Header("Actions")]
        public List<Action> PickUpActions = new();
        public List<Action> PlaceActions = new();

        [System.Serializable]
        public class Action
        {
            [Tooltip("Filter (array works only in object wich have this tag)")]
            public string requiredTag;
            public float delay;

            [Header("Transform")]
            public Vector3 vectorValue;
            public Vector3 scaleValue;

            [Space(5)]

            [Tooltip("World - works in global coordinates, Local - adds to existing")]
            public Space space = Space.Self;

            [Tooltip("Lerp needed for smooth playback actions")]
            public bool useLerp;
            public float duration = 1f;

            [Header("Audio")]
            public AudioClip audioClip;
            public AudioMixerGroup output;

            [Header("Animation")]
            public AnimationClip animationClip;

            [Space(10)]
            public Material[] materials;

            [Header("Custom")]
            public UnityEvent unityEvent;
        }

        // Set parameters when adding script
        public void InitList(Vector3 startScale)
        {
            // If no arrays, add them
            if (PickUpActions == null || PickUpActions.Count == 0)
            {
                PickUpActions = new List<Action>
        {
            new Action { scaleValue = startScale }
        };
            }

            if (PlaceActions == null || PlaceActions.Count == 0)
            {
                PlaceActions = new List<Action>
        {
            new Action { scaleValue = startScale }
        };
            }
        }

        // Start action
        public static IEnumerator RunActions(MonoBehaviour runner, List<Action> actions, GameObject context, GameObject target)
        {
            if (actions == null || context == null)
                yield break;

            Transform t = context.transform;
            Renderer r = context.GetComponentInChildren<Renderer>();
            Animation anim = context.GetComponent<Animation>();

            foreach (var a in actions)
            {
                if (a == null) continue;

                // Define tag
                if (!string.IsNullOrEmpty(a.requiredTag) && (target == null || !target.CompareTag(a.requiredTag)))
                    continue;

                // Define delay
                if (a.delay > 0)
                    yield return new WaitForSeconds(a.delay);

                float maxDuration = 0f;

                // MOVE
                if (a.vectorValue != Vector3.zero)
                {
                    if (a.useLerp)
                    {
                        runner.StartCoroutine(LerpMove(t, a));
                        maxDuration = Mathf.Max(maxDuration, a.duration);
                    }
                    else
                    {
                        if (a.space == Space.World)
                            t.position = a.vectorValue;
                        else
                            t.localPosition += a.vectorValue;
                    }
                }

                // SCALE
                if (a.scaleValue != t.localScale)
                {
                    if (a.useLerp)
                    {
                        runner.StartCoroutine(LerpScale(t, a));
                        maxDuration = Mathf.Max(maxDuration, a.duration);
                    }
                    else
                    {
                        t.localScale = a.scaleValue;
                    }
                }

                // AUDIO
                if (a.audioClip != null)
                {
                    GameObject go = new GameObject("OneShotAudio");
                    go.transform.position = t.position;

                    AudioSource src = go.AddComponent<AudioSource>();
                    src.clip = a.audioClip;
                    src.spatialBlend = 1f;

                    if (a.output != null)
                        src.outputAudioMixerGroup = a.output;

                    src.Play();
                    Object.Destroy(go, a.audioClip.length);
                }

                // MATERIAL
                if (r != null && a.materials != null && a.materials.Length > 0)
                {
                    var mats = r.materials;
                    int count = Mathf.Min(mats.Length, a.materials.Length);

                    for (int i = 0; i < count; i++)
                    {
                        if (a.materials[i] != null)
                            mats[i] = a.materials[i];
                    }

                    r.materials = mats;
                }

                // ANIMATION
                if (a.animationClip != null)
                {
                    if (anim == null)
                        anim = context.AddComponent<Animation>();

                    a.animationClip.legacy = true;

                    string clipName = a.animationClip.name;

                    if (anim.GetClip(clipName) == null)
                        anim.AddClip(a.animationClip, clipName);

                    anim.clip = a.animationClip;
                    anim.Play(clipName);

                    maxDuration = Mathf.Max(maxDuration, a.animationClip.length);
                }

                a.unityEvent?.Invoke();

                if (maxDuration > 0)
                    yield return new WaitForSeconds(maxDuration);
            }
        }

        // Produces move during some period of time
        private static IEnumerator LerpMove(Transform t, Action a)
        {
            if (t == null) yield break;

            Vector3 start = a.space == Space.World ? t.position : t.localPosition;
            Vector3 end = a.space == Space.World
                ? a.vectorValue
                : start + a.vectorValue;

            float time = 0;

            while (time < a.duration)
            {
                if (t == null) yield break;

                time += Time.deltaTime;
                Vector3 v = Vector3.Lerp(start, end, time / a.duration);

                if (t == null) yield break;

                if (a.space == Space.World)
                    t.position = v;
                else
                    t.localPosition = v;

                yield return null;
            }

            if (t != null)
            {
                if (a.space == Space.World)
                    t.position = end;
                else
                    t.localPosition = end;
            }
        }

        // Produces scale during some period of time
        private static IEnumerator LerpScale(Transform t, Action a)
        {
            if (t == null) yield break;

            Vector3 start = t.localScale;
            Vector3 end = a.scaleValue;

            float time = 0;

            while (time < a.duration)
            {
                if (t == null) yield break;

                time += Time.deltaTime;
                t.localScale = Vector3.Lerp(start, end, time / a.duration);

                yield return null;
            }

            if (t != null)
                t.localScale = end;
        }

        private class Runner : MonoBehaviour { }
        private static Runner runner;

        // Check Runner status
        private static Runner GetRunner()
        {
            if (runner != null) return runner;

            var go = new GameObject("[PickupActionRunner]");
            DontDestroyOnLoad(go);

            runner = go.AddComponent<Runner>();
            return runner;
        }

        // This method is called from Grabbing.cs to start action
        public static void Run(List<Action> actions, GameObject context, GameObject target, System.Action onComplete = null)
        {
            GetRunner().StartCoroutine(RunRoutine(actions, context, target, onComplete));
        }

        // Prepare RunActions()
        private static IEnumerator RunRoutine(List<Action> actions, GameObject context, GameObject target, System.Action onComplete)
        {
            yield return RunActions(GetRunner(), actions, context, target);
            onComplete?.Invoke();
        }
    }
}
