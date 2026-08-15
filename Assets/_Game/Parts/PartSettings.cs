using Unity.VisualScripting;
using UnityBaJam2026.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityBaJam2026.Gameplay.Parts
{
    public enum PartType { Eye, Arm, Leg }
    [CreateAssetMenu(fileName = "PartSettings", menuName = "Scriptable Objects/PartSettings")]
    public class PartSettings : ScriptableObject
    {
        [SerializeField] PartType type;
        [SerializeField] Sprite partUI;
        [SerializeField] PartInnerSettings innerSettings;

        public PartType PartType => type;
        public Sprite PartUI => partUI;
        public PartInnerSettings InnerSettings => innerSettings;
    }

    public abstract class PartInnerSettings : ScriptableObject { }
}