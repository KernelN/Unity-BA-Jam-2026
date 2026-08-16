using SaintsField.Playa;
using UnityEngine;

namespace SaintsField.Samples.Scripts.AnimBehav
{
    [HelpURL("https://saintsfield.comes.today")]
    public class AnimBehaveExample : StateMachineBehaviour
    {
        [LayoutStart("Tabs", ELayout.Tab)]
        [LayoutStart("./I'll")] public string f1;
        [LayoutStart("../Follow")] public string f2;
        [LayoutStart("../You")] public string f3;
        [LayoutStart("../Into")] public string f4;
        [LayoutStart("../The")] public string f5;
        [LayoutStart("../Dark")] public string f6;
    }
}
