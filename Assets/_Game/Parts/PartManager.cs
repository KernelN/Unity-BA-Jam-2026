using UnityEngine;
using UnityEngine.UI;

namespace UnityBaJam2026.Gameplay.Parts
{
    public class PartManager : MonoBehaviour
    {
        [Header("Arm")]
        [SerializeField] Interaction.Interactor interactor;
        [SerializeField] PartSettings armSettings;
        [SerializeField] SaintsField.SaintsDictionary<PartSettings, GameObject> arms;
        GameObject currentArm;

        [Header("Leg")]
        [SerializeField] Movement.MoveModifier moveModifier;
        [SerializeField] PartSettings legSettings;
        
        [Header("Pick Up")]
        [SerializeField] float reach;
        [SerializeField] LayerMask rayLayers;
        
        [Header("UI")]
        [SerializeField] Image armUI;
        [SerializeField] Image legUI;
        [SerializeField] Image partPickerUI;
       
        void Awake()
        {
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