/////////////////////////////////////////////////////////////////////////////////
//
//	FoldoutSectionEditor.cs
//
//	Description:	creates folders for fields scripts in the editor.
//					
/////////////////////////////////////////////////////////////////////////////////

#if !VS_CONTROLLER_EDITORS_DISABLED

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using static FoldoutAttribute;

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class FoldoutSectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
        EditorGUI.EndDisabledGroup();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        bool isInsideFoldout = false;
        bool currentFoldoutExpanded = true;
        string currentFoldoutKey = "";

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script")
                continue;

            var fieldInfo = target.GetType().GetField(prop.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo?.GetCustomAttribute<EndFoldoutAttribute>() != null)
            {
                isInsideFoldout = false;
                EditorGUILayout.PropertyField(prop, true);
                continue;
            }

            var foldoutAttr = fieldInfo?.GetCustomAttribute<FoldoutAttribute>();
            if (foldoutAttr != null)
            {
                string group = foldoutAttr.GroupName;
                currentFoldoutKey = GetPrefsKey(group);

                bool foldoutState = GetFoldoutState(currentFoldoutKey);
                bool newState = EditorGUILayout.Foldout(foldoutState, group, true);
                if (newState != foldoutState)
                    SetFoldoutState(currentFoldoutKey, newState);

                currentFoldoutExpanded = newState;
                isInsideFoldout = true;

                if (currentFoldoutExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(prop, true);
                    EditorGUI.indentLevel--;
                }

                continue;
            }

            if (isInsideFoldout && currentFoldoutExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(prop, true);
                EditorGUI.indentLevel--;
            }
            else if (!isInsideFoldout)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string GetPrefsKey(string group)
    {
        return $"Foldout_{target.GetType().FullName}_{group}";
    }

    private bool GetFoldoutState(string key)
    {
        return EditorPrefs.GetBool(key, false);
    }

    private void SetFoldoutState(string key, bool state)
    {
        EditorPrefs.SetBool(key, state);
    }
}

#endif