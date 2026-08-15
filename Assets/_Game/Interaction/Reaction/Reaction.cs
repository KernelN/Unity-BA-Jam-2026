using UnityEngine;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public abstract class Reaction
    {
        public abstract void Set(params object[] _params);
        public abstract void Execute(params object[] _params);
    }
}