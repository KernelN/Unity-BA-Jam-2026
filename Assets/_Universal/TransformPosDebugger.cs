using System;
using TMPro;
using UnityEngine;

namespace Universal.Debug
{
    public class TransformPosDebugger : MonoBehaviour
    {
        [SerializeField] Transform trackedTransform;
        [SerializeField] TextMeshProUGUI text;

        void Update()
        {
            text.text = trackedTransform.position.ToString("N0");
        }
    }
}