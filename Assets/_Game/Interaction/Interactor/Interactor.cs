using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityBaJam2026.Gameplay.Interaction
{
    public class Interactor : MonoBehaviour
    {
        static readonly int InteractParam = Animator.StringToHash("Interact");
        static readonly int IsInteracting = Animator.StringToHash("IsInteracting");
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
        
        [Header("Pick Up & Carry")]
        [SerializeField] Grabber grabber;
        InteractorSetting grabbedInteractor;
       
        void Awake()
        {
            if(interactorSetting == null)
                SetInteractor(startingInteractor);
        }
        void Update()
        {
            if(!interactorSetting) return;
            
            if(grabbedInteractor || grabber.IsGrabbing)
                CheckGrabbedInteractions();
            else CheckInteractions();
        }
        void CheckInteractions()
        {
            if(playingInteractionAnim) return;
            
            bool pressedInput = Input.GetMouseButtonDown(0);
            if (pressedInput && currentAnimator)
            {
                playingInteractionAnim = true;
                currentAnimator.SetTrigger(InteractParam);
            }
            
            if(lastInteractable && playingInteractionAnim) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach, rayLayers, QueryTriggerInteraction.Ignore))
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
        void CheckGrabbedInteractions()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, reach, rayLayers))
            {
                if (Input.GetMouseButtonDown(0)) Drop();
                interactorUI.gameObject.SetActive(false);
                return;
            }

            //If detected surface, but no interactable, input only drops objects
            if (!hit.transform.TryGetComponent(out Interactable interactable) || !interactable.enabled)
            {
                if (Input.GetMouseButtonDown(0)) Drop();
                interactorUI.gameObject.SetActive(false);
                return;
            }
            
            if(!grabbedInteractor) return;
            
            if(interactable.interactions.Any(inter => inter.tag == grabbedInteractor.tag))
                interactorUI.color = interactorUIActiveColor;
            else 
                interactorUI.color = interactorUIColor;
            
            interactorUI.gameObject.SetActive(true);

            if (Input.GetMouseButtonDown(0))
            {
                lastInteractable = interactable;
                Interact(grabbedInteractor);
                grabber.ClearObject(true);
                grabbedInteractor = null;
                
                currentAnimator.SetBool(IsInteracting, false);
            }
        }
        public void SetInteractor(InteractorSetting interactor)
        {
            interactorSetting = interactor;

            if (!interactor)
            {
                interactorUI.gameObject.SetActive(false);
                return;
            }
            
            reach = interactor.reach;
            rayLayers = interactor.rayLayers;
            interactorUI.sprite = interactor.interactUI;
            currentAnimator = animators[interactorSetting];
            
            if(grabber.IsGrabbing)
                Drop();
        }
        public void Interact()
        {
            if(!interactorSetting) return;
            
            lastInteractable?.GetInteracted(new[] { interactorSetting.tag }, this);
            lastInteractable = null;
            playingInteractionAnim = false;
        }
        public void Interact(InteractorSetting interactor)
        {
            lastInteractable?.GetInteracted(new[] { interactor.tag }, this);
            lastInteractable = null;
            playingInteractionAnim = false;
        }
        public void PickUp(Rigidbody objRigidbody, InteractorSetting interactor = null)
        {
            currentAnimator.SetBool(IsInteracting, true);
            grabber.Grab(objRigidbody);
            grabbedInteractor = interactor;
            lastInteractable = null;
        }
        public void Drop()
        {
            currentAnimator.SetBool(IsInteracting, false);
            lastInteractable = null;
            grabbedInteractor = null;
            grabber.DropObject();
        }
    }
}