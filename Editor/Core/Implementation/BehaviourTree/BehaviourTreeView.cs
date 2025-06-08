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

using UnityEngine;
using System.Collections.Generic;
using XDay.UtilityAPI.Editor;
using UnityEditor;
using XDay.WorldAPI;

namespace XDay.BehaviourTreeAPI.Editor
{ 
    internal partial class BehaviourTreeView : GraphNodeEditor
    {
        public BehaviourTreeView() : base(100000, 100000)
        {
            m_ButtonBackground = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }
            m_ButtonBackground.SetPixels32(pixels);
            m_ButtonBackground.Apply();

            m_SuccessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/SuccessState.png"));
            m_FailTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/FailState.png"));
            m_RunningTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/RunningState.png"));
            m_DynamicTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/Dynamic.png"));
            m_BreakpointTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/Breakpoint.png"));
        }

        public void Init(BehaviourTree tree, float windowWidth, float windowHeight)
        {
            ResetViewPosition();
            m_AlignToGrid = true;
            Reset();
            m_Viewer.SetWorldPosition(-windowWidth * 0.5f, -windowHeight * 0.5f);

            SetTree(tree);
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                Reset();
            }
        }

        public void OnDestroy()
        {
            Reset();
        }

        protected override void OnReset()
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            m_TreeFilePath = null;
            m_TempList.Clear();
            SetTree(null);
            Object.DestroyImmediate(m_ButtonBackground);
            m_ButtonBackground = null;
            foreach (var node in m_AllNodes)
            {
                node.OnDestroy();
            }
            m_AllNodes.Clear();
            m_SelectionInfo.Nodes.Clear();
            m_SelectionInfo.Part = Part.None;
            m_Effects.Clear();
        }

        private void CreateNodeView(Node node, Vector2 worldPosition)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/{node.GetType().Name}.png"));
            var nodeView = new NodeView(node, texture, AlignToGrid, this)
            {
                WorldPosition = worldPosition
            };
            m_AllNodes.Add(nodeView);
        }

        private void OnCreateNode(Node node, Node source)
        {
            var pos = Window2World(m_CreateWindowPosition);
            if (source != null)
            {
                pos = GetNodeView(source).WorldPosition + new Vector2(30, 30);
            }
            CreateNodeView(node, pos);
        }

        private void OnDestroyNode(Node node)
        {
            var nodeView = GetNodeView(node);
            nodeView?.OnDestroy();
            m_AllNodes.Remove(nodeView);
        }

        private NodeView GetNodeView(Node node)
        {
            foreach (var nodeView in m_AllNodes)
            {
                if (nodeView.Node == node)
                {
                    return nodeView;
                }
            }
            return null;
        }

        private void SetTree(BehaviourTree tree)
        {
            if (m_Tree != tree)
            {
                m_Tree = tree;
                if (m_Tree != null)
                {
                    m_Tree.EventCreateNode += OnCreateNode;
                    m_Tree.EventDestroyNode += OnDestroyNode;
                }

                if (tree != null)
                {
                    foreach (var kv in m_Tree.Nodes)
                    {
                        var node = kv.Value;
                        CreateNodeView(node, GetNodePosition(tree.State, node.ID));
                    }

                    m_Viewer.SetZoom(tree.State.Zoom);
                    m_Viewer.SetWorldPosition(tree.State.ViewPosition.x, tree.State.ViewPosition.y);
                }

                EditorApplication.update -= Update;
                EditorApplication.update += Update;
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;

                Repaint();
            }
        }

        private void OnSceneGUI(SceneView view)
        {
        }

        private void GetTopMostNodes(List<NodeView> input, List<NodeView> output)
        {
            output.Clear();
            for (var i = 0; i < input.Count; i++)
            {
                var testNode = input[i].Node;
                bool topmost = true;
                for (var j = i + 1; j < input.Count; j++)
                {
                    var otherNode = input[j].Node;
                    if (testNode.IsDescendantOf(otherNode))
                    {
                        topmost = false;
                        break;
                    }
                }
                if (topmost)
                {
                    output.Add(input[i]);
                }
            }
        }

        protected override void OnZoomChanged()
        {
            for (var i = 0; i < m_AllNodes.Count; i++)
            {
                m_AllNodes[i].Move(Vector2.zero);
            }
        }

        private void Update()
        {
            UpdateNodeEffects();
        }

        private BehaviourTree m_Tree;
        private readonly List<NodeView> m_AllNodes = new();
        private readonly SelectionInfo m_SelectionInfo = new();
        private readonly List<NodeView> m_TempList = new();
        private string m_TreeFilePath;
        private Texture2D m_SuccessTexture;
        private Texture2D m_FailTexture;
        private Texture2D m_RunningTexture;
        private Texture2D m_DynamicTexture;
        private Texture2D m_BreakpointTexture;

        private class SelectionInfo
        {
            public List<NodeView> Nodes = new();
            public Part Part = Part.None;
        };
    };

    internal enum Part
    {
        None,
        Top,
        Bottom,
        Center,
    }
}