/////////////////////////////////////////////////////////////////////////////////
//
//	Joystick.cs
//
//	Description:	controls the mobile joystick in the interface.           
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.EventSystems;

namespace VSController
{
    public enum JoystickType { Fixed, Floating, Dynamic }

    public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;       // Side bow                      
        [SerializeField] private RectTransform handle;           // Central  lever
        [SerializeField] private JoystickType joystickType;      // Changing the joystick interaction type 

        [SerializeField] private bool lockX = false;             // Joystick lock by X
        [SerializeField] private bool lockY = false;             // Joystick lock by Y

        private int activeFinger = -1;                           // Detect pressing
        private bool inputAllowed = false;                       // Does the pressing into the JoystickZone

        private Vector2 input = Vector2.zero;                    // Current offset of the joystick relative to its center
        private Vector2 startPosition;                           // Default location of handle

        public bool IsInputAllowed => inputAllowed;
        public float Horizontal => input.x;
        public float Vertical => input.y;
        public Vector2 Direction => input;
            
        private void Start()
        {
            startPosition = background.anchoredPosition;

            // Depending on the mod, enable or disable the joystick display
            if (joystickType == JoystickType.Fixed)

                background.gameObject.SetActive(true);
            else
                background.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
#if UNITY_2022_1_OR_NEWER
            RectTransform joystickZone = FindAnyObjectByType<UIManager>()?.GetJoystickZone();
#else
            RectTransform joystickZone = FindObjectOfType<UIManager>()?.GetJoystickZone();
#endif
            // if the press is not in the JoystickZone - block use
            if (joystickZone != null && !RectTransformUtility.RectangleContainsScreenPoint(joystickZone, eventData.position, null))
            {
                inputAllowed = false;
                return;
            }

            // Else allow use 
            inputAllowed = true;
            activeFinger = eventData.pointerId;

            // In mode fixed, the background always remains in one place
            if (joystickType != JoystickType.Fixed)
            {
                background.position = eventData.position;
                background.gameObject.SetActive(true);
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!inputAllowed || eventData.pointerId != activeFinger)
                return;

            // Calculate the cursor movement from the joystick center (background)
            Vector2 delta = eventData.position - (Vector2)background.position;

            // Joystick radius (half the background width)
            float radius = background.sizeDelta.x / 2;

            // Convert the offset to a normalized vector [-1, 1] and limit the length to 1
            input = Vector2.ClampMagnitude(delta / radius, 1);

            if (lockX) input.x = 0;
            if (lockY) input.y = 0;

            handle.anchoredPosition = input * radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activeFinger)
            {
                inputAllowed = false;
                activeFinger = -1;

                input = Vector2.zero;
                handle.anchoredPosition = Vector2.zero;

                // In dynamic mode, disable the background after touch
                if (joystickType == JoystickType.Dynamic)
                    background.gameObject.SetActive(false);

                // Floating saves new coordinates each time after touch
                else if (joystickType == JoystickType.Floating)
                    background.anchoredPosition = startPosition;
            }
        }
    }
}

