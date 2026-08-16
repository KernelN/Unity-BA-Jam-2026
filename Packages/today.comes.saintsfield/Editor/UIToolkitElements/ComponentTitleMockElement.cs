using System;
using SaintsField.Editor.Utils;
using SaintsField.Utils;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace SaintsField.Editor.UIToolkitElements
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class ComponentTitleMockElement: BindableElement, INotifyValueChanged<bool>
    {
#if !UNITY_6000_0_OR_NEWER
        // public new class UxmlTraits : BindableElement.UxmlTraits { }
        public new class UxmlFactory : UxmlFactory<ComponentTitleMockElement, UxmlTraits> { }
#endif

        private static VisualTreeAsset _template;
        public override VisualElement contentContainer { get; }

        private bool _expanded = true;
        private readonly VisualElement _foldoutIcon;

        public readonly Toggle ToggleActive;
        public readonly Image Icon;
        public readonly Label TitleLabel;
        public readonly Button ContextMenuButton;
        public readonly Button PresentButton;
        public readonly Button HelpButton;

        public ComponentTitleMockElement()
        {
            _template ??= Util.LoadResource<VisualTreeAsset>("UIToolkit/ComponentTitleMock/Title.uxml");
            TemplateContainer element = _template.CloneTree();
            hierarchy.Add(element);

            contentContainer = element.Q<VisualElement>(name: "content");

            Button titleButton = element.Q<Button>(name: "titleButton");
            titleButton.clicked += () =>
            {
                _expanded = !_expanded;
                RefreshExpand();
            };

            _foldoutIcon = element.Q<VisualElement>(name: "foldoutIcon");

            ToggleActive = element.Q<Toggle>(name: "toggleActive");
            ToggleActive.RegisterCallback<ChangeEvent<bool>>(evt => evt.StopPropagation());

            Icon = element.Q<Image>(name: "icon");
            TitleLabel = element.Q<Label>(name: "titleLabel");

            ContextMenuButton = element.Q<Button>(name: "contextMenuButton");
            PresentButton = element.Q<Button>(name: "presentButton");
            HelpButton = element.Q<Button>(name: "helpButton");
        }

        public ComponentTitleMockElement(UnityEngine.Object target) : this()
        {
            if (RuntimeUtil.IsNull(target))
            {
                return;
            }

            Texture2D thumb = AssetPreview.GetAssetPreview(target);
            if (thumb == null)
            {
                thumb = AssetPreview.GetMiniThumbnail(target);
            }

            if (thumb == null)
            {
                Icon.style.display = DisplayStyle.None;
            }
            else
            {
                Icon.image = thumb;
            }

            TitleLabel.text = ObjectNames.GetInspectorTitle(target);

            if (Help.HasHelpForObject(target))
            {
                HelpButton.style.display = DisplayStyle.Flex;
                HelpButton.clicked += () => Help.ShowHelpForObject(target);
            }
            else
            {
                HelpButton.style.display = DisplayStyle.None;
            }


            PresetType presetType = new PresetType(target);
            bool showPreset =
                target != null &&
                presetType.IsValid() &&
                (target.hideFlags & HideFlags.DontSaveInEditor) == 0;
            if (showPreset)
            {
                PresentButton.style.display = DisplayStyle.Flex;
                PresentButton.clicked +=
                    () => PresetSelector.ShowSelector(new[] { target }, null, true);
            }
            else
            {
                PresentButton.style.display = DisplayStyle.None;
            }
        }

        private string _viewDataKey;

        // ReSharper disable once InconsistentNaming
        public new string viewDataKey
        {
            get => _viewDataKey;
            set
            {
                _viewDataKey = value;
                // ReSharper disable once InvertIf
                if (!string.IsNullOrEmpty(_viewDataKey))
                {
                    ViewDataKeyFoldoutStatus fold = (ViewDataKeyFoldoutStatus)SessionState.GetInt(_viewDataKey, (int)ViewDataKeyFoldoutStatus.None);
                    switch (fold)
                    {
                        case ViewDataKeyFoldoutStatus.None:
                            return;
                        case ViewDataKeyFoldoutStatus.Fold:
                            this.value = false;
                            return;
                        case ViewDataKeyFoldoutStatus.Expand:
                            this.value = true;
                            return;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(fold), fold, null);
                    }
                }
            }
        }

        private void RefreshExpand()
        {
            contentContainer.style.display = _expanded? DisplayStyle.Flex: DisplayStyle.None;
            _foldoutIcon.style.rotate = _expanded ? new StyleRotate(new Rotate(90)) : new StyleRotate(StyleKeyword.None);
            if (!string.IsNullOrEmpty(_viewDataKey))
            {
                SessionState.SetInt(_viewDataKey, (int)(_expanded? ViewDataKeyFoldoutStatus.Expand: ViewDataKeyFoldoutStatus.Fold));
            }
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            _expanded = newValue;
            RefreshExpand();
        }

        public bool value
        {
            get => _expanded;
            set
            {
                if (_expanded == value)
                {
                    return;
                }

                bool previous = this.value;
                SetValueWithoutNotify(value);

                using ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(previous, value);
                evt.target = this;
                SendEvent(evt);
            }
        }
    }
}
