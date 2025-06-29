/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.GUIAPI.Editor
{
    internal partial class UIBinder : EditorWindow
    {
        public bool IsValid => m_Metadata != null;

        private void OnEnable()
        {
            var configSelector = EditorHelper.QueryAsset<UIConfigSelector>();
            if (configSelector == null)
            {
                Debug.LogError("Create UIConfigSelector first!");
                return;
            }
            m_Config = configSelector.ActiveConfig;
            if (m_Config == null)
            {
                Debug.LogError("No active config!");
                return;
            }

            if (!m_Config.IsValid())
            {
                Debug.LogError("Invalid UIBuilderConfig!");
                return;
            }

            if (configSelector.ActiveMetadataManager == null)
            {
                Debug.LogError("No active metadata manager!");
                return;
            }

            m_MetadataManager = configSelector.ActiveMetadataManager;

            InitEvents(true);
            SetActiveMetadata();
            OnSelectionChanged();
            RemoveInvalidateData();
        }

        private void OnDestroy()
        {
            InitEvents(false);
        }

        public bool IsBound(long localID)
        {
            if (m_Metadata == null)
            {
                return false;
            }

            foreach (var metadata in m_Metadata.GameObjects)
            {
                if (metadata.localID == localID)
                {
                    return true;
                }
            }

            return false;
        }

        public void Bind(long localID, bool bind)
        {
            if (m_Metadata == null)
            {
                return;
            }

            if (bind)
            {
                Debug.Assert(!IsBound(localID));
                var gameObjectMetadata = new UIGameObjectMetadata
                {
                    localID = localID,
                };
                m_Metadata.GameObjects.Add(gameObjectMetadata);
            }
            else
            {
                for (var idx = 0; idx < m_Metadata.GameObjects.Count; ++idx)
                {
                    if (localID == m_Metadata.GameObjects[idx].localID)
                    {
                        m_Metadata.GameObjects.RemoveAt(idx);
                        break;
                    }
                }
            }

            Repaint();

            SaveMetadata();
        }

        private void OnGUI()
        {
            if (!EditorHelper.IsPrefabMode())
            {
                GUILayout.Label("Edit in Prefab Mode!");
                return;
            }

            if (m_Metadata == null)
            {
                if (GUILayout.Button("Create"))
                {
                    CreateUIMetadata();
                }
            }
            else
            {
                DrawMetadataEditor();
            }
        }

        private void SetActiveMetadata()
        {
            m_EditingPrefab = EditorHelper.GetEditingPrefab();
            var guid = EditorHelper.GetObjectGUID(m_EditingPrefab);
            foreach (var metadata in m_MetadataManager.UIMetadatas)
            {
                if (metadata.PrefabGUID == guid)
                {
                    m_Metadata = metadata;
                    break;
                }
            }
        }

        private void CreateUIMetadata()
        {
            var prefab = EditorHelper.GetEditingPrefab();
            m_Metadata = new UIMetadata
            {
                PrefabGUID = EditorHelper.GetObjectGUID(prefab),
                ViewClassName = prefab.name + "View",
                ControllerClassName = prefab.name + "Controller",
                Namespace = m_Config.DefaultNamespace,
                WindowLayer = UIWindowLayer.Layer0,
            };
            Debug.Assert(!string.IsNullOrEmpty(m_Metadata.PrefabGUID), "Prefab guid is null!");
            m_MetadataManager.UIMetadatas.Add(m_Metadata);
            EditorUtility.SetDirty(m_MetadataManager);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void DrawMetadataEditor()
        {
            //RemoveInvalidateData();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate", GUILayout.MaxWidth(100)))
            {
                Generate();
                AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button("Save", GUILayout.MaxWidth(60)))
            {
                AssetDatabase.SaveAssets();
            }

            bool removed = false;
            if (GUILayout.Button("Remove Binding", GUILayout.MaxWidth(120)))
            {
                RemoveBinding();
                removed = true;
            }

            if (GUILayout.Button("Remove Invalidate Metadata"))
            {
                RemoveInvalidateData();
            }

            EditorGUILayout.EndHorizontal();

            if (removed)
            {
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            EditorGUI.BeginChangeCheck();

            m_Metadata.ViewClassName = EditorGUILayout.TextField("View Class Name", m_Metadata.ViewClassName);
            m_Metadata.ControllerClassName = EditorGUILayout.TextField("Controller Class Name", m_Metadata.ControllerClassName);
            m_Metadata.Namespace = EditorGUILayout.TextField("Namespace", m_Metadata.Namespace);
            m_Metadata.WindowLayer = (UIWindowLayer)EditorGUILayout.EnumPopup("Layer", m_Metadata.WindowLayer);

            EditorHelper.HorizontalLine();

            UIGameObjectMetadata metadata = GetGameObjectMetadata(m_SelectedGameObjectLocalID);
            if (metadata != null)
            {
                DrawBindGameObject();

                DrawBindView();

                DrawComponentSelection();

                DrawEventSelection(metadata);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{m_Metadata.ViewClassName}-{m_Metadata.PrefabGUID}");
                EditorGUILayout.EndHorizontal();

                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < m_Metadata.GameObjects.Count; ++i)
                    {
                        if (m_Metadata.GameObjects[i].localID == m_SelectedGameObjectLocalID)
                        {
                            DrawGameObjectMetadata(m_Metadata.GameObjects[i]);
                        }
                    }
                });
            }
            if (EditorGUI.EndChangeCheck())
            {
                SaveMetadata();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGameObjectMetadata(UIGameObjectMetadata metadata)
        {
            EditorHelper.IndentLayout(() =>
            {
                EditorGUILayout.Foldout(true, "Components");
                EditorHelper.IndentLayout(() => {
                    for (var i = 0; i < metadata.Components.Count; ++i)
                    {
                        bool deleted = DrawComponentMetadata(i, metadata.Components[i]);
                        if (deleted)
                        {
                            metadata.Components.RemoveAt(i);
                            break;
                        }
                    }
                });

                EditorGUILayout.Foldout(true, "Events");
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < metadata.Events.Count; ++i)
                    {
                        bool deleted = DrawEventMetadata(i, metadata.Events[i]);
                        if (deleted)
                        {
                            metadata.Events.RemoveAt(i);
                            break;
                        }
                    }
                });
            });
        }

        private bool DrawComponentMetadata(int index, UIComponentMetadata metadata)
        {
            bool deleted = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{index}-{metadata.TypeName}-{metadata.localID}");
            metadata.VariableName = EditorGUILayout.TextField("", metadata.VariableName);
            if (GUILayout.Button("X", GUILayout.MaxWidth(20))) 
            {
                if (EditorUtility.DisplayDialog("Warning", "Continue?", "Yes", "No"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            return deleted;
        }

        private bool DrawEventMetadata(int index, UIEventMetadata metadata)
        {
            bool deleted = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{index}-{metadata.Type}");
            metadata.HandlerName = EditorGUILayout.TextField("", metadata.HandlerName);
            if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
            {
                if (EditorUtility.DisplayDialog("Warning", "Continue?", "Yes", "No"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            return deleted;
        }

        private void DrawBindView()
        {
            var viewClassName = GetViewClassName(Selection.activeGameObject);
            if (!string.IsNullOrEmpty(viewClassName))
            {
                UIGameObjectMetadata metadata = GetGameObjectMetadata(m_SelectedGameObjectLocalID);
                if (metadata != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    metadata.BindView = EditorGUILayout.Toggle("Bind View", metadata.BindView);
                    if (metadata.BindView)
                    {
                        metadata.ViewVariableName = EditorGUILayout.TextField("", metadata.ViewVariableName);
                        metadata.ViewClassName = viewClassName;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawBindGameObject()
        {
            UIGameObjectMetadata metadata = GetGameObjectMetadata(m_SelectedGameObjectLocalID);
            if (metadata != null) 
            {
                EditorGUILayout.BeginHorizontal();
                metadata.BindGameObject = EditorGUILayout.Toggle("Bind Game Object", metadata.BindGameObject);
                if (metadata.BindGameObject)
                {
                    metadata.GameObjectVariableName = EditorGUILayout.TextField("", metadata.GameObjectVariableName);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawComponentSelection()
        {
            UpdateComponentNames();

            EditorGUILayout.BeginHorizontal();
            m_SelectedComponentType = EditorGUILayout.Popup("Component", m_SelectedComponentType, m_ComponentNames);

            UIGameObjectMetadata metadata = GetGameObjectMetadata(m_SelectedGameObjectLocalID);
            if (metadata != null)
            {
                if (GUILayout.Button("Add", GUILayout.MaxWidth(40)))
                {
                    var selectedGameObject = Selection.activeGameObject;
                    var component = selectedGameObject.GetComponent(m_ComponentNames[m_SelectedComponentType]);
                    var componentLocalID = UIEditorHelper.GetPrefabContentsChildComponentLocalID(selectedGameObject, m_SelectedComponentType);
                    AddComponent(metadata, component, componentLocalID);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateComponentNames()
        {
            if (Selection.activeGameObject == null)
            {
                m_ComponentNames = new string[0];
                m_SelectedComponentType = -1;
            }
            else
            {
                var allComponents = Selection.activeGameObject.GetComponents<Component>();
                if (m_ComponentNames == null ||
                    m_ComponentNames.Length != allComponents.Length)
                {
                    m_ComponentNames = new string[allComponents.Length];
                }

                for (var i = 0; i < m_ComponentNames.Length; ++i)
                {
                    m_ComponentNames[i] = allComponents[i].GetType().Name;
                }

                if (m_SelectedComponentType < 0 && m_ComponentNames.Length > 0)
                {
                    m_SelectedComponentType = 0;
                }

                m_SelectedComponentType = Mathf.Min(m_SelectedComponentType, m_ComponentNames.Length - 1);
            }
        }

        private UIGameObjectMetadata GetGameObjectMetadata(long localID)
        {
            foreach (var metadata in m_Metadata.GameObjects)
            {
                if (localID == metadata.localID)
                {
                    return metadata;
                }
            }
            return null;
        }

        private void InitEvents(bool add)
        {
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;

            if (add)
            {
                Selection.selectionChanged = OnSelectionChanged;
                PrefabStage.prefabStageOpened += OnPrefabStageOpened;
                PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            }
            else
            {
                Selection.selectionChanged = null;
            }
        }

        private void OnSelectionChanged()
        {
            m_SelectedGameObjectLocalID = UIEditorHelper.GetPrefabContentsChildLocalID(Selection.activeGameObject);
            Repaint();
        }

        private void OnPrefabStageClosing(PrefabStage stage)
        {
            Clear();
            SetActiveMetadata();
            OnSelectionChanged();
        }

        private void OnPrefabStageOpened(PrefabStage stage)
        {
            Clear();
            SetActiveMetadata();
            OnSelectionChanged();
        }

        private void Clear()
        {
            m_EditingPrefab = null;
            m_Metadata = null;
            m_SelectedGameObjectLocalID = 0;
            m_SelectedComponentType = -1;
        }

        private void AddComponent(UIGameObjectMetadata metadata, Component component, long componentLocalID)
        {
            var componentMetadata = new UIComponentMetadata
            {
                localID = componentLocalID,
                TypeName = component.GetType().Name,
                VariableName = component.GetType().Name,
            };
            Debug.Assert(componentMetadata.localID != 0);
            metadata.Components.Add(componentMetadata);
        }

        private void SaveMetadata()
        {
            EditorUtility.SetDirty(m_MetadataManager);
        }

        private void RemoveInvalidateData()
        {
            if (m_Metadata == null)
            {
                return;
            }
            for (var i = m_Metadata.GameObjects.Count - 1; i >= 0; --i)
            {
                var metadata = m_Metadata.GameObjects[i];
                if (!UIEditorHelper.IsValidLocalID(m_Metadata.PrefabGUID, metadata.localID))
                {
                    m_Metadata.GameObjects.RemoveAt(i);
                }
                else
                {
                    for (var k = metadata.Components.Count - 1; k >= 0; --k)
                    {
                        if (!UIEditorHelper.IsValidLocalID(m_Metadata.PrefabGUID, metadata.Components[k].localID))
                        {
                            metadata.Components.RemoveAt(k);
                        }
                    }
                }
            }
        }

        private string GetViewClassName(GameObject go)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
            var guid = EditorHelper.GetObjectGUID(prefab);
            return GetViewClassName(guid);
        }

        private string GetViewClassName(string guid)
        {
            return m_MetadataManager.GetViewClassName(guid);   
        }

        private void DrawEventSelection(UIGameObjectMetadata gameObjectMetadata)
        {
            EditorGUILayout.BeginHorizontal();
            m_SelectedEventType = (UIEventType)EditorGUILayout.EnumPopup("Event", m_SelectedEventType);

            if (GUILayout.Button("Add", GUILayout.MaxWidth(40)))
            {
                gameObjectMetadata.Events.Add(new UIEventMetadata() { Type = m_SelectedEventType, HandlerName = "" });
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void RemoveBinding()
        {
            m_MetadataManager.UIMetadatas.Remove(m_Metadata);
            m_Metadata = null;
            Clear();
            EditorUtility.SetDirty(m_MetadataManager);
            AssetDatabase.SaveAssets();
        }

        private UIBinderConfig m_Config;
        private UIMetadata m_Metadata;
        private UIMetadataManager m_MetadataManager;
        private long m_SelectedGameObjectLocalID;
        private string[] m_ComponentNames;
        private int m_SelectedComponentType;
        private UIEventType m_SelectedEventType = UIEventType.Click;
        private GameObject m_EditingPrefab;
        private Vector2 m_ScrollPos;
    }
}