using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Playables;

namespace SaintsField.Samples.Scripts.PlayableExposedReference
{
#if SAINTSFIELD_DEBUG && SAINTSFIELD_DEBUG_MENU_ITEM
    [CreateAssetMenu(
        fileName = "ExposedReferencePlayable",
        menuName = "SaintsField Debug/Exposed Reference Playable")]
#endif
    public class ExposedReferencePlayableAsset : PlayableAsset
    {
        // Yeah if you have SaintsEditor enabled, this works just fine
        [LayoutStart("So layout is fine?", ELayout.Horizontal)]
        public int i1;
        public int i2;
        public ExposedReference<GameObject> exposedReference;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Create(graph);
        }
    }
}
