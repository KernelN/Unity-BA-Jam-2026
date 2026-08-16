#if SAINTSFIELD_SAINTS_EDITOR_NETWORK_BEHAVIOR_APPLY
using Unity.Netcode;
using UnityEditor;

namespace SaintsField.Editor.Playa.NetCode
{
    [CustomEditor(typeof(NetworkBehaviour), true)]
    public class ApplyNetworkBehaviourEditor: SaintsNetworkBehaviourEditor
    {

    }
}
#endif
