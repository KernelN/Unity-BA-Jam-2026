/////////////////////////////////////////////////////////////////////////////////
//
//	DoorAutoCollider.cs
//
//	Description:	adds trigger when using the "Door" tag.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

namespace VSController
{
    [InitializeOnLoad]
    public static class DoorAutoCollider
    {
        static DoorAutoCollider()
        {
            ObjectChangeEvents.changesPublished += OnObjectChanged;
        }

        // Call when any change on scene
        private static void OnObjectChanged(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    // Looking for changes in objects 
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties: stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var changeEvent);

                        // Get the Gameobject
                        if (changeEvent.entityId != 0)
                        {
                            var obj = EditorUtility.EntityIdToObject(changeEvent.entityId) as GameObject;

                            // If Gameobject have a "door" tag - adds trigger
                            if (obj != null && obj.CompareTag("Door"))
                            {
                                EnsureTriggerOnce(obj);
                            }
                        }
                        break;
                }
            }
        }

        // Check trigger on obj
        private static void EnsureTriggerOnce(GameObject obj)
        {
            Collider[] colliders = obj.GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                    return;
            }

            BoxCollider triggerCol = obj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;

            Debug.Log($"Trigger added to {obj.name}");
        }
    }
}

