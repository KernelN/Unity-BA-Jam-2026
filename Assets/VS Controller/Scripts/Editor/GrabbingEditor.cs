/////////////////////////////////////////////////////////////////////////////////
//
//	GrabbingEditor.cs
//
//	Description:	manages the GrabbingEditor.cs interface.
//					
/////////////////////////////////////////////////////////////////////////////////

#if !VS_CONTROLLER_EDITORS_DISABLED

using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace VSController
{
    [CustomEditor(typeof(Grabbing))]
    public class GrabbingEditor : Editor
    {
        private static readonly FieldInfo CollectedStacksField =
        typeof(Grabbing).GetField("collectedStacks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Dictionary<string, AnimBool> _collectableFades = new Dictionary<string, AnimBool>();

        private AnimBool GetFade(SerializedProperty element, bool target)
        {
            string key = element.propertyPath;

            if (!_collectableFades.TryGetValue(key, out var ab) || ab == null)
            {
                ab = new AnimBool(target);
                ab.valueChanged.AddListener(Repaint);
                _collectableFades[key] = ab;
            }

            ab.target = target;
            return ab;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "objectTypes", "collectedStacks");

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Collected Stacks");

            if (GUILayout.Button("Open", GUILayout.Width(80)))
            {
                CollectedStacksWindow.Open((Grabbing)target);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            var grabbing = (Grabbing)target;
            var objectTypesProp = serializedObject.FindProperty("objectTypes");

            if (objectTypesProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(objectTypesProp, new GUIContent("Object Types"), false);

                if (Event.current.type == EventType.Layout && !objectTypesProp.isExpanded)
                {
                    objectTypesProp.isExpanded = true;
                }

                if (GUILayout.Button("+", GUILayout.Width(22)))
                {
                    int index = objectTypesProp.arraySize;
                    objectTypesProp.InsertArrayElementAtIndex(index);

                    objectTypesProp.isExpanded = true;

                    var newElement = objectTypesProp.GetArrayElementAtIndex(index);
                    if (newElement != null)
                    {
                        var tagProp = newElement.FindPropertyRelative("tag");
                        var pickupRange = newElement.FindPropertyRelative("pickupRange");
                        var throwForce = newElement.FindPropertyRelative("throwForce");
                        var pickUpSound = newElement.FindPropertyRelative("pickUpSound");
                        var throwSound = newElement.FindPropertyRelative("throwSound");
                        var canPickUp = newElement.FindPropertyRelative("canPickUp");
                        var isCollectable = newElement.FindPropertyRelative("isCollectable");
                        var teleportToThrownObject = newElement.FindPropertyRelative("teleportToThrownObject");
                        var allowedPlacementTags = newElement.FindPropertyRelative("allowedPlacementTags");
                        var useFaceGrid = newElement.FindPropertyRelative("useFaceGrid");
                        var enablePlacementPreview = newElement.FindPropertyRelative("enablePlacementPreview");
                        var spawnBottomToObject = newElement.FindPropertyRelative("spawnBottomToObject");
                        var hoverInteractions = newElement.FindPropertyRelative("hoverInteractions");

                        if (tagProp != null) tagProp.stringValue = "Untagged";
                        if (pickupRange != null) pickupRange.floatValue = 5f;
                        if (throwForce != null) throwForce.floatValue = 10f;
                        if (pickUpSound != null) pickUpSound.objectReferenceValue = null;
                        if (throwSound != null) throwSound.objectReferenceValue = null;
                        if (canPickUp != null) canPickUp.boolValue = true;
                        if (isCollectable != null) isCollectable.boolValue = false;
                        if (teleportToThrownObject != null) teleportToThrownObject.boolValue = false;
                        if (allowedPlacementTags != null) allowedPlacementTags.arraySize = 0;
                        if (useFaceGrid != null) useFaceGrid.boolValue = false;
                        if (enablePlacementPreview != null) enablePlacementPreview.boolValue = true;
                        if (spawnBottomToObject != null) spawnBottomToObject.boolValue = true;
                        if (hoverInteractions != null) hoverInteractions.arraySize = 0;
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (objectTypesProp.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < objectTypesProp.arraySize; i++)
                    {
                        var element = objectTypesProp.GetArrayElementAtIndex(i);
                        if (element == null) continue;

                        var tagProp = element.FindPropertyRelative("tag");
                        var isCollectable = element.FindPropertyRelative("isCollectable");
                        var allowedTags = element.FindPropertyRelative("allowedPlacementTags");
                        var useFaceGrid = element.FindPropertyRelative("useFaceGrid");
                        var enablePlacementPreview = element.FindPropertyRelative("enablePlacementPreview");
                        var spawnBottomToObject = element.FindPropertyRelative("spawnBottomToObject");

                        string realTag = (tagProp != null && !string.IsNullOrWhiteSpace(tagProp.stringValue))
                                ? tagProp.stringValue
                                : "Untagged";

                        bool collectableNow = (isCollectable != null && isCollectable.boolValue);

                        int runtimeCount =
                            Application.isPlaying && collectableNow
                            ? GetRuntimeCountByTag(grabbing, realTag)
                            : 0;

                        EditorGUILayout.BeginVertical(GUI.skin.box);

                        EditorGUILayout.BeginHorizontal();

                        string headerName = (Application.isPlaying && collectableNow)
                            ? $"{realTag}   (x{runtimeCount})"
                            : realTag;

                        element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, headerName, true);

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            objectTypesProp.DeleteArrayElementAtIndex(i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }

                        EditorGUILayout.EndHorizontal();

                        if (element.isExpanded)
                        {
                            EditorGUI.indentLevel++;

                            if (tagProp != null)
                            {
                                if (string.IsNullOrEmpty(tagProp.stringValue)) tagProp.stringValue = "Untagged";
                                tagProp.stringValue = EditorGUILayout.TagField("Tag", tagProp.stringValue);
                                realTag = tagProp.stringValue;
                            }

                            if (Application.isPlaying && collectableNow)
                            {
                                int cur = GetRuntimeCountByTag(grabbing, realTag);

                                EditorGUI.BeginChangeCheck();

                                int next =
                                    EditorGUILayout.IntField("Runtime Count", cur);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    SetRuntimeCountForTag(grabbing, i, realTag, next);
                                    Repaint();
                                }
                            }

                            bool disable = isCollectable != null && isCollectable.boolValue;

                            var prop = element.Copy();
                            var end = prop.GetEndProperty();

                            prop.NextVisible(true);

                            while (!SerializedProperty.EqualContents(prop, end))
                            {
                                if (prop.name == "tag" ||
                                    prop.name == "allowedPlacementTags" ||
                                    prop.name == "useFaceGrid" ||
                                    prop.name == "enablePlacementPreview" ||
                                    prop.name == "spawnBottomToObject")
                                {
                                    if (!prop.NextVisible(false)) break;
                                    continue;
                                }

                                if (prop.name == "canPickUp" || prop.name == "teleportToThrownObject")
                                {
                                    EditorGUI.BeginDisabledGroup(disable);
                                    EditorGUILayout.PropertyField(prop, true);
                                    EditorGUI.EndDisabledGroup();
                                }
                                else
                                {
                                    EditorGUILayout.PropertyField(prop, true);
                                }

                                if (!prop.NextVisible(false)) break;
                            }

                            if (isCollectable != null)
                            {
                                var fade = GetFade(element, isCollectable.boolValue);

                                if (EditorGUILayout.BeginFadeGroup(fade.faded))
                                {
                                    EditorGUILayout.Space(4);

                                    EditorGUILayout.BeginVertical("box");

                                    if (allowedTags != null)
                                        DrawTagStringList(allowedTags, "Allowed Placement Tags");

                                    if (useFaceGrid != null)
                                        EditorGUILayout.PropertyField(useFaceGrid);

                                    if (enablePlacementPreview != null)
                                        EditorGUILayout.PropertyField(enablePlacementPreview);

                                    if (spawnBottomToObject != null)
                                        EditorGUILayout.PropertyField(spawnBottomToObject);

                                    EditorGUILayout.EndVertical();
                                }

                                EditorGUILayout.EndFadeGroup();
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ================= Runtime stacks =================
        public static void DrawCollectedStacks(Grabbing grabbing, SerializedProperty stacksProp)
        {
            if (grabbing == null || stacksProp == null)
                return;

            EditorGUILayout.LabelField("Collected Stacks", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            bool isPlaying = Application.isPlaying;
            bool changed = false;
            int removeIndex = -1;

            EditorGUI.indentLevel++;

            for (int i = 0; i < grabbing.collectedStacks.Count; i++)
            {
                var stack = grabbing.collectedStacks[i];
                if (stack == null) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();

                string key = $"CollectedStack_{(isPlaying ? "Play" : "Edit")}_{grabbing.GetEntityId()}_{i}";
                bool expanded = SessionState.GetBool(key, false);
                expanded = EditorGUILayout.Foldout(expanded, $"Item {i + 1}", true);
                SessionState.SetBool(key, expanded);

                if (GUILayout.Button("-", GUILayout.Width(22)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUI.indentLevel++;

                    var collectables = GetCollectableObjectTypes(grabbing);
                    var names = GetCollectableObjectTypeNames(collectables);

                    int currentIndex = 0;

                    if (collectables != null && collectables.Count > 0 && stack.objectType != null)
                    {
                        int index = collectables.IndexOf(stack.objectType);

                        if (index >= 0)
                        {
                            currentIndex = index + 1;
                        }
                        else
                        {
                            for (int j = 0; j < collectables.Count; j++)
                            {
                                var ot = collectables[j];

                                if (ot != null && !string.IsNullOrEmpty(ot.tag) && stack.objectType.tag == ot.tag)
                                {
                                    currentIndex = j + 1;
                                    break;
                                }
                            }
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    int nextIndex = EditorGUILayout.Popup("Object Type", Mathf.Max(0, currentIndex), names);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(grabbing, "Change Collected Stack Object Type");
                        stack.objectType = nextIndex == 0 ? null : collectables[nextIndex - 1];
                        changed = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    var newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", stack.prefab, typeof(GameObject), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(grabbing, "Change Collected Stack Prefab");
                        stack.prefab = newPrefab;
                        changed = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    int newCount = EditorGUILayout.IntField("Count", stack.count);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(grabbing, "Change Collected Stack Count");
                        stack.count = Mathf.Max(0, newCount);
                        changed = true;
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0 && removeIndex < grabbing.collectedStacks.Count)
            {
                Undo.RecordObject(grabbing, "Remove Collected Stack");
                grabbing.collectedStacks.RemoveAt(removeIndex);
                changed = true;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                Undo.RecordObject(grabbing, "Add Collected Stack");
                grabbing.collectedStacks.Add(new Grabbing.CollectedStack
                {
                    objectType = null,
                    prefab = null,
                    count = 0
                });
                changed = true;
            }

            EditorGUILayout.EndHorizontal();

            if (changed)
            {
                MergeDuplicateStacks(grabbing);
                EditorUtility.SetDirty(grabbing);
            }

            EditorGUI.indentLevel--;
        }

        private static List<Grabbing.ObjectType> GetCollectableObjectTypes(Grabbing grabbing)
        {
            List<Grabbing.ObjectType> list = new();

            if (grabbing == null || grabbing.objectTypes == null)
                return list;

            foreach (var ot in grabbing.objectTypes)
            {
                if (ot != null && ot.isCollectable)
                    list.Add(ot);
            }

            return list;
        }

        private static string[] GetCollectableObjectTypeNames(List<Grabbing.ObjectType> list)
        {
            if (list == null || list.Count == 0)
                return new[] { "None" };

            string[] names = new string[list.Count + 1];
            names[0] = "None";

            for (int i = 0; i < list.Count; i++)
                names[i + 1] = list[i].tag;

            return names;
        }

        private static void MergeDuplicateStacks(Grabbing grabbing)
        {
            if (grabbing == null || grabbing.collectedStacks == null || grabbing.collectedStacks.Count <= 1)
                return;

            for (int i = grabbing.collectedStacks.Count - 1; i >= 0; i--)
            {
                var a = grabbing.collectedStacks[i];
                if (a == null) continue;

                for (int j = i - 1; j >= 0; j--)
                {
                    var b = grabbing.collectedStacks[j];
                    if (b == null) continue;

                    if (a.objectType == b.objectType && a.prefab == b.prefab)
                    {
                        b.count += a.count;
                        grabbing.collectedStacks.RemoveAt(i);
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(grabbing);
        }

        private static IList GetStacksList(Grabbing grabbing)
        {
            if (!Application.isPlaying || grabbing == null || CollectedStacksField == null)
                return null;

            return CollectedStacksField.GetValue(grabbing) as IList;
        }

        private static int GetRuntimeCountByTag(Grabbing grabbing, string tag)
        {
            if (string.IsNullOrEmpty(tag) || grabbing == null || grabbing.collectedStacks == null)
                return 0;

            int total = 0;

            foreach (var cs in grabbing.collectedStacks)
            {
                if (cs == null || cs.count <= 0)
                    continue;

                if (cs.objectType != null && cs.objectType.tag == tag)
                    total += cs.count;
            }

            return total;
        }

        private static void SetRuntimeCountForTag(Grabbing grabbing, int objectTypeIndex, string tag, int newCount)
        {
            if (!Application.isPlaying || grabbing == null || string.IsNullOrEmpty(tag))
                return;

            var listObj = GetStacksList(grabbing);
            if (listObj == null) return;

            newCount = Mathf.Max(0, newCount);

            // Найдём первый стак по тегу
            int firstIndex = -1;
            for (int i = 0; i < listObj.Count; i++)
            {
                if (listObj[i] is Grabbing.CollectedStack cs && cs != null && cs.objectType != null && cs.objectType.tag == tag)
                {
                    firstIndex = i;
                    break;
                }
            }

            Undo.RecordObject(grabbing, "Change runtime stack count");

            if (newCount == 0)
            {
                for (int i = listObj.Count - 1; i >= 0; i--)
                {
                    if (listObj[i] is Grabbing.CollectedStack cs && cs != null && cs.objectType != null && cs.objectType.tag == tag)
                        listObj.RemoveAt(i);
                }

                EditorUtility.SetDirty(grabbing);
                return;
            }

            if (firstIndex == -1)
            {
                Grabbing.ObjectType ot = null;
                if (grabbing.objectTypes != null && objectTypeIndex >= 0 && objectTypeIndex < grabbing.objectTypes.Count)
                    ot = grabbing.objectTypes[objectTypeIndex];

                var prefab = FindPrefabInResourcesByTag(tag);
                if (prefab == null)
                    Debug.LogWarning($"[GrabbingEditor] No prefab with tag '{tag}' found in Resources. Stack will be created, but prefab will be null.");

                var newStack = new Grabbing.CollectedStack
                {
                    objectType = ot,
                    prefab = prefab,
                    count = newCount
                };

                listObj.Add(newStack);

                EditorUtility.SetDirty(grabbing);
                return;
            }
            var keep = listObj[firstIndex] as Grabbing.CollectedStack;
            if (keep != null)
                keep.count = newCount;

            for (int i = listObj.Count - 1; i >= 0; i--)
            {
                if (i == firstIndex) continue;
                if (listObj[i] is Grabbing.CollectedStack cs && cs != null && cs.objectType != null && cs.objectType.tag == tag)
                    listObj.RemoveAt(i);
            }

            EditorUtility.SetDirty(grabbing);
        }

        private static GameObject FindPrefabInResourcesByTag(string tag)
        {
            var all = Resources.LoadAll<GameObject>("");
            for (int i = 0; i < all.Length; i++)
            {
                var go = all[i];
                if (go != null && go.CompareTag(tag))
                    return go;
            }
            return null;
        }

        private static void DrawTagStringList(SerializedProperty listProp, string label)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                int i = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).stringValue = "Untagged";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            int removeIndex = -1;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();

                el.stringValue = EditorGUILayout.TagField(
                    GUIContent.none,
                    string.IsNullOrEmpty(el.stringValue)
                        ? "Untagged"
                        : el.stringValue
                );

                if (GUILayout.Button("-", GUILayout.Width(22)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0)
            {
                listProp.DeleteArrayElementAtIndex(removeIndex);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
