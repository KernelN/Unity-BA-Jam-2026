using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public class Interactor : MonoBehaviour
    {
        static readonly int InteractParam = Animator.StringToHash("Interact");
        [SerializeField] InteractorSetting startingInteractor;
        InteractorSetting interactorSetting;
        float reach;
        LayerMask rayLayers;
        Interactable lastInteractable;
        
        [Header("Animators")]
        [SerializeField] SaintsField.SaintsDictionary<InteractorSetting, Animator>  animators;
        Animator currentAnimator;
        bool playingInteractionAnim;
        
        [Header("UI")]
        [SerializeField] Image interactorUI;
        [SerializeField] Color interactorUIColor = Color.gray;
        [SerializeField] Color interactorUIActiveColor = Color.white;
       
        void Awake()
        {
            if(interactorSetting == null)
                SetInteractor(startingInteractor);
        }
        void Update()
        {
            if(playingInteractionAnim) return;
            
            bool pressedInput = Input.GetMouseButtonDown(0);
            if (pressedInput)
            {
                playingInteractionAnim = true;
                currentAnimator.SetTrigger(InteractParam);
            }
            
            if(lastInteractable && playingInteractionAnim) return;
            
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
            
            if(interactable.interactions.Any(inter => inter.tag == interactorSetting.tag))
                interactorUI.color = interactorUIActiveColor;
            else 
                interactorUI.color = interactorUIColor;
            
            interactorUI.gameObject.SetActive(true);
            
            lastInteractable = interactable;
        }
        public void SetInteractor(InteractorSetting interactor)
        {
            interactorSetting = interactor;
            reach = interactor.reach;
            rayLayers = interactor.rayLayers;
            interactorUI.sprite = interactor.interactUI;
            currentAnimator = animators[interactorSetting];
        }
        public void Interact()
        {
            lastInteractable?.GetInteracted(new[] { interactorSetting.tag });
            lastInteractable = null;
            playingInteractionAnim = false;
        }
    }
}