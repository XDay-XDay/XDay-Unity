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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.BehaviourTreeAPI.Editor
{
    internal partial class BehaviourTreeView
    {
        private void Create(BehaviourTreeState treeState, string name)
        {
            Reset();

            var tree = treeState.CreateTree(1, name);
            SetTree(tree);
        }

        private void Load()
        {
            var selection = Selection.activeObject;
            if (selection is not BehaviourTreeState treeState)
            {
                EditorUtility.DisplayDialog("出错了", "选中BehaviourTree再Load", "确定");
                return;
            }

            LoadGlobalVariables();

            var path = AssetDatabase.GetAssetPath(treeState);
            Create(treeState, Helper.GetPathName(path, false));
            m_TreeFilePath = path;
        }

        private void LoadGlobalVariables()
        {
            var state = AssetDatabase.LoadAssetAtPath<VariableManagerState>("Assets/Resource/GlobalVar.asset");
            if (state != null)
            {
                Global.GlobalVariableManager = state.CreateVariableManager();
            }
        }

        private Vector2 GetNodePosition(BehaviourTreeState treeState, int nodeID)
        {
            var nodeState = treeState.GetNodeState(nodeID);
            return nodeState.Position;
        }
    }
}