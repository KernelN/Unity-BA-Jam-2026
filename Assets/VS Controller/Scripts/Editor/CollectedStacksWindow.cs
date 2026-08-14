/////////////////////////////////////////////////////////////////////////////////
//
//	CollectedStacksWindow.cs
//
//	Description:	in this class we finaly draw the UI window from
//	                GrabbingEditor.cs
//					
/////////////////////////////////////////////////////////////////////////////////

#if !VS_CONTROLLER_EDITORS_DISABLED

using UnityEditor;
using UnityEngine;

namespace VSController
{
    public class CollectedStacksWindow : EditorWindow
    {
        [SerializeField] private EntityId grabbingEntityId;

        private Grabbing grabbing;
        private SerializedObject serializedGrabbing;
        private Vector2 scroll;

        public static void Open(Grabbing target)
        {
            var window = GetWindow<CollectedStacksWindow>("Collected Stacks");

            if (target != null)
            {
                window.grabbing = target;
                window.grabbingEntityId = target.GetEntityId();
                window.serializedGrabbing = new SerializedObject(target);
            }

            window.minSize = new Vector2(420f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            RestoreTarget();
            EditorApplication.update += UpdateWindow;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateWindow;
        }

        private void UpdateWindow()
        {
            if (this == null) return;
            Repaint();
        }

        private void RestoreTarget()
        {
            if (grabbing != null)
                return;

            if (grabbingEntityId == 0)
                return;

            grabbing = EditorUtility.EntityIdToObject(grabbingEntityId) as Grabbing;

            if (grabbing != null)
                serializedGrabbing = new SerializedObject(grabbing);
        }

        private void OnSelectionChange()
        {
            if (!Selection.activeGameObject) return;

            var newGrabbing = Selection.activeGameObject.GetComponent<Grabbing>();
            if (!newGrabbing || newGrabbing == grabbing) return;

            grabbing = newGrabbing;
            grabbingEntityId = grabbing.GetEntityId();
            serializedGrabbing = new SerializedObject(grabbing);

            Repaint();
        }

        private void OnGUI()
        {
            RestoreTarget();

            EditorGUI.BeginChangeCheck();
            var newGrabbing = (Grabbing)EditorGUILayout.ObjectField("Target", grabbing, typeof(Grabbing), true);

            if (EditorGUI.EndChangeCheck())
            {
                grabbing = newGrabbing;

                if (grabbing != null)
                {
                    grabbingEntityId = grabbing.GetEntityId();
                    serializedGrabbing = new SerializedObject(grabbing);
                }
                else
                {
                    grabbingEntityId = 0;
                    serializedGrabbing = null;
                }
            }

            EditorGUILayout.Space(6);

            if (grabbing == null)
            {
                EditorGUILayout.HelpBox("No Grabbing selected", MessageType.Warning);
                return;
            }

            if (serializedGrabbing == null)
                serializedGrabbing = new SerializedObject(grabbing);

            serializedGrabbing.Update();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            var stacksProp = serializedGrabbing.FindProperty("collectedStacks");
            GrabbingEditor.DrawCollectedStacks(grabbing, stacksProp);

            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Layout)
                serializedGrabbing.Update();

            serializedGrabbing.ApplyModifiedProperties();
        }
    }
}
#endif