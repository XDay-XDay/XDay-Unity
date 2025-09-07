using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace XDay.PlayerBuildPipeline.Editor
{
    partial class AssetBundleRuleTreeView : TreeView
    {
        private class Item : TreeViewItem
        {
            public Color Color => m_Color;
            public AssetBundleRule Rule => m_Rule;
            public override bool hasChildren => m_Rule.SubRules.Count > 0;

            public Item(int id, int depth, string displayName, AssetBundleRule rule, Color color)
                : base(id, depth, displayName)
            {
                m_Rule = rule;
                m_Color = color;
            }

            private Color m_Color;
            private AssetBundleRule m_Rule;
        }

        public AssetBundleRuleTreeView(TreeViewState state, MultiColumnHeader header) 
            : base(state, header)
        {
            columnIndexForTreeFoldouts = 0;
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f;
            Reload();
        }

        public void Build(AssetBundleRuleConfig config)
        {
            m_Config = config;
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

            if (m_Config != null)
            {
                CreateNode(m_Config.RootRule, 0);

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
        }

        private void CreateNode(AssetBundleRule rule, int depth)
        {
            //var item = new Item(rule, depth, rule.Name, rule, Color.white);
            //AddItem(id, item);
            //if (IsExpanded(id))
            //{
            //    for (int i = 0; i < rule.SubRules.Count; ++i)
            //    {
            //        CreateNode(rule.SubRules[i], depth + 1);
            //    }
            //}
            //else
            //{
            //    item.children = CreateChildListForCollapsedParent();
            //}
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

        private void CellGUI(Rect cellRect, Item item, ColumnIndex column, ref RowGUIArgs args)
        {
            // Center the cell rect vertically using EditorGUIUtility.singleLineHeight.
            // This makes it easier to place controls and icons in the cells.
            CenterRectUsingSingleLineHeight(ref cellRect);

            m_Style.normal.textColor = item.Color;

            switch (column)
            {
                case ColumnIndex.Name:
                    {
                        var toggleRect = cellRect;
                        var deltaX = GetContentIndent(item);
                        toggleRect.x += deltaX;
                        toggleRect.width -= deltaX + m_Style.padding.right;
                        item.Rule.Name = EditorGUI.TextField(toggleRect, item.Rule.Name, m_Style);
                        break;
                    }
                case ColumnIndex.Path:
                    {
                        item.Rule.RelativePath = EditorGUI.TextField(cellRect, item.Rule.RelativePath, m_Style);
                        break;
                    }
                case ColumnIndex.BundleName:
                    {
                        item.Rule.BundleName = EditorGUI.TextField(cellRect, item.Rule.BundleName, m_Style);
                        break;
                    }
                case ColumnIndex.RuleType:
                    {
                        item.Rule.Type = (AssetBundleRuleType)EditorGUI.EnumPopup(cellRect, item.Rule.Type, m_Style);
                        break;
                    }
                case ColumnIndex.Description:
                    {
                        item.Rule.Description = EditorGUI.TextField(cellRect, item.Rule.Description, m_Style);
                        break;
                    }
                case ColumnIndex.AddChild:
                    {
                        if (GUI.Button(cellRect, "Add Child"))
                        {
                            var newRule = new AssetBundleRule()
                            {
                                ParentRule = item.Rule,
                            };
                            item.Rule.SubRules.Add(newRule);
                        }
                        break;
                    }
                default:
                    Debug.Assert(false, "todo");
                    break;
            }
        }

        protected override void ContextClickedItem(int id)
        {
            GenericMenu menu = new();

            var item = GetTreeViewItem(id) as Item;
            if (item != null)
            {
                menu.AddItem(new GUIContent("Copy Name"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = item.displayName;
                });
            }

            menu.ShowAsContext();
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = GetTreeViewItem(id);
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

        private readonly Dictionary<int, TreeViewItem> m_IDToItems = new();
        private readonly List<TreeViewItem> m_Rows = new();
        private Dictionary<string, int> m_NameToIDs = new();
        private GUIStyle m_Style;
        private int m_NextID = 100;
        private AssetBundleRuleConfig m_Config;
        private int m_SelectionID;
        private const float kRowHeights = 20f;
    }
}
