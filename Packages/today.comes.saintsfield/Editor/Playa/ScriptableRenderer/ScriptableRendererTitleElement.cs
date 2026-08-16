using System;
using System.Collections.Generic;
using System.Reflection;
using SaintsField.Editor.UIToolkitElements;
using SaintsField.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace SaintsField.Editor.Playa.ScriptableRenderer
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class ScriptableRendererTitleElement: VisualElement
    {
#if !UNITY_6000_0_OR_NEWER
        // public new class UxmlTraits : BindableElement.UxmlTraits { }
        public new class UxmlFactory : UxmlFactory<ScriptableRendererTitleElement, UxmlTraits> { }
#endif

        private static VisualTreeAsset _template;
        private static readonly Dictionary<string, bool> CustomViewDataCache = new Dictionary<string, bool>();

        private readonly VisualElement _foldoutIcon;

        public ScriptableRendererTitleElement() : this(null, null)
        {
        }

        public ScriptableRendererTitleElement(SerializedObject rendererFeatureSo, Action onRemove)
        {
            ComponentTitleMockElement title = new ComponentTitleMockElement(rendererFeatureSo?.targetObject);
            hierarchy.Add(title);

            contentContainer = title.contentContainer;

            #region contextMenuButton
            Button contextMenuButton = title.ContextMenuButton;
            contextMenuButton.clicked += () =>
            {
                GenericDropdownMenu genericDropdownMenu = new GenericDropdownMenu();

                genericDropdownMenu.AddItem("Remove", false, onRemove.Invoke);

#if SAINTSFIELD_RENDER_PIPELINE_UNIVERSAL_17_1_0_OR_NEWER
                if(rendererFeatureSo?.targetObject?.GetType() == typeof(FullScreenPassRendererFeature))
                {
                    genericDropdownMenu.AddSeparator("");
                    genericDropdownMenu.AddItem("Show All Advanced Properties", UnityEditor.Rendering.AdvancedProperties.enabled, () => UnityEditor.Rendering.AdvancedProperties.enabled = !UnityEditor.Rendering.AdvancedProperties.enabled);
                }
#endif

                genericDropdownMenu.DropDown(
                    contextMenuButton.worldBound,
                    contextMenuButton,
#if UNITY_6000_3_OR_NEWER
                    DropdownMenuSizeMode.Auto
#else
                    true
#endif
                );
            };
            #endregion

            Label titleLabel = title.TitleLabel;

            if (rendererFeatureSo == null)
            {
                title.value = true;
                titleLabel.text = "Missing";
                return;
            }

            SerializedProperty activeProperty = rendererFeatureSo.FindProperty("m_Active");
            Toggle toggleActive = title.ToggleActive;
            toggleActive.bindingPath = activeProperty.propertyPath;
            toggleActive.TrackPropertyValue(activeProperty, p =>
            {
                bool active = p.boolValue;
                titleLabel.style.color = active ? Color.white : Color.gray;
            });
            titleLabel.style.color = activeProperty.boolValue ? Color.white : Color.gray;

            SerializedProperty nameProperty = rendererFeatureSo.FindProperty("m_Name");
            // Debug.Log(nameProperty.stringValue);
            titleLabel.TrackPropertyValue(nameProperty, p =>
            {
                // string newName = p.stringValue;
                titleLabel.text = GetCustomTitle(rendererFeatureSo.targetObject);
            });
            titleLabel.text = GetCustomTitle(rendererFeatureSo.targetObject);
            titleLabel.style.color = new Color(128/255f, 128/255f, 128/255f);

            this.Bind(rendererFeatureSo);
        }

        // private static string FormatName(string namePropertyStringValue, string typeTitle)
        // {
        //     if (string.IsNullOrWhiteSpace(namePropertyStringValue))
        //     {
        //         return typeTitle;
        //     }
        //
        //     return $"{namePropertyStringValue} ({typeTitle})";
        // }

        private string GetCustomTitle(UnityEngine.Object rendererFeatureObjRef)
        {
            string title = null;
            DisallowMultipleRendererFeature isSingleFeature = rendererFeatureObjRef.GetType().GetCustomAttribute<DisallowMultipleRendererFeature>();
            if (isSingleFeature != null)
            {
                title = isSingleFeature.customTitle;
            }

            if (string.IsNullOrEmpty(title))
            {
                title = ObjectNames.GetInspectorTitle(rendererFeatureObjRef);
            }

            return title;
        }

        public override VisualElement contentContainer { get; }
    }
}
