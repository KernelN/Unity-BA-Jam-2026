/////////////////////////////////////////////////////////////////////////////////
//
//	MechanismManagerEditor.cs
//
//	Description:	manages the MechanismManager.cs interface.
//					
/////////////////////////////////////////////////////////////////////////////////

#if !VS_CONTROLLER_EDITORS_DISABLED

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static VSController.MechanismManager;

namespace VSController
{
    [CustomEditor(typeof(MechanismManager))]
    public class MechanismManagerEditor : Editor
    {
        private static readonly Dictionary<Mechanism.MechanismColor, Color> colorMap =
            new Dictionary<Mechanism.MechanismColor, Color>()
        {
            { Mechanism.MechanismColor.White, Color.white },
            { Mechanism.MechanismColor.Red, Color.red },
            { Mechanism.MechanismColor.Green, Color.green },
            { Mechanism.MechanismColor.Blue, Color.blue },
            { Mechanism.MechanismColor.Yellow, Color.yellow },
            { Mechanism.MechanismColor.Cyan, Color.cyan },
            { Mechanism.MechanismColor.Magenta, Color.magenta },
            { Mechanism.MechanismColor.Orange, new Color(1f, 0.5f, 0f) }
        };

        private SerializedProperty mechanismsProp;
        private ReorderableList mechanismsList;

        private void OnEnable()
        {
            mechanismsProp = serializedObject.FindProperty("mechanisms");

            mechanismsList = new ReorderableList(
                serializedObject,
                mechanismsProp,
                true,   // draggable
                true,   // header
                true,   // add
                false); // remove 

            mechanismsList.drawHeaderCallback = DrawHeader;
            mechanismsList.drawElementCallback = DrawElement;
            mechanismsList.elementHeightCallback = GetElementHeight;

            mechanismsList.onAddCallback = AddMechanism;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            mechanismsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Mechanisms");
        }

        private float GetElementHeight(int index)
        {
            SerializedProperty mechanism = mechanismsProp.GetArrayElementAtIndex(index);

            float height = 100;

            height += (mechanism.FindPropertyRelative("allowedTags").arraySize + 2) * 22;
            height += (mechanism.FindPropertyRelative("doors").arraySize + 2) * 22;
            height += (mechanism.FindPropertyRelative("floorButtons").arraySize + 2) * 22;
            height += (mechanism.FindPropertyRelative("manualButtons").arraySize + 2) * 22;

            height += 40;

            return height;
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {

            rect.y += 4;
            rect.height -= 8;

            SerializedProperty mechanism = mechanismsProp.GetArrayElementAtIndex(index);
            SerializedProperty nameProp = mechanism.FindPropertyRelative("name");
            SerializedProperty colorProp = mechanism.FindPropertyRelative("mechanismColor");
            SerializedProperty buttonSoundProp = mechanism.FindPropertyRelative("buttonPressSound");
            SerializedProperty doorSoundProp = mechanism.FindPropertyRelative("doorOpenSound");

            rect.y += 4;

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = colorMap[(Mechanism.MechanismColor)colorProp.enumValueIndex];

            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            GUI.backgroundColor = old;

            Rect r = new Rect(rect.x + 8, rect.y + 8,rect.width - 16,EditorGUIUtility.singleLineHeight);
            Rect nameRect = new Rect(r.x, r.y, r.width - 32, 20);
            Rect colorButton = new Rect(r.x + r.width - 24, r.y,24,20);

            // Name
            nameProp.stringValue = EditorGUI.TextField(nameRect, "Name", nameProp.stringValue);

            GUI.backgroundColor = colorMap[(Mechanism.MechanismColor)colorProp.enumValueIndex];

            if (GUI.Button(colorButton, ""))
            {
                PopupWindow.Show(colorButton, new ColorPickerPopup(colorProp));
            }

            GUI.backgroundColor = Color.white;

            r.y += 24;

            // Sounds
            buttonSoundProp.objectReferenceValue = EditorGUI.ObjectField(new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight), "Button Sound",
            buttonSoundProp.objectReferenceValue, typeof(AudioClip),false);

            r.y += 22;

            doorSoundProp.objectReferenceValue = EditorGUI.ObjectField(new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight), "Movable Object Sound",
            doorSoundProp.objectReferenceValue, typeof(AudioClip),false);

