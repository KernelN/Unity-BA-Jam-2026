using SaintsField.ScriptableRenderer;
using UnityEngine;

namespace SaintsField.Samples.Scripts.ScriptableRenderer
{
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_MENU_ITEM
    [CreateAssetMenu(
        fileName = "ScriptableRendererDataTarget",
        menuName = "SaintsField Debug/ScriptableRendererDataTarget")]
#endif
    public class ScriptableRendererDataTarget: SaintsScriptableRendererData
    {
        protected override UnityEngine.Rendering.Universal.ScriptableRenderer Create()
        {
            return new ScriptableRendererExample(this);
        }
    }
}
