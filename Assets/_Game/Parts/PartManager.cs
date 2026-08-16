using UnityEngine;
using UnityEngine.UI;

namespace UnityBaJam2026.Gameplay.Parts
{
    public class PartManager : MonoBehaviour
    {
        [Header("Pick Up")]
        [SerializeField] float reach;
        [SerializeField] LayerMask rayLayers;
        [SerializeField] Image partPickerUI;
        
        [Header("Eye")]
        [SerializeField] Vision.VisionModifier visionModifier;
        [SerializeField] PartSettings eyeSettings;
        [SerializeField] Image eyeUI;
        
        [Header("Arm")]
        [SerializeField] Interaction.Interactor interactor;
        [SerializeField] PartSettings armSettings;
        [SerializeField] Image armUI;
        [SerializeField] SaintsField.SaintsDictionary<PartSettings, GameObject> arms;
        GameObject currentArm;

        [Header("Leg")]
        [SerializeField] Movement.MoveModifier moveModifier;
        [SerializeField] PartSettings legSettings;
        [SerializeField] Image legUI;
       
        void Awake()
        {
            visionModifier.SetSettings((Vision.VisionModifierSettings)eyeSettings.InnerSettings, false);
            eyeUI.sprite = eyeSettings.PartUI;
            
            if (arms.TryGetValue(armSettings, out var arm))
            {
                currentArm = arm;
                currentArm.SetActive(true);
                interactor.SetInteractor((Interaction.InteractorSetting)armSettings.InnerSettings);
                armUI.sprite = armSettings.PartUI;
            }
            
            moveModifier.SetSettings((Movement.MoveModifierSettings)legSettings.InnerSettings);
            legUI.sprite = legSettings.PartUI;
        }
        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach, rayLayers))
            {
                partPickerUI.gameObject.SetActive(false);
                return;
            }

            if (!hit.transform.TryGetComponent(out PickablePart part))
            {
                partPickerUI.gameObject.SetActive(false);
                return;
            }
            
            partPickerUI.gameObject.SetActive(true);
            
            if(Input.GetKeyDown(KeyCode.E))
            {
                switch (part.Type)
                {
                    case PartType.Eye:
                        eyeSettings = part.SwapSettings(eyeSettings);
                        visionModifier.SetSettings((Vision.VisionModifierSettings)eyeSettings.InnerSettings);
                        eyeUI.sprite = eyeSettings.PartUI;
                        break;
                    case PartType.Arm:
                       armSettings = part.SwapSettings(armSettings); 
                       interactor.SetInteractor((Interaction.InteractorSetting)armSettings.InnerSettings);
                       armUI.sprite = armSettings.PartUI;
                       
                       //swap arm
                       currentArm.SetActive(false);
                       currentArm = arms[armSettings];
                       currentArm.SetActive(true);
                       break;
                    case PartType.Leg:
                        legSettings = part.SwapSettings(legSettings);
                        moveModifier.SetSettings((Movement.MoveModifierSettings)legSettings.InnerSettings);
                        legUI.sprite = legSettings.PartUI;
                        break;
                }
            }
        }
    }
}