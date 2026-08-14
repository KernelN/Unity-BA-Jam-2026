/////////////////////////////////////////////////////////////////////////////////
//
//	Grabbing.cs
//
//	Description:	interaction with the outside world.
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace VSController
{
    public class Grabbing : MonoBehaviour
    {
        [Header("Objects & References")]
        [SerializeField] private Transform holdPoint;             // Position where the picked object is held
        [SerializeField] private Transform player;                // Reference to the player transform
        public Camera playerCamera;

        [Space(5)]
        [SerializeField] private AudioSource audioSource;         // AudioSource for playback
        [SerializeField] private AudioClip errorTeleport;         // if spawning is not possible

        [Header("Pickup & Throw Settings")]
        [SerializeField] private float throwForce = 10f;          // Force applied when throwing an object
        [SerializeField] private float pickUpSpeed = 10f;         // Speed of moving picked object to hold point

        private GameObject pickedObject;                          // Currently held object
        private Rigidbody pickedRigidbody;                        // Rigidbody of the held object
        private bool isHoldingObject = false;                     // Whether player is holding an object
        private bool inHandsNow = false;                          // Is the object located near the HoldPoint
        private bool longDistance = false;                        // When taken, the object is long than nearDist inside MovePickedObjectToHoldPoint()
        private bool heldButtonMobile;                            // Is UI button on the mobile control holds on
        private float placeHoldTime;                              // How long is button held down on mobile controls mode
        private float timeToPickUpTotal = 0f;                     // Time it will take to pick up obj
        private float timeToPickUp = 0f;                          // Current time that will be take to pick up obj
        private float initialPickDistance;                        // Distance from player to object pick up
        private int rotationVariant = 0;                          // Rotate variation when object spawn
        private ObjectType lastHighlightedObject = null;          // Last highlighted object type 
        private ObjectType currentObjectType;                     // Current object type being interacted with
        private PlacementPreview placementPreview;                 
        private FPSController playerController;

        private Vector3 pickUpStartPosition;                      // Player position during start pick up
        private Vector3? lastCapsuleBottom;                       // Debug object designation 
        private Vector3? lastCapsuleTop;                          // Same
        private Color lastGizmoColor = Color.green;               // Debug color

        [Header("UI/Input")]
        [SerializeField] private FPSInput InputData;
        [SerializeField] private UIManager UIManager;
        private bool isMobileControl;                             // Actual status from FPSController.cs

        [Header("Object Settings")]
        public List<ObjectType> objectTypes = new List<ObjectType>();

        [System.Serializable]
        public class ObjectType
        {
            public string tag;

            [Header("Interaction Settings")]
            public float activateRange = 5f;
            public float deactivateRange = 10f;

            [Header("Audio Clips")]
            public AudioClip activateSound;
            public AudioClip deactivateSound;

            [Header("Special Flags")]
            public bool canPickUp = true;
            public bool isCollectable = false;
            public bool teleportToThrownObject;

            [Header("Collectable Settings")]
            public List<string> allowedPlacementTags = new List<string>();
            public bool useFaceGrid = false;
            public bool enablePlacementPreview = true;
            public bool spawnBottomToObject = true;

            public List<HoverInteraction> hoverInteractions;

            [System.Serializable]
            public class HoverInteraction
            {
                public UnityEvent onHover; 
                public UnityEvent onExit;  
                public bool isMobile;     
            }
        }

        [System.Serializable]
        public class CollectedStack
        {
            public ObjectType objectType;
            public GameObject prefab;
            public string originalTag;
            public int count;
        }

        public List<CollectedStack> collectedStacks = new();
        public Dictionary<(Collider surface, Vector3Int faceId), HashSet<Vector2Int>> occupiedFaceSlots = new();
        public Dictionary<GameObject, (Collider surface, Vector3Int faceId, Vector2Int cell)> gridObjects = new(); 
        public System.Func<GameObject, Vector3> GetColliderSizeFunc;
        public int RotationVariant => rotationVariant;

        private void Start()
        {
            playerController = GetComponent<FPSController>();
            placementPreview = GetComponent<PlacementPreview>();
            GetColliderSizeFunc = GetColliderSizeLocal;

            lastHighlightedObject = null;

            ResetAllHighlights();

            // Assign buttons
            UIManager.GetPointer().enabled = false;
            UIManager.GetInteractButton().onClick.AddListener(OnInteractButtonPressed);
            UIManager.GetThrowButton().onClick.AddListener(ThrowObject);
            UIManager.GetReleaseButton().onClick.AddListener(DropObject);
            UIManager.GetPlaceButton().onClick.AddListener(TryPlaceCollectedObject);
            UIManager.GetTakeButton().onClick.AddListener(OnInteractButtonPressed);

            // Hide buttons
            UIManager.GetInteractButton().gameObject.SetActive(false);
            UIManager.GetThrowButton().gameObject.SetActive(false);
            UIManager.GetReleaseButton().gameObject.SetActive(false);
            UIManager.GetPlaceButton().gameObject.SetActive(false);
            UIManager.GetTakeButton().gameObject.SetActive(false);

            // Add event trigger
            var placeBtn = UIManager.GetPlaceButton();

            var trig = placeBtn.gameObject.GetComponent<EventTrigger>() ?? placeBtn.gameObject.AddComponent<EventTrigger>();
            trig.triggers ??= new List<EventTrigger.Entry>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => heldButtonMobile = true);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => heldButtonMobile = false);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => heldButtonMobile = false);

            trig.triggers.Add(down);
            trig.triggers.Add(up);
            trig.triggers.Add(exit);

            placeBtn.onClick.AddListener(() =>
            {
                TryPlaceCollectedObject();
            });
        }

        private void Update()
        {
            // Assign the control mode with base FPSController.cs
            isMobileControl = playerController.useMobileControls;

            // Toggle between PC and mobile control (default key "O")
            if (Input.GetKeyDown(InputData.controlsToggle))
            {
                isMobileControl = !isMobileControl;

                // Kill preview object
                if (placementPreview != null)
                {
                    placementPreview.Kill();
                    Destroy(placementPreview);
                    placementPreview = null;
                }

                // Reset UI
                heldButtonMobile = false;
                placeHoldTime = 0f;

                UIManager.UpdateInteractionButtons(isMobileControl, isHoldingObject);
            }

            // Auto drop check
            if (isHoldingObject && pickedObject != null)
            {
                CheckDistanceToDropObject();
            }

            CheckForObjectUnderPointer();

            // Set up PC controls
            bool held = (!isMobileControl && Input.GetKey(InputData.placementKey)) || (isMobileControl && heldButtonMobile);

            if (Input.GetKeyDown(InputData.interactionKey))
            {
                if (held)
                {
                    // When preview 
                    rotationVariant = (rotationVariant + 1) % 4;
                    return; 
                }
                else
                {
                    if (!isMobileControl)
                    {
                        if (!isHoldingObject)
                            TryInteractWithObject();
                        else
                            DropObject();
                    }
                }
            }

            if (!held && !isMobileControl)
            {
                if (Input.GetKeyDown(InputData.throwKey) && isHoldingObject)
                {
                    ThrowObject();
                }
            }

            InteractionInput(held);
        }

        // Taking object 
        private void FixedUpdate()
        {
            if (isHoldingObject && pickedObject != null)
            {
                MovePickedObjectToHoldPoint();
            }
        }

        // Turning off the script, reset UI
        private void OnDisable()
        {
            DisableInteractionsButtons();
            ResetPlacement();
        }

        // Setting up the interaction controls 
        private void InteractionInput(bool held)
        {
            bool canPlace = false;
            bool canCollect = false;
            bool canPickup = false;

            // Disable some ui buttons if we have object on "hands"
            if (isHoldingObject)
            {
                if (isMobileControl)
                {
                    UIManager.GetTakeButton().gameObject.SetActive(false);
                    UIManager.GetInteractButton().gameObject.SetActive(false);
                    UIManager.GetPlaceButton().gameObject.SetActive(false);
                }
                return;
            }

            // Reset all if hit empty tag
            if (!TryGetInteractionHit(out RaycastHit hit))
            {
                DisableInteractionsButtons();
                ResetPlacement();
                return;
            }

            var obj = hit.collider?.gameObject;

            // Check distance and interactions which we can use 
            if (obj != null)
            {
                var type = objectTypes.Find(t => obj.CompareTag(t.tag));

                if (type != null)
                {
                    float dist = Vector3.Distance(playerCamera.transform.position, hit.point);

                    if (dist <= type.activateRange)
                    {
                        // Define type of interaction
                        canCollect = type.isCollectable;
                        canPickup = type.canPickUp || obj.GetComponent<MovableObject>() != null || obj.GetComponent<ManualButton>() != null;
                    }
                }
            }

            // Check objects count
            CollectedStack sel = null;

            if (collectedStacks != null && collectedStacks.Count > 0)
                canPlace = PlacementStack(hit, out sel);

            // Activation and deactivation ui buttons
            if (isMobileControl)
            {
                var take = UIManager.GetTakeButton();
                var interact = UIManager.GetInteractButton();
                var place = UIManager.GetPlaceButton();

                take.gameObject.SetActive(canCollect);
                interact.gameObject.SetActive(!canCollect && canPickup);
                place.gameObject.SetActive(canPlace);
            }

            if (!canPlace)
            {
                ResetPlacement();
                return;
            }

            if (!canPlace && canPickup && held)
                return;

            // if hold place button
            if (held)
            {
                placeHoldTime += Time.deltaTime;
                if (placeHoldTime < 0.15f) return;

                if (sel == null)
                {
                    ResetPlacement();
                    return;
                }

                if (sel.objectType == null || !sel.objectType.enablePlacementPreview)
                {
                    ResetPlacement();
                    return;
                }

                // Add PlacementPreview.cs to obj
                if (placementPreview == null)
                    placementPreview = gameObject.AddComponent<PlacementPreview>();

                placementPreview.Tick(sel, hit);

                // Change position object automatically on mobile control during preview
                if (isMobileControl && placeHoldTime >= 1f)
                {
                    rotationVariant = (rotationVariant + 1) % 4;
                    placeHoldTime -= 1f; 
                }

                return;
            }

            // Release check
            bool released = (!isMobileControl && Input.GetKeyUp(InputData.placementKey)) || (isMobileControl && !heldButtonMobile && placeHoldTime > 0f);

            if (!released)
                return;

            // If release place button
            if (placementPreview != null)
            {
                placementPreview.Kill();
                Destroy(placementPreview);
                placementPreview = null;
            }

            TryPlaceCollectedObject();
            placeHoldTime = 0f;
        }

        private void ResetPlacement()
        {
            placeHoldTime = 0f;

            if (placementPreview != null)
            {
                placementPreview.Kill();
                Destroy(placementPreview);
                placementPreview = null;
            }
        }

        private void DisableInteractionsButtons()
        {
            var take = UIManager.GetTakeButton();
            var interact = UIManager.GetInteractButton();
            var place = UIManager.GetPlaceButton();

            if (take) take.gameObject.SetActive(false);
            if (interact) interact.gameObject.SetActive(false);
            if (place) place.gameObject.SetActive(false);
        }

        // Interactions depending on the object's status
        private void OnInteractButtonPressed()
        {
            // Switching buttons depending on whether you have object (in your "hands" or no)
            if (isHoldingObject)
                ThrowObject();
            else
                TryInteractWithObject();
        }

        // Finds and determines the type of interaction with object
        private void TryInteractWithObject()
        {
            // Launch a raycast to search for an object
            if (!TryGetInteractionHit(out RaycastHit hit))
                return;

            GameObject targetObject = hit.collider.gameObject;

            // Checking the object type by tag
            ObjectType objectType = objectTypes.Find(c => c.tag == targetObject.tag);
            if (objectType == null)
                return;

            // Cheack pickupRange after hit
            float dist = Vector3.Distance(playerCamera.transform.position, hit.point);
            if (dist > objectType.activateRange)
                return;

            // if the object has a MovanleObject.cs script, we switch its state by Toggle()
            MovableObject movable = targetObject.GetComponent<MovableObject>();
            if (movable != null)
            {
                movable.Toggle();

                if (audioSource != null)
                {
                    AudioClip clip = movable.IsOpen()
                        ? objectType.activateSound
                        : objectType.deactivateSound;

                    if (clip != null)
                        audioSource.PlayOneShot(clip);
                }

                return;
            }

            // Same us MovableObject
            ManualButton manualButton = targetObject.GetComponent<ManualButton>();
            if (manualButton != null)
            {
                manualButton.Toggle();

                if (audioSource != null)
                {
                    AudioClip clip = manualButton.IsActive
                        ? objectType.activateSound
                        : objectType.deactivateSound;

                    if (clip != null)
                        audioSource.PlayOneShot(clip);
                }

                return;
            }

            // If object is collectable we collect it instantly, not hold in hand
            CollectableItem pickup = targetObject.GetComponent<CollectableItem>();

            if (objectType.isCollectable)
            {
                CollectableItem.Run(pickup.PickUpActions, targetObject, targetObject, () =>
                    {
                        CollectItem(objectType, targetObject, pickup.prefabToSpawn);
                    }
                );

                return;
            }
            // If a player is standing on an object, it cannot be picked up.
            if (objectType.canPickUp)
            {
                if (IsPlayerStandingOnObject(targetObject))
                {
                    return;
                }

                pickedObject = targetObject;
                pickedRigidbody = pickedObject.GetComponent<Rigidbody>();
                currentObjectType = objectType;
                pickUpStartPosition = pickedObject.transform.position;
                initialPickDistance = Vector3.Distance(holdPoint.position, pickedObject.transform.position);

                // Disable physics on the picked object
                if (pickedRigidbody != null)
                {
                    pickedRigidbody.useGravity = false;
                    pickedRigidbody.freezeRotation = true;
                    isHoldingObject = true;

                    // Switch interface buttons
                    UIManager.UpdateInteractionButtons(isMobileControl, isHoldingObject);

                    if (audioSource != null && objectType.activateSound != null)
                    {
                        audioSource.PlayOneShot(objectType.activateSound);
                    }
                }
            }
        }

        // Raycasts to check the object under the player
        private bool IsPlayerStandingOnObject(GameObject targetObject)
        {
            Vector3 rayStart = player.transform.position + Vector3.up * 0.2f;
            Vector3[] rayDirections =
            {
               Vector3.down,
               (Vector3.down + Vector3.forward * 0.3f).normalized, (Vector3.down + Vector3.back * 0.3f).normalized,
               (Vector3.down + Vector3.left * 0.3f).normalized, (Vector3.down + Vector3.right * 0.3f).normalized
            };
            foreach (Vector3 dir in rayDirections)
            {
                RaycastHit hit;
                if (Physics.Raycast(rayStart, dir, out hit, 2f))
                {
                    if (hit.collider.gameObject == targetObject)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Collecting object to "inventory"
        private void CollectItem(ObjectType objectType, GameObject collectedObject, GameObject prefab)
        {
            if (objectType == null || !objectType.isCollectable || prefab == null)
                return;

            if (collectedObject == null)
                return;

            string collectedName = collectedObject.name;

            // Cleaning the grid slot
            if (gridObjects.TryGetValue(collectedObject, out var data))
            {
                // Surface its object and faceId his sides
                var key = (data.surface, data.faceId);

                if (occupiedFaceSlots.TryGetValue(key, out var set))
                {
                    set.Remove(data.cell);  

                    if (set.Count == 0)
                        occupiedFaceSlots.Remove(key);
                }

                gridObjects.Remove(collectedObject);
            }

            // It handle to add item to an existing stack or create a new stack (in virtual "inventory")
            CollectedStack stack = ResolveStack(objectType, prefab);

            if (stack != null)
                stack.count++;
            else
            {
                // Data that it saves from object
                collectedStacks.Add(new CollectedStack
                {
                    objectType = objectType,             
                    prefab = prefab,
                    count = 1
                });
            }

            audioSource.PlayOneShot(objectType.activateSound);

            Debug.Log($"Collected {collectedName}. Total stacks: {collectedStacks.Count}");
            Destroy(collectedObject);
        }

        // Find in "inventory" needed stack and merge same 
        private CollectedStack ResolveStack(ObjectType objectType, GameObject prefab)
        {
            if (collectedStacks == null)
                return null;

            // Assign what we found
            CollectedStack found = null;

            // Find stacks (from end)
            for (int i = collectedStacks.Count - 1; i >= 0; i--)
            {
                var stack = collectedStacks[i];
                if (stack == null) continue;

                // Reset if no object on stack
                if (stack.count <= 0)
                {
                    collectedStacks.RemoveAt(i);
                    continue;
                }

                // Matching conditions: sameType(tag, object type), SamePrefab
                bool sameType = stack.objectType == objectType || (stack.objectType != null && objectType != null && stack.objectType.tag == objectType.tag);
                bool samePrefab = stack.prefab == prefab || (stack.prefab != null && prefab != null && stack.prefab.name == prefab.name);

                // Drop if something doesnt match
                if (!sameType || !samePrefab)
                    continue;

                // If we found - assign (or merge)
                if (found == null)
                {
                    found = stack;
                }
                else
                {
                    found.count += stack.count;
                    collectedStacks.RemoveAt(i);
                }
            }

            return found;
        }

        // Stack to ObjectType conversion (by priority): object type, tag on object, prefab
        private ObjectType ResolveStackObjectType(CollectedStack stack)
        {
            if (stack == null) return null;

            string tag = stack.objectType?.tag ?? stack.prefab?.tag;
            return string.IsNullOrEmpty(tag) ? null : objectTypes.Find(t => t != null && t.tag == tag);
        }

        // Trying to place the object with user settings and selected types
        private void TryPlaceCollectedObject()
        {
            // Checks for spawn approval
            if (collectedStacks.Count == 0)
                return;

            if (!TryGetInteractionHit(out RaycastHit hitProbe))
                return;

            if (!PlacementStack(hitProbe, out CollectedStack selected))
                return;

            ObjectType type = ResolveStackObjectType(selected);
            if (type == null)
                return;

            selected.objectType = type;

            // Take permitted distance from object type
            float dist = Vector3.Distance(playerCamera.transform.position, hitProbe.point);
            if (dist > type.deactivateRange)
                return;

            float placeRange = type.deactivateRange;

            if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, placeRange))
                return;

            if (hit.collider != hitProbe.collider)
                return;

            Vector3 spawnPos = hit.point;
            Quaternion spawnRot;

            Collider surfaceCol = hit.collider;

            Vector2Int usedCell = Vector2Int.zero; Vector3Int usedFaceId = Vector3Int.zero;  
            bool usedGrid = false;

            // Check obj size
            Vector3 objSize = GetColliderSizeLocal(selected.prefab);
            if (objSize == Vector3.zero)
            {
                return;
            }

            // Surface which we looking 
            Vector3 normal = hit.normal.normalized;

            if (type.spawnBottomToObject)
            {
                // Attach to surface
                spawnRot = Quaternion.FromToRotation(Vector3.up, normal);
                Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;

                // If bad forward
                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.ProjectOnPlane(Vector3.right, normal).normalized;

                // Rotation of different sides
                Quaternion spin = Quaternion.AngleAxis(rotationVariant * 90f, normal);
                spawnRot = Quaternion.LookRotation(spin * forward, normal);
            }
            else
            {
                spawnRot = Quaternion.AngleAxis(rotationVariant * 90f, Vector3.up);
            }

            // FACE GRID MODE
            if (type.useFaceGrid)
            {
                // Define face by face id system)))
                Vector3 absN = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                Vector3Int faceId; 

                if (absN.x >= absN.y && absN.x >= absN.z)
                    faceId = new Vector3Int(normal.x >= 0f ? 1 : -1, 0, 0);
                else if (absN.y >= absN.x && absN.y >= absN.z)
                    faceId = new Vector3Int(0, normal.y >= 0f ? 1 : -1, 0);
                else
                    faceId = new Vector3Int(0, 0, normal.z >= 0f ? 1 : -1);

                usedFaceId = faceId; // Save face

                // Any face has its own grid
                var key = (surface: surfaceCol, faceId: faceId);

                // Is cell occupied?
                if (!occupiedFaceSlots.ContainsKey(key))
                    occupiedFaceSlots[key] = new HashSet<Vector2Int>();

                Bounds sb = surfaceCol.bounds;

                // Grid axes on this face
                Vector3 axisA, axisB;

                if (faceId.x != 0)          
                {
                    axisA = Vector3.forward; // Z
                    axisB = Vector3.up;      // Y
                }
                else if (faceId.y != 0)     
                {
                    axisA = Vector3.right;   // X
                    axisB = Vector3.forward; // Z
                }
                else                       
                {
                    axisA = Vector3.right;   // X
                    axisB = Vector3.up;      // Y
                }

                // Size of face and object along the A/B axes
                float faceA = Vector3.Dot(sb.size, axisA);
                float faceB = Vector3.Dot(sb.size, axisB);

                float objA = Mathf.Abs(Vector3.Dot(objSize, axisA));
                float objB = Mathf.Abs(Vector3.Dot(objSize, axisB));

                int cellsA = Mathf.FloorToInt(faceA / objA);
                int cellsB = Mathf.FloorToInt(faceB / objB);

                if (cellsA <= 0 || cellsB <= 0)
                    return;

                // Find center of face
                Vector3 faceCenter = sb.center + new Vector3(faceId.x * sb.extents.x, faceId.y * sb.extents.y, faceId.z * sb.extents.z);
                Vector3 faceOrigin = faceCenter - axisA * (faceA * 0.5f) - axisB * (faceB * 0.5f);

                // Find out local coordinates
                Vector3 hitLocal = (hit.point - faceOrigin) + normal * 0.001f;

                float aCoord = Vector3.Dot(hitLocal, axisA) / objA;
                float bCoord = Vector3.Dot(hitLocal, axisB) / objB;

                int cellA = Mathf.Clamp(Mathf.RoundToInt(aCoord - 0.5f), 0, cellsA - 1);
                int cellB = Mathf.Clamp(Mathf.RoundToInt(bCoord - 0.5f), 0, cellsB - 1);

                Vector2Int cell = new Vector2Int(cellA, cellB);

                // Checking occupied face on this border
                if (occupiedFaceSlots[key].Contains(cell))
                    return;

                // Find center of cell
                Vector3 planePoint = faceOrigin + axisA * (cellA * objA + objA * 0.5f) + axisB * (cellB * objB + objB * 0.5f);

                // Shift obj outside
                Vector3 half = objSize * 0.5f;

                // Rotate half size to the objects orientation
                Vector3 right = spawnRot * Vector3.right * half.x;
                Vector3 up = spawnRot * Vector3.up * half.y;
                Vector3 fwd = spawnRot * Vector3.forward * half.z;

                float offset =
                    Mathf.Abs(Vector3.Dot(right, normal)) +
                    Mathf.Abs(Vector3.Dot(up, normal)) +
                    Mathf.Abs(Vector3.Dot(fwd, normal));

                spawnPos = planePoint + normal * offset;

                // Adds object to cell
                usedCell = cell;
                usedGrid = true;
            }
            else
            {
                // FACE FREE MODE
                Vector3 facePoint = hit.point;
                Vector3 half = objSize * 0.5f;

                // Pay attention to rotation of object
                Vector3 right = spawnRot * Vector3.right * half.x;
                Vector3 up = spawnRot * Vector3.up * half.y;
                Vector3 fwd = spawnRot * Vector3.forward * half.z;

                // How move out object from the surface
                float offset =
                    Mathf.Abs(Vector3.Dot(right, normal)) +
                    Mathf.Abs(Vector3.Dot(up, normal)) +
                    Mathf.Abs(Vector3.Dot(fwd, normal));

                // Shift obj outside
                spawnPos = facePoint + normal * offset;

                Vector3 halfExtents = objSize * 0.5f;

                // Create invisible box to check can spawn object in this place
                Collider[] overlaps = Physics.OverlapBox(spawnPos, halfExtents, spawnRot, ~0, QueryTriggerInteraction.Ignore);

                // Checking overlaps 
                foreach (var col in overlaps)
                {
                    if (col != surfaceCol)
                        return;
                }
            }

            // Checking if object passes through the player
            if (Physics.OverlapBox(spawnPos, objSize * 0.5f, spawnRot)
                      .Any(c => c.GetComponent<CharacterController>()))
                return;

            // Spawn starts (find object type and another)
            string resolvedTag = selected.objectType != null && !string.IsNullOrEmpty(selected.objectType.tag)
                    ? selected.objectType.tag
                    : selected.prefab != null && !string.IsNullOrEmpty(selected.prefab.tag)
                        ? selected.prefab.tag
                        : string.Empty;

            // Get from obj type some settings
            ObjectType spawnType = null;

            if (!string.IsNullOrEmpty(resolvedTag)) spawnType = objectTypes.Find(t => t.tag == resolvedTag);
            if (spawnType == null && selected.objectType != null) spawnType = selected.objectType;

            // Play throwSound (spawn) sound 
            if (audioSource != null && spawnType != null && spawnType.deactivateSound != null) audioSource.PlayOneShot(spawnType.deactivateSound);

            // Instantiate obj 
            GameObject spawned = Instantiate(selected.prefab, spawnPos, spawnRot);

            Vector3 originalScale = spawned.transform.localScale;
            spawned.transform.localScale = Vector3.zero;

            // Disable rigbody
            var rb = spawned.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;    
                rb.useGravity = false;    
            }

            // Registrate in grid system (if use grid)
            if (usedGrid)
            {
                // Assing data frim grid
                var key = (surface: surfaceCol, faceId: usedFaceId);

                // Create new key 
                if (!occupiedFaceSlots.ContainsKey(key))
                    occupiedFaceSlots[key] = new HashSet<Vector2Int>();

                // Marking cell as occupied and save
                occupiedFaceSlots[key].Add(usedCell);
                gridObjects[spawned] = (surfaceCol, usedFaceId, usedCell);
            }

            // Assign tag only if valid
            if (!string.IsNullOrEmpty(resolvedTag))
            {
                spawned.tag = resolvedTag;
            }

            // Add CollectableItem.cs to obj to can be take in future
            CollectableItem pickup = spawned.GetComponent<CollectableItem>();

            if (pickup == null)
            {
                pickup = spawned.AddComponent<CollectableItem>();
            }

            // Send the size of object for spawning
            pickup.InitList(originalScale);

            // Save prefab
            pickup.prefabToSpawn = selected.prefab;

            // Activate actions
            if (pickup.PlaceActions != null && pickup.PlaceActions.Count > 0)
            {
                CollectableItem.Run(pickup.PlaceActions, spawned, hit.collider.gameObject);
            }

            // Update "inventory"
            selected.count--;
            if (selected.count <= 0)
                collectedStacks.Remove(selected);
        }

        // Gets coordinates from a prefab for spawning in TryPlaceCollectedObject()
        private Vector3 GetColliderSizeLocal(GameObject prefab)
        {
            Collider col = prefab.GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogError("Prefab has no Collider!");
                return Vector3.zero;
            }

            // Recommended to use!
            if (col is BoxCollider box)
            {
                return Vector3.Scale(box.size, box.transform.lossyScale);
            }

            // Unsupported collider (not recommended for use)
            else if (col is SphereCollider sphere)
            {
                float d = sphere.radius * 2f;
                return new Vector3(d, d, d);
            }

            // Unsupported collider (not recommended for use)
            else if (col is CapsuleCollider capsule)
            {
                float d = capsule.radius * 2f;
                return new Vector3(d, capsule.height, d);
            }

            Debug.LogError("Unsupported collider type");
            return Vector3.zero;
        }

        // Take out object from "inventory"
        private bool PlacementStack(RaycastHit hitProbe, out CollectedStack sel)
        {
            sel = null;

            if (collectedStacks == null || collectedStacks.Count == 0) return false;

            string tag = hitProbe.collider.tag;
            int best = int.MaxValue;

            // Check all items
            for (int i = 0; i < collectedStacks.Count; i++)
            {
                var st = collectedStacks[i];
                var ot = ResolveStackObjectType(st);

                // Pass the object through "filters"
                if (st.count <= 0) continue;
                if (ot == null || st.prefab == null) continue;
                if (st.count >= best) continue;
                if (!ot.allowedPlacementTags.Contains(tag)) continue;

                // Giving object to instance
                best = st.count;
                sel = st;
            }

            return sel != null;
        }

        // Move the object to the HoldPoint coordinates
        private void MovePickedObjectToHoldPoint()
        {
            if (pickedObject == null || pickedRigidbody == null)
                return;

            // Get the distance and hold point position
            float currentDist = Vector3.Distance(holdPoint.position, pickedObject.transform.position);

            // Distance limit from the object 
            float maxDist = initialPickDistance * 1.3f;

            // if the player moves away from the object during picked up - drop the obj
            if (currentDist > maxDist && currentDist > initialPickDistance)
            {
                DropObject();
                return;
            }

            // Distance and vector to pick up
            Vector3 toHold = holdPoint.position - pickedRigidbody.position;
            float dist = toHold.magnitude;

            if (dist < 0.1f)
            {
#if UNITY_6000_0_OR_NEWER
                pickedRigidbody.linearVelocity = Vector3.zero;
#else
                pickedRigidbody.velocity = Vector3.zero;
#endif
                inHandsNow = true;      // If object is already near the player
                longDistance = false;   // If object is long than nearDist 

                timeToPickUpTotal = 0f;
                timeToPickUp = 0f;
                return;
            }

            float baseSpeed = pickUpSpeed;
            float nearDist = 20f; float farDist = 200f;
            float minTime = 1.5f; float maxTime = 4f;

            // If distance is less nearDist
            if (!longDistance && !inHandsNow && dist > nearDist)
            {
                longDistance = true;

                float t = Mathf.InverseLerp(nearDist, farDist, dist);
                timeToPickUpTotal = Mathf.Lerp(minTime, maxTime, t);
                timeToPickUp = Time.time;
            }

            float maxSpeed = baseSpeed;  // We take the speed from pickUpSpeed ​​as the base

            // If distance is more than nearDist
            if (longDistance)
            {
                float elapsed = Time.time - timeToPickUp;
                float remaining = Mathf.Max(0.05f, timeToPickUpTotal - elapsed); 
                maxSpeed = Mathf.Max(baseSpeed, dist / remaining);
            }

            Vector3 desiredVel = toHold.normalized * maxSpeed;

            // Slow down object near a player
            float slowDownDist = 0.6f;
            float k = Mathf.Clamp01(dist / slowDownDist);
            desiredVel *= k;

#if UNITY_6000_0_OR_NEWER
            pickedRigidbody.linearVelocity = desiredVel;
#else
            pickedRigidbody.velocity = desiredVel;
#endif
        }

        private void ThrowObject()
        {
            // Can only be thrown if the object is near the player now
            if (!inHandsNow)
                return;

            // To throw an object you need it to be in hands and have a rigbody
            if (pickedObject == null || pickedRigidbody == null)
                return;

            // Return the parameters back to the object
            float force = currentObjectType != null ? currentObjectType.deactivateRange : throwForce;
            pickedRigidbody.useGravity = true;
            pickedRigidbody.freezeRotation = false;

#if UNITY_6000_0_OR_NEWER
            pickedRigidbody.linearVelocity = playerCamera.transform.forward * force;
#else
            pickedRigidbody.velocity = playerCamera.transform.forward * force;
#endif
            if (audioSource != null && currentObjectType != null && currentObjectType.deactivateSound != null)
            {
                audioSource.PlayOneShot(currentObjectType.deactivateSound);
            }

            // If the current object type should teleport the player to the thrown object,
            // attach a RigidbodyCollisionTracker to continue the teleportation logic.
            if (currentObjectType != null && currentObjectType.teleportToThrownObject)
            {
                var tracker = pickedObject.AddComponent<RigidbodyCollisionTracker>();
                tracker.Init(this);
            }

            ResetPickedObject();
        }

        // This method differs from ThrowObject() in that there is no physical impact on the throw of the object
        private void DropObject()
        {
            ResetPickedObject();
            UIManager.UpdateInteractionButtons(isMobileControl, isHoldingObject);
        }

        // Checks if the held object is too far from the player and automatically drops it
        private void CheckDistanceToDropObject()
        {
            if (pickedObject == null || !isHoldingObject)
                return;

            if (Vector3.Distance(playerCamera.transform.position, pickedObject.transform.position) > 4f && inHandsNow)
            {
                DropObject();
            }
        }

        // Return the parameters of the object and the player to the original ones
        public void ResetPickedObject()
        {
            if (pickedObject != null)
            {
                pickedRigidbody.useGravity = true;
                pickedRigidbody.freezeRotation = false;
            }

            pickedObject = null;     pickedRigidbody = null;
            isHoldingObject = false; inHandsNow = false;

            // Reset all data from MovePickedObjectToHoldPoint()
            inHandsNow = false;      longDistance = false;
            timeToPickUp = 0f;       timeToPickUpTotal = 0f;

            currentObjectType = null;

            UIManager.UpdateInteractionButtons(isMobileControl, isHoldingObject);
        }

        private void CheckForObjectUnderPointer()
        {
            if (isHoldingObject)
            {
                // If the object is picked, disable the button responsible for this
                if (isMobileControl)
                {
                    UIManager.GetInteractButton().gameObject.SetActive(false);
                }

                // Disable hit maker
                if (UIManager.GetPointer().enabled) UIManager.GetPointer().enabled = false;

                // Disable activated objects if there were any
                if (lastHighlightedObject != null)
                {
                    HighlightObjects(false, lastHighlightedObject);
                    lastHighlightedObject = null;
                }
                return;
            }

            RaycastHit hit;
            if (TryGetInteractionHit(out hit))
            {
                ObjectType objectType = objectTypes.Find(c => c.tag == hit.collider.tag);

                bool isValidTarget = false;

                if (objectType != null)
                {
                    float dist = Vector3.Distance(playerCamera.transform.position, hit.point);

                    // It only works within the pickupRange of the selected object 
                    if (dist <= objectType.activateRange)
                        isValidTarget = true;
                }

                UIManager.GetPointer().enabled = isValidTarget;

                // Check available objects to place and disable the button if there null
                bool canPlaceHere = false;
                for (int i = 0; i < collectedStacks.Count; i++)
                {
                    var st = collectedStacks[i];
                    var ot = st?.objectType;

                    if (ot != null &&
                        st.count > 0 &&
                        ot.allowedPlacementTags.Contains(hit.collider.tag) &&
                        Vector3.Distance(playerCamera.transform.position, hit.point) <= ot.deactivateRange)
                    {
                        canPlaceHere = true;
                        break;
                    }
                }

                bool showPointer = isValidTarget || canPlaceHere;
                UIManager.GetPointer().enabled = showPointer;

                if (isMobileControl)
                {
                    UIManager.GetInteractButton().gameObject.SetActive(false);
                }

                // Update highlighted object if the currently detected one is different.
                // Disable highlight on the previous object, then assign the new one.
                if (objectType != lastHighlightedObject)
                {
                    if (lastHighlightedObject != null)
                    {
                        HighlightObjects(false, lastHighlightedObject);
                    }

                    lastHighlightedObject = objectType;
                }

                if (isValidTarget)
                {
                    HighlightObjects(true, objectType);
                }
                else
                {
                    // If you hit object, but it's out of pickupRange, turn off hitmarker
                    if (lastHighlightedObject != null)
                    {
                        HighlightObjects(false, lastHighlightedObject);
                        lastHighlightedObject = null;
                    }
                }
            }

            // Bring it all back
            else
            {
                UIManager.GetPointer().enabled = false;
                if (isMobileControl)
                {
                    UIManager.GetInteractButton().gameObject.SetActive(false);
                }

                if (lastHighlightedObject != null)
                {
                    HighlightObjects(false, lastHighlightedObject);
                    lastHighlightedObject = null;
                }
            }
        }

        // The method is responsible for the activation and operation of objects
        // that are activated when raycasting an object with the required tag
        private void HighlightObjects(bool enable, ObjectType targetObjectType)
        {
            if (targetObjectType == null) return;

            foreach (var evt in targetObjectType.hoverInteractions)
            {
                if (evt.isMobile == isMobileControl)
                {
                    if (enable)
                        evt.onHover?.Invoke();
                    else
                        evt.onExit?.Invoke();
                }
            }
        }

        // Calls onExit on all objects to reset their state on startup
        private void ResetAllHighlights()
        {
            foreach (var obj in objectTypes)
            {
                foreach (var evt in obj.hoverInteractions)
                {
                    evt.onExit?.Invoke();
                }
            }
        }

        // Auto-assigns references to fields
        public void AssignReferences()
        {
            GameObject audioObj = GameObject.Find("Audio Source");
            if (audioObj != null)
            {
                audioSource = audioObj.GetComponent<AudioSource>();
#if UNITY_EDITOR
                AssignEditorSerializedReference("audioSource", audioSource);
#endif
            }

            GameObject takeJumpObject = GameObject.Find("TakeJump");
            if (takeJumpObject != null)
            {
                foreach (var objectType in objectTypes)
                {
                    if (objectType != null && objectType.tag == "JumpCube")
                    {
                        var interaction = new ObjectType.HoverInteraction
                        {
                            isMobile = isMobileControl
                        };

                        interaction.onHover = new UnityEvent();
                        interaction.onHover.AddListener(() =>
                        {
                            if (takeJumpObject != null)
                                takeJumpObject.SetActive(true);
                        });

                        interaction.onExit = new UnityEvent();
                        interaction.onExit.AddListener(() =>
                        {
                            if (takeJumpObject != null)
                                takeJumpObject.SetActive(false);
                        });

                        objectType.hoverInteractions.Add(interaction);
                        takeJumpObject.SetActive(false);
                    }
                }
            }

            GameObject playerObj = GameObject.Find("PlayerController");
            if (playerObj != null)
            {
                player = playerObj.transform;
#if UNITY_EDITOR
                AssignEditorSerializedReference("player", player);
#endif
            }

            Transform hold = playerObj?.transform.Find("HoldPoint");
            if (hold != null)
            {
                holdPoint = hold;
#if UNITY_EDITOR
                AssignEditorSerializedReference("holdPoint", holdPoint);
#endif
            }

            Transform cam = playerObj?.transform.Find("CameraHolder/Camera");
            if (cam != null)
            {
                playerCamera = cam.GetComponent<Camera>();
#if UNITY_EDITOR
                AssignEditorSerializedReference("playerCamera", playerCamera);
#endif
            }

            if (UIManager == null)
            {
#if UNITY_2022_1_OR_NEWER
                UIManager = Object.FindAnyObjectByType<UIManager>();
#else
                UIManager = Object.FindObjectOfType<UIManager>();
#endif

#if UNITY_EDITOR
                AssignEditorSerializedReference("UIManager", UIManager);
#endif
            }

            errorTeleport = Resources.Load<AudioClip>("Sounds/Mechanism/Error");
        }

