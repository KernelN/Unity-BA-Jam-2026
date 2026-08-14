/////////////////////////////////////////////////////////////////////////////////
//
//	CollectableItemEditor.cs
//
//	Description:	manages the CollectableItem.cs interface.
//					
/////////////////////////////////////////////////////////////////////////////////

#if !VS_CONTROLLER_EDITORS_DISABLED

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VSController
{
    [CustomPropertyDrawer(typeof(CollectableItem.Action))]
    public class CollectableItemEditor : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return height;

            height += Spacing;

            height += EditorGUIUtility.singleLineHeight + Spacing;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                if (iterator.name == "requiredTag")
                    continue;

                height += EditorGUI.GetPropertyHeight(iterator, true) + Spacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;

            Rect rect = new Rect(position.x, position.y, position.width, line);

            var requiredTag = property.FindPropertyRelative("requiredTag");

            string tagLabel = string.IsNullOrEmpty(requiredTag.stringValue)
                ? "For: All Tags"
                : $"Only: {requiredTag.stringValue}";

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, tagLabel, true);
            rect.y += line + Spacing;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            var unityTagsRaw = UnityEditorInternal.InternalEditorUtility.tags;
            var unityTags = unityTagsRaw.Where(t => t != "Untagged").ToArray();

            string[] tags = new string[unityTags.Length + 1];
            tags[0] = "All Tags";

            for (int i = 0; i < unityTags.Length; i++)
                tags[i + 1] = unityTags[i];

            int index = 0;

            if (!string.IsNullOrEmpty(requiredTag.stringValue))
            {
                int found = System.Array.IndexOf(unityTags, requiredTag.stringValue);
                if (found >= 0)
                    index = found + 1;
            }

            index = EditorGUI.Popup(rect, "Required Tag", index, tags);

            requiredTag.stringValue = index == 0 ? "" : unityTags[index - 1];

            rect.y += line + Spacing;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                if (iterator.name == "requiredTag")
                    continue;

                float h = EditorGUI.GetPropertyHeight(iterator, true);
                rect.height = h;

                EditorGUI.PropertyField(rect, iterator, true);

                rect.y += h + Spacing;
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }
    }
}
#endif