            r.y += 24;

            SerializedProperty tagsProp = mechanism.FindPropertyRelative("allowedTags");
            EditorGUI.LabelField( new Rect(r.x, r.y, r.width, 20), "Allowed Tags", EditorStyles.boldLabel);
            r.y += 22;

            // Add tag
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tagProp = tagsProp.GetArrayElementAtIndex(i);

                Rect tagRect = new Rect(r.x, r.y, r.width - 30, 20);
                Rect removeRect = new Rect(r.x + r.width - 25, r.y, 25, 20);

                tagProp.stringValue = EditorGUI.TagField(tagRect, $"Tag {i + 1}", tagProp.stringValue);

                if (GUI.Button(removeRect, EditorGUIUtility.IconContent("TreeEditor.Trash")))
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                r.y += 22;
            }

            if (GUI.Button(new Rect(r.x, r.y, 120, 22), "+ Add Tag"))
            {
                tagsProp.arraySize++;
            }

            r.y += 30;

            SerializedProperty doorsProp = mechanism.FindPropertyRelative("doors");
            EditorGUI.LabelField(new Rect(r.x, r.y, r.width, 20), "Move Objects", EditorStyles.boldLabel);

            r.y += 22;

            // Movable Object
            for (int i = 0; i < doorsProp.arraySize; i++)
            {
                SerializedProperty doorProp = doorsProp.GetArrayElementAtIndex(i);

                Rect objectRect = new Rect(r.x, r.y, r.width - 30, 20);
                Rect removeRect = new Rect(r.x + r.width - 25, r.y, 25, 20);

                doorProp.objectReferenceValue = EditorGUI.ObjectField(objectRect, $"Object #{i + 1}", doorProp.objectReferenceValue, typeof(MovableObject), true);

                if (GUI.Button(removeRect, EditorGUIUtility.IconContent("TreeEditor.Trash")))
                {
                    doorsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                r.y += 22;
            }

            if (GUI.Button(new Rect(r.x, r.y, r.width, 22), "+ Add Movable Object"))
            {
                doorsProp.arraySize++;
            }

            r.y += 25;

            SerializedProperty floorButtonsProp = mechanism.FindPropertyRelative("floorButtons");
            EditorGUI.LabelField(new Rect(r.x, r.y, r.width, 20),"Floor Buttons",EditorStyles.boldLabel);

            r.y += 22;

            // Floor Button
            for (int i = 0; i < floorButtonsProp.arraySize; i++)
            {
                SerializedProperty buttonProp = floorButtonsProp.GetArrayElementAtIndex(i);
                Rect buttonRect = new Rect(r.x, r.y, r.width - 30, 20);
                Rect removeRect = new Rect(r.x + r.width - 25, r.y, 25, 20);

                buttonProp.objectReferenceValue = EditorGUI.ObjectField(buttonRect, $"Floor Button #{i + 1}",buttonProp.objectReferenceValue, typeof(FloorButton), true);

                if (GUI.Button(removeRect,
                    EditorGUIUtility.IconContent("TreeEditor.Trash")))
                {
                    floorButtonsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                r.y += 22;
            }

            if (GUI.Button(new Rect(r.x, r.y, r.width, 22),"+ Add Floor Button"))
            {
                floorButtonsProp.arraySize++;
            }

            r.y += 30;

            SerializedProperty manualButtonsProp = mechanism.FindPropertyRelative("manualButtons");
            EditorGUI.LabelField(new Rect(r.x, r.y, r.width, 22), "Manual Buttons",EditorStyles.boldLabel);

            r.y += 22;

            // Manual Button
            for (int i = 0; i < manualButtonsProp.arraySize; i++)
            {
                SerializedProperty buttonProp = manualButtonsProp.GetArrayElementAtIndex(i);
                Rect buttonRect = new Rect(r.x, r.y, r.width - 30, 20);
                Rect removeRect = new Rect(r.x + r.width - 25, r.y, 25, 20);

                buttonProp.objectReferenceValue = EditorGUI.ObjectField(buttonRect, $"Manual Button #{i + 1}", buttonProp.objectReferenceValue,typeof(ManualButton),true);

                if (GUI.Button(removeRect,
                    EditorGUIUtility.IconContent("TreeEditor.Trash")))
                {
                    manualButtonsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                r.y += 22;
            }

            if (GUI.Button(new Rect(r.x, r.y, r.width, 22),"+ Add Manual Button"))
            {
                manualButtonsProp.arraySize++;
                Undo.RecordObject(target, "+ Add Manual Button");
            }

            r.y += 25;

            // Remove Mechanism
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;

            if (GUI.Button(new Rect(r.x, r.y, r.width, 25),"Remove Mechanism"))
            {
                mechanismsProp.DeleteArrayElementAtIndex(index);
                Undo.RecordObject(target, "Remove Mechanism");
                return;
            }

            GUI.backgroundColor = oldColor;
        }

        private void AddMechanism(ReorderableList list)
        {
            mechanismsProp.arraySize++;

            SerializedProperty newMech = mechanismsProp.GetArrayElementAtIndex(mechanismsProp.arraySize - 1);

            newMech.FindPropertyRelative("name").stringValue = "New Mechanism";
            newMech.FindPropertyRelative("mechanismColor").enumValueIndex = (int)Mechanism.MechanismColor.White;

            newMech.FindPropertyRelative("buttonPressSound").objectReferenceValue = null;
            newMech.FindPropertyRelative("doorOpenSound").objectReferenceValue = null;

            newMech.FindPropertyRelative("doors").arraySize = 0;
            newMech.FindPropertyRelative("floorButtons").arraySize = 0;
            newMech.FindPropertyRelative("manualButtons").arraySize = 0;

            SerializedProperty newTags = newMech.FindPropertyRelative("allowedTags");

            newTags.arraySize = 0;

            // Copy from past mechanism
            if (mechanismsProp.arraySize > 1)
            {
                SerializedProperty prev = mechanismsProp.GetArrayElementAtIndex(mechanismsProp.arraySize - 2);
                SerializedProperty prevTags = prev.FindPropertyRelative("allowedTags");

                for (int i = 0; i < prevTags.arraySize; i++)
                {
                    newTags.InsertArrayElementAtIndex(i);
                    newTags.GetArrayElementAtIndex(i).stringValue = prevTags.GetArrayElementAtIndex(i).stringValue;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private class ColorPickerPopup : PopupWindowContent
        {
            private readonly SerializedProperty colorProp;

            private const float buttonSize = 25f;
            private const int columns = 4;

            public ColorPickerPopup(SerializedProperty colorProp)
            {
                this.colorProp = colorProp;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(
                    columns * (buttonSize + 5),
                    Mathf.CeilToInt(colorMap.Count / (float)columns) * (buttonSize + 5));
            }

            public override void OnGUI(Rect rect)
            {
                int i = 0;

                foreach (var kvp in colorMap)
                {
                    if (i % columns == 0)
                        EditorGUILayout.BeginHorizontal();

                    GUI.backgroundColor = kvp.Value;

                    if (GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    {
                        colorProp.enumValueIndex = (int)kvp.Key;
                        colorProp.serializedObject.ApplyModifiedProperties();
                        editorWindow.Close();
                    }

                    i++;

                    if (i % columns == 0)
                        EditorGUILayout.EndHorizontal();
                }

                GUI.backgroundColor = Color.white;

                if (i % columns != 0)
                    EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif