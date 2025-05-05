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


using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    enum ColumnIndex
    {
        Name,
        ChildCount,
        Description,
    }

    public partial class CodeAssistantTreeViewWindow : EditorWindow
    {
        [MenuItem("XDay/Code Assistant/Open Tree View")]
        static void Open()
        {
            var dlg = GetWindow<CodeAssistantTreeViewWindow>("Code Tree");
            dlg.Show();
        }

        void OnEnable()
        {
            if (mSearchField == null)
            {
                mSearchField = new SearchField();
            }
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Source Code Folder", mData.codeFolderName);
            if (GUILayout.Button("Open", GUILayout.MaxWidth(50)))
            {
                mData.codeFolderName = EditorUtility.OpenFolderPanel("Select code folder", "", "");
            }
            EditorGUILayout.EndHorizontal();
            mData.predefinedSymbols = EditorGUILayout.TextField("Predefined Symbols", mData.predefinedSymbols);
            mData.csProjectName = EditorGUILayout.TextField("Project Name", mData.csProjectName);

            GUI.enabled = mTreeView != null;
            EditorGUILayout.BeginHorizontal();
            Attribute displayMask = (Attribute)EditorGUILayout.EnumFlagsField("Important Class", mData.attributeDisplayMask);
            if (displayMask != mData.attributeDisplayMask)
            {
                SetAttributeDisplayMask(displayMask);
            }
            EditorGUILayout.EndHorizontal();

            string searchText = mTreeView != null ? mTreeView.searchText : "";
            float padding = 5;
            float pos = 0;
            float fieldRatio = 0.5f;
            float searchTypeRatio = 0.2f;
            float searchModeRatio = 0.2f;
            float matchCaseRatio = 1.0f;
            float searchFieldY = 90;
            searchText = mSearchField.OnGUI(new Rect(pos, searchFieldY, position.width * fieldRatio - padding, 20), searchText);
            if (mTreeView != null)
            {
                mTreeView.searchText = searchText;
            }
            pos += fieldRatio;

            var searchType = (SearchType)EditorGUI.EnumFlagsField(new Rect(position.width * pos, searchFieldY, position.width * searchTypeRatio - padding, 20), mData.searchType);
            if (searchType != mData.searchType)
            {
                SetSearchType(searchType);
            }
            pos += searchTypeRatio;
            var searchMode = (SearchMode)EditorGUI.EnumPopup(new Rect(position.width * pos, searchFieldY, position.width * searchModeRatio - padding, 20), mData.searchMode);
            if (searchMode != mData.searchMode)
            {
                SetSearchMode(searchMode);
            }
            pos += searchModeRatio;
            bool searchMatchCase = EditorGUI.ToggleLeft(new Rect(position.width * pos, searchFieldY, position.width * matchCaseRatio - padding, 20), "Case", mData.searchMatchCase);
            if (searchMatchCase != mData.searchMatchCase)
            {
                SetSearchMatchCase(searchMatchCase);
            }
            pos += matchCaseRatio;
            GUI.enabled = true;

            if (GUI.Button(new Rect(0, 110, position.width, 20), "Start"))
            {
                CallServer(mData.serverFilePath, mData.codeFolderName, mData.predefinedSymbols, mData.parseResultSavePath);
                mData.Load();

                mState = new TreeViewState();

                var headerState = CreateMultiColumnHeaderState();
                var multiColumnHeader = new MultiColumnHeader(headerState);
                multiColumnHeader.ResizeToFit();

                mTreeView = new CodeAssistantTreeView(mState, multiColumnHeader);
                mTreeView.Build(mData);

                mSearchField.downOrUpArrowKeyPressed += mTreeView.SetFocusAndEnsureSelectedItem;
            }

            float offset = 130;
            if (mTreeView != null)
            {
                var rect = new Rect(0, offset, position.width, position.height - offset);
                mTreeView.OnGUI(rect);
            }
        }

        void CallServer(string serverFilePath, string codeFolder, string predefinedSymbols, string parseResultSavePath)
        {
            try
            {
                string arguments = $"{codeFolder} {predefinedSymbols}; {parseResultSavePath}";
                var process = System.Diagnostics.Process.Start(serverFilePath, arguments);
                process.WaitForExit();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    contextMenuText = "Name",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 350,
                    minWidth = 100,
                    maxWidth = 500,
                    autoResize = false,
                    allowToggleVisibility = true
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Child Count"),
                    contextMenuText = "Child Count",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 100,
                    minWidth = 100,
                    maxWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = true
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Description"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    maxWidth = 1000,
                    minWidth = 100,
                    autoResize = true
                },
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        void SetAttributeDisplayMask(Attribute displayMask)
        {
            mData.attributeDisplayMask = displayMask;
            mTreeView?.Reload();
        }

        void SetSearchType(SearchType mode)
        {
            mData.searchType = mode;
            mTreeView?.Reload();
        }

        void SetSearchMode(SearchMode mode)
        {
            mData.searchMode = mode;
            mTreeView?.Reload();
        }

        void SetSearchMatchCase(bool matchCase)
        {
            mData.searchMatchCase = matchCase;
            mTreeView?.Reload();
        }

        public CodeAssistantData data { get { return mData; } }

        CodeAssistantTreeView mTreeView;
        CodeAssistantData mData = new CodeAssistantData();
        SearchField mSearchField;
        TreeViewState mState;
    }

    //[Important]
    partial class CodeAssistantTreeView : TreeView
    {
        class Item : TreeViewItem
        {
            public Item(int id, int depth, string displayName, string key, bool hasChildren, string description, Color color, int childCount) : base(id, depth, displayName)
            {
                mKey = key;
                mHasChildren = hasChildren;
                mDescription = description;
                mColor = color;
                mChildCount = childCount;
            }

            public string key { get { return mKey; } }
            public string description { get { return mDescription; } }
            public Color color { get { return mColor; } }
            public override bool hasChildren => mHasChildren;
            public int childCount { get { return mChildCount; } }
            public virtual int rows { get { return -1; } }

            //用来生成tree item id
            string mKey;
            string mDescription;
            Color mColor;
            int mChildCount;
            bool mHasChildren;
        }

        class GroupItem : Item
        {
            public GroupItem(int id, int depth, string displayName, string key, bool hasChildren, string description, Color color, GroupInfo group) : base(id, depth, displayName, key, hasChildren, description, color, group.types.Count)
            {
                mGroup = group;
            }

            public GroupInfo group { get { return mGroup; } }

            GroupInfo mGroup;
        }

        class TypeItem : Item
        {
            public TypeItem(int id, int depth, string displayName, string key, bool hasChildren, string description, Color color, TypeInfo type) : base(id, depth, displayName, key, hasChildren, description, color, type.methods.Count + type.properties.Count)
            {
                mType = type;
            }

            public TypeInfo type { get { return mType; } }
            public override int rows { get { return mType.rows; } }

            TypeInfo mType;
        }

        class MethodItem : Item
        {
            public MethodItem(int id, int depth, string displayName, string key, bool hasChildren, string description, Color color, MethodInfo method) : base(id, depth, displayName, key, hasChildren, description, color, 0)
            {
                mMethod = method;
            }

            public MethodInfo method { get { return mMethod; } }
            public override int rows { get { return mMethod.rows; } }

            MethodInfo mMethod;
        }

        class PropertyItem : Item
        {
            public PropertyItem(int id, int depth, string displayName, string key, bool hasChildren, string description, Color color, PropertyInfo property) : base(id, depth, displayName, key, hasChildren, description, color, 0)
            {
                mProperty = property;
            }

            public PropertyInfo property { get { return mProperty; } }
            public override int rows { get { return mProperty.rows; } }

            PropertyInfo mProperty;
        }

        public CodeAssistantTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            columnIndexForTreeFoldouts = 0;
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            Reload();
        }

        public void Build(CodeAssistantData data)
        {
            m_Data = data;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem() { id = 0, depth = -1, displayName = "Root" };

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();
            m_IDToItems.Clear();

            if (m_Data != null)
            {
                if (!string.IsNullOrEmpty(m_Data.searchText))
                {
                    CreateSearchResult();
                }
                else
                {
                    CreateTypes();
                    CreateGroups();
                }

                SetupParentsAndChildrenFromDepths(root, m_Rows);

                if (m_SelectionID != 0)
                {
                    SetSelection(new List<int>() { m_SelectionID });
                    m_SelectionID = 0;
                }
            }
            return m_Rows;
        }

        protected override void BeforeRowsGUI()
        {
            base.BeforeRowsGUI();
            if (m_SelectedType != null)
            {
                //SetSelection(mSelectedNodesInSearch);
                //if (mSelectedNodesInSearch.Count > 0)
                //{
                //    state.scrollPos = new Vector2(0, mNextScrollY);
                //    mNextScrollY = 0;
                //}

                m_SelectedType = null;
                m_SelectedProperty = null;
                m_SelectedMethod = null;
            }
        }

        void CreateTypes()
        {
            string keyName = "All Types";
            var id = AllocateID(keyName);
            var item = new Item(id, 0, keyName, keyName, m_Data.allTypes.Count > 0, "", Color.white, m_Data.allTypes.Count);
            AddItem(id, item);
            if (m_SelectedType != null)
            {
                SetExpanded(id, true);
            }
            if (IsExpanded(id))
            {
                for (int i = 0; i < m_Data.allTypes.Count; ++i)
                {
                    if ((m_Data.allTypes[i].attribute & m_Data.attributeDisplayMask) != 0)
                    {
                        CreateType(m_Data.allTypes[i], item.depth + 1, i, keyName);
                    }
                }
            }
            else
            {
                item.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateGroups()
        {
            string keyName = "All Groups";
            var id = AllocateID(keyName);
            var groupsItem = new Item(id, 0, keyName, keyName, m_Data.groups.Count > 0, "", Color.white, m_Data.groups.Count);
            AddItem(id, groupsItem);
            if (IsExpanded(id))
            {
                for (int i = 0; i < m_Data.groups.Count; ++i)
                {
                    CreateGroup(m_Data.groups[i], i, groupsItem.depth + 1);
                }
            }
            else
            {
                groupsItem.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateGroup(GroupInfo group, int idx, int depth)
        {
            string keyName = $"Group {idx}";
            var id = AllocateID(keyName);
            var groupsItem = new GroupItem(id, depth, keyName, keyName, group.types.Count > 0, group.ToString(), Color.white, group);
            AddItem(id, groupsItem);

            if (IsExpanded(id))
            {
                for (int i = 0; i < group.types.Count; ++i)
                {
                    CreateType(group.types[i], depth + 1, i, "");
                }
            }
            else
            {
                groupsItem.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateType(TypeInfo type, int depth, int idx, string parentKey)
        {
            string key = $"{parentKey}.{type.nameInfo.name}-{idx}";
            var id = AllocateID(key);
            var typeItem = new TypeItem(id, depth, type.nameInfo.modifiedName, key, type.properties.Count > 0 || type.methods.Count > 0, type.description, Utility.GetTypeColor(type), type);
            AddItem(id, typeItem);
            if (type.rows == -1)
            {
                type.rows = m_Rows.Count - 1;
            }
            if (m_SelectedType == type)
            {
                SetExpanded(id, true);
                if (m_SelectedMethod == null && m_SelectedProperty == null)
                {
                    state.scrollPos = new Vector2(0, (m_Rows.Count - 1) * rowHeight);
                    m_SelectionID = id;
                }
            }
            
            if (IsExpanded(id))
            {
                CreateMethods(type, depth + 1, key);
                CreateProperties(type, depth + 1, key);
                CreateBaseTypes(type, depth + 1, key);
                CreateDerivedTypes(type, depth + 1, key);
            }
            else
            {
                typeItem.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateBaseTypes(TypeInfo type, int depth, string parentKey)
        {
            string key = $"{parentKey}.Base.{type.nameInfo.name}-types";
            var id = AllocateID(key);
            var item = new Item(id, depth, "Base", key, type.baseTypes.Count > 0, "", Color.white, type.baseTypes.Count);
            AddItem(id, item);

            if (IsExpanded(id))
            {
                for (int i = 0; i < type.baseTypes.Count; ++i)
                {
                    CreateTypeOnly(type.baseTypes[i], depth + 1, i, key);
                }
            }
            else
            {
                item.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateDerivedTypes(TypeInfo type, int depth, string parentKey)
        {
            string key = $"{parentKey}.Derived.{type.nameInfo.name}-types";
            var id = AllocateID(key);
            var item = new Item(id, depth, "Derived", key, type.derivedTypes.Count > 0, "", Color.white, type.derivedTypes.Count);
            AddItem(id, item);

            if (IsExpanded(id))
            {
                for (int i = 0; i < type.derivedTypes.Count; ++i)
                {
                    CreateTypeOnly(type.derivedTypes[i], depth + 1, i, key);
                }
            }
            else
            {
                item.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateTypeOnly(TypeInfo type, int depth, int idx, string extraKey)
        {
            string key = $"{type.nameInfo.name}-{idx} {extraKey}";
            var id = AllocateID(key);
            var typeItem = new TypeItem(id, depth, type.nameInfo.modifiedName, key, false, type.description, Utility.GetTypeColor(type), type);
            AddItem(id, typeItem);
            typeItem.children = CreateChildListForCollapsedParent();
        }

        void CreateMethods(TypeInfo type, int depth, string parentKey)
        {
            string key = $"{parentKey}.Methods.{type.nameInfo.name}-methods";
            var id = AllocateID(key);
            var methodsItem = new Item(id, depth, "Methods", key, type.methods.Count > 0, "", Color.white, type.methods.Count);
            AddItem(id, methodsItem);

            if (m_SelectedType == type)
            {
                SetExpanded(id, true);
            }

            if (IsExpanded(id))
            {
                for (int i = 0; i < type.methods.Count; ++i)
                {
                    CreateMethod(type.methods[i], depth + 1, i, key);
                }
            }
            else
            {
                methodsItem.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateMethod(MethodInfo method, int depth, int idx, string extraKey)
        {
            if ((m_Data.attributeDisplayMask & method.attribute) == 0)
            {
                return;
            }

            string key = $"{method.name}-{idx}-{extraKey}";
            var id = AllocateID(key);
            var methodItem = new MethodItem(id, depth, method.modifiedFullName, key, false, method.description, Utility.METHOD_COLOR, method);
            AddItem(id, methodItem);
            if (method.rows == -1)
            {
                method.rows = m_Rows.Count - 1;
            }
            if (m_SelectedMethod == method)
            {
                state.scrollPos = new Vector2(0, (m_Rows.Count - 1) * rowHeight);
                m_SelectionID = id;
            }

            methodItem.children = CreateChildListForCollapsedParent();
        }

        void CreateProperties(TypeInfo type, int depth, string parentKey)
        {
            string key = $"{parentKey}.Properties.{type.nameInfo.name}-properties";
            var id = AllocateID(key);
            var propertyItem = new Item(id, depth, "Properties", key, type.properties.Count > 0, "", Color.white, type.properties.Count);
            AddItem(id, propertyItem);

            if (m_SelectedType == type)
            {
                SetExpanded(id, true);
            }

            if (IsExpanded(id))
            {
                for (int i = 0; i < type.properties.Count; ++i)
                {
                    CreateProperty(type.properties[i], depth + 1, i, key);
                }
            }
            else
            {
                propertyItem.children = CreateChildListForCollapsedParent();
            }
        }

        void CreateProperty(PropertyInfo property, int depth, int idx, string extraKey)
        {
            if ((m_Data.attributeDisplayMask & property.attribute) == 0)
            {
                return;
            }

            string key = $"{property.name}-{idx}-{extraKey}";
            var id = AllocateID(key);

            var propertyItem = new PropertyItem(id, depth, property.fullName, key, false, property.description, Utility.METHOD_COLOR, property);
            AddItem(id, propertyItem);
            if (property.rows == -1)
            {
                property.rows = m_Rows.Count - 1;
            }

            if (m_SelectedProperty == property)
            {
                state.scrollPos = new Vector2(0, (m_Rows.Count - 1) * rowHeight);
                m_SelectionID = id;
            }

            propertyItem.children = CreateChildListForCollapsedParent();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (m_Style == null)
            {
                m_Style = new GUIStyle(GUI.skin.textField);
            }

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var item = args.item as Item;
                CellGUI(args.GetCellRect(i), item, (ColumnIndex)args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, Item item, ColumnIndex column, ref RowGUIArgs args)
        {
            // Center the cell rect vertically using EditorGUIUtility.singleLineHeight.
            // This makes it easier to place controls and icons in the cells.
            CenterRectUsingSingleLineHeight(ref cellRect);

            m_Style.normal.textColor = item.color;

            switch (column)
            {
                case ColumnIndex.Name:
                    {
                        var toggleRect = cellRect;
                        toggleRect.x += GetContentIndent(item);
                        EditorGUI.LabelField(toggleRect, item.displayName, m_Style);
                        break;
                    }
                case ColumnIndex.ChildCount:
                    {
                        GUI.enabled = false;
                        if (item.hasChildren && item.children != null)
                        {
                            //EditorGUI.LabelField(cellRect, item.childCount.ToString(), mStyle);
                            EditorGUI.LabelField(cellRect, item.id.ToString(), m_Style);
                        }
                        GUI.enabled = true;
                        break;
                    }
                case ColumnIndex.Description:
                    {
                        EditorGUI.LabelField(cellRect, item.description, m_Style);
                        break;
                    }
                default:
                    Debug.Assert(false, "todo");
                    break;
            }
        }

        protected override void ContextClickedItem(int id)
        {
            GenericMenu menu = new GenericMenu();

            var item = GetTreeViewItem(id) as Item;

            if (item != null)
            {
                menu.AddItem(new GUIContent("Copy Name"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = item.displayName;
                });

                if (item is GroupItem groupItem)
                {
                    menu.AddItem(new GUIContent("Show In Graph"), false, () =>
                    {
                        EditorWindow.GetWindow<CodeAssistantEditor>().SetGroup(groupItem.group);
                    });
                }
                else if (item is TypeItem typeItem)
                {
                    menu.AddItem(new GUIContent("Show In Graph"), false, () =>
                    {
                        EditorWindow.GetWindow<CodeAssistantEditor>().SetType(typeItem.type);
                    });
                }
            }

            menu.ShowAsContext();
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = GetTreeViewItem(id);
            if (item is TypeItem typeItem)
            {
                Utility.OpenCSFile(m_Data.vsDTEFilePath, typeItem.type.filePath, typeItem.type.lineNumber, m_Data.csProjectName);
            }
            else if (item is GroupItem groupItem)
            {
                if (groupItem.group.types.Count == 1)
                {
                    var type = groupItem.group.types[0];
                    Utility.OpenCSFile(m_Data.vsDTEFilePath, type.filePath, type.lineNumber, m_Data.csProjectName);
                }
            }
            else if (item is MethodItem methodItem)
            {
                Utility.OpenCSFile(m_Data.vsDTEFilePath, methodItem.method.owner.filePath, methodItem.method.lineNumber, m_Data.csProjectName);
            }
            else if (item is PropertyItem propertyItem)
            {
                Utility.OpenCSFile(m_Data.vsDTEFilePath, propertyItem.property.owner.filePath, propertyItem.property.lineNumber, m_Data.csProjectName);
            }
        }

        TreeViewItem GetTreeViewItem(int id)
        {
            m_IDToItems.TryGetValue(id, out var item);
            return item;
        }

        int AllocateID(string name)
        {
            bool found = m_NameToIDs.TryGetValue(name, out var id);
            if (found == false)
            {
                id = ++m_NextID;
                m_NameToIDs[name] = id;
            }
            return id;
        }

        void Expand(int id, bool expand, bool recursively)
        {
            SetExpanded(id, expand);
            if (recursively)
            {
                var item = GetTreeViewItem(id);
                if (item != null && item.children != null)
                {
                    foreach (var child in item.children)
                    {
                        if (child != null)
                        {
                            Expand(child.id, expand, true);
                        }
                    }
                }
            }
        }

        void AddItem(int id, TreeViewItem item)
        {
            m_IDToItems.Add(id, item);
            m_Rows.Add(item);
        }

        private Dictionary<int, TreeViewItem> m_IDToItems = new Dictionary<int, TreeViewItem>();
        private List<TreeViewItem> m_Rows = new List<TreeViewItem>();
        //当search text清空时选中物体
        private int m_SelectionID;
        private TypeInfo m_SelectedType;
        private MethodInfo m_SelectedMethod;
        private PropertyInfo m_SelectedProperty;
        private Dictionary<string, int> m_NameToIDs = new Dictionary<string, int>();
        private CodeAssistantData m_Data;
        private GUIStyle m_Style;
        private int m_NextID = 100;
        private const float kRowHeights = 20f;
    }
}

