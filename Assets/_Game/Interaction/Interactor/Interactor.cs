using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] InteractorSetting startingInteractor;
        InteractorSetting interactorSetting;
        float reach;
        LayerMask rayLayers;
        
        [Header("UI")]
        [SerializeField] Image interactorUI;
        [SerializeField] Color interactorUIColor = Color.gray;
        [SerializeField] Color interactorUIActiveColor = Color.white;
       
        void Awake()
        {
            SetInteractor(startingInteractor);
        }
        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach, rayLayers))
            {
                interactorUI.gameObject.SetActive(false);
                return;
            }

            if (!hit.transform.TryGetComponent(out Interactable interactable))
            {
                interactorUI.gameObject.SetActive(false);
                return;
            }
            
            if(interactable.validInteractionTags.Contains(interactorSetting.tag))
                interactorUI.color = interactorUIActiveColor;
            else 
                interactorUI.color = interactorUIColor;
            
            interactorUI.gameObject.SetActive(true);
            if(Input.GetMouseButtonDown(0))
                interactable.GetInteracted(new[] { interactorSetting.tag });
        }
        
        public void SetInteractor(InteractorSetting interactor)
        {
            interactorSetting = interactor;
            reach = interactor.reach;
            rayLayers = interactor.rayLayers;
            interactorUI.sprite = interactor.interactUI;
        }
    }
}