#if UNITY_EDITOR
        private void AssignEditorSerializedReference(string propertyName, Object reference)
        {
            var so = new SerializedObject(this);
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = reference;
                so.ApplyModifiedProperties();

                Undo.RecordObject(this, $"Assign {propertyName}");
                EditorUtility.SetDirty(this);
            }
        }
#endif

        // The distance at which you can interact with an object
        public bool TryGetInteractionHit(out RaycastHit hit, float maxDistance = 1000f, int layerMask = Physics.DefaultRaycastLayers)
        {
            return Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }

        public void TeleportToThrownObject(Vector3 position)
        {
            float capsuleHeight = 2f;   // The height to which the player will be calculated
            float capsuleRadius = 0.3f; // Player width
            float teleportObjectOffset = 1.5f; // Distance from the object that teleports
            float skin = 0.05f; // Tolerance

            // Calculating the space around the place where the player will teleport (substitute a virtual capsule)
            Vector3 upwardOffset = Vector3.up * (capsuleHeight / 2 + capsuleRadius + skin);
            Vector3 upBottom = position + upwardOffset;
            Vector3 upTop = upBottom + Vector3.up * (capsuleHeight - capsuleRadius * 2);

            // More obstacle check
            bool fitsAbove = !Physics.CheckCapsule(upBottom, upTop, capsuleRadius, ~0, QueryTriggerInteraction.Ignore);
            bool pathClear = !Physics.Raycast(position, Vector3.up, upwardOffset.magnitude, ~0, QueryTriggerInteraction.Ignore);

            // Visually displayed in the editor
            lastCapsuleBottom = upBottom;
            lastCapsuleTop = upTop;
            lastGizmoColor = (fitsAbove && pathClear) ? Color.green : Color.red;

            // If all conditions are satisfied, teleport the player
            if (fitsAbove && pathClear)
            {
                TeleportPlayerTo(upBottom + Vector3.up * capsuleRadius);
                return;
            }

            Vector3 safePosition = position;

            // If object is inside the surface, push it up along normal
            Vector3 downOffset = Vector3.down * (capsuleHeight / 2 + capsuleRadius + skin + teleportObjectOffset);
            Vector3 downBottom = position + downOffset;
            Vector3 downTop = downBottom + Vector3.up * (capsuleHeight - capsuleRadius * 2);

            bool fitsBelow = !Physics.CheckCapsule(downBottom, downTop, capsuleRadius, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;

            Ray ray = new Ray(position, Vector3.down);
            float dist = Mathf.Abs(downOffset.y);

            foreach (var hit in Physics.RaycastAll(ray, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                // Igrnore telepor obj
                if (hit.collider.gameObject == gameObject) 
                    continue;

                blocked = true;
                break;
            }

            fitsBelow &= !blocked;

            lastCapsuleBottom = downBottom;
            lastCapsuleTop = downTop;
            lastGizmoColor = fitsBelow ? Color.green : Color.yellow;

            // Try to teleport again under the object
            if (fitsBelow)
            {
                TeleportPlayerTo(downBottom + Vector3.up * capsuleRadius);
                return;
            }

            // If there is no space, play the teleport error sound
            if (audioSource != null && errorTeleport != null)
                audioSource.PlayOneShot(errorTeleport);
        }

        // Debug teleport display
        private void OnDrawGizmos()
        {
            if (lastCapsuleBottom.HasValue && lastCapsuleTop.HasValue)
            {
                Gizmos.color = lastGizmoColor;
                Gizmos.DrawWireSphere(lastCapsuleBottom.Value, 0.5f);
                Gizmos.DrawWireSphere(lastCapsuleTop.Value, 0.5f);
                Gizmos.DrawLine(lastCapsuleBottom.Value, lastCapsuleTop.Value);
            }
        }

        // Changes the player's position and disables CharacterController to avoid bugs
        private void TeleportPlayerTo(Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.position = position;
                controller.enabled = true;
            }
            else
            {
                player.position = position;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        //
        //	Grabbing.RigidbodyCollisionTracker
        //
        //	Description:	is located at the teleportation object and notifies about
        //	                landing and readiness for teleportation the grabbing
        //					
        /////////////////////////////////////////////////////////////////////////////////

        private class RigidbodyCollisionTracker : MonoBehaviour
        {
            private Grabbing grabbing;
            private bool hasTeleported = false;

            public void Init(Grabbing grabbingScript)
            {
                grabbing = grabbingScript;
            }

            private void OnCollisionEnter(Collision collision)
            {
                if (!hasTeleported && collision.gameObject.tag != "Player")
                {
                    // Completion of teleportation
                    hasTeleported = true;
                    grabbing.TeleportToThrownObject(transform.position);
                }
            }
        }
    }
}

