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

using System.IO;
using UnityEditor;
using XDay.UtilityAPI;

namespace XDay.BehaviourTreeAPI.Editor
{
    internal partial class BehaviourTreeView
    {
        private void Save()
        {
            var root = m_Tree.Root;
            if (root != null)
            {
                Reorder(root);
            }

            bool ok = AssureSavePath();
            if (ok)
            {
                SaveGlobalVariables();

                var treeState = m_Tree.CreateState();
                SetEditorState(treeState);

                if (File.Exists(m_TreeFilePath))
                {
                    ReplaceAsset(treeState);
                }
                else
                {
                    AssetDatabase.CreateAsset(treeState, m_TreeFilePath);
                }
            }

            AssetDatabase.Refresh();
        }

        private void ReplaceAsset(BehaviourTreeState newState)
        {
            var old = AssetDatabase.LoadAssetAtPath<BehaviourTreeState>(m_TreeFilePath);
            old.Assign(newState);
            EditorUtility.SetDirty(old);
            AssetDatabase.SaveAssets();
        }

        private void SetEditorState(BehaviourTreeState state)
        {
            state.ViewPosition = m_Viewer.GetWorldPosition();
            state.Zoom = m_Viewer.GetZoom();

            foreach (var node in m_AllNodes)
            {
                NodeState nodeState = state.GetNodeState(node.Node.ID);
                nodeState.Position = node.WorldPosition;
            }
        }

        private bool AssureSavePath()
        {
            if (!string.IsNullOrEmpty(m_TreeFilePath))
            {
                return true;
            }

            string path = EditorUtility.SaveFilePanel("保存", "Assets/", "BehaviourTree", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                m_TreeFilePath = Helper.ToUnityPath(path);
            }

            return !string.IsNullOrEmpty(m_TreeFilePath);
        }

        private void SaveGlobalVariables()
        {
            var state = Global.GlobalVariableManager.CreateState();
            AssetDatabase.CreateAsset(state, "Assets/Resource/GlobalVar.asset");
        }

        private void Reorder(Node node)
        {
            if (node is CompoundNode comp)
            {
                comp.Children.Sort(
                (a, b) =>
                {
                    var va = GetNodeView(a);
                    var vb = GetNodeView(b);
                    return va.WorldPosition.x.CompareTo(vb.WorldPosition.x);
                });

                foreach (var child in comp.Children)
                {
                    Reorder(child);
                }
            }
        }
    }
}
