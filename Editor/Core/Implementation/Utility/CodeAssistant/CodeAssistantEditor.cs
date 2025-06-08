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
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    public partial class CodeAssistantEditor : EditorWindow
    {
        [MenuItem("XDay/Code Assistant/Open")]
        static void Open()
        {
            var dlg = GetWindow<CodeAssistantEditor>("Code Graph");
            dlg.Show();
        }

        public static CodeAssistantEditor GetInstance()
        {
            return GetWindow<CodeAssistantEditor>("Code Graph", false);
        }

        void OnEnable()
        {
            mGraphView = new GraphView();
        }

        void OnDisable()
        {
            mGraphView.OnDestroy();    
        }

        void OnGUI()
        {
            var data = Utility.GetData();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start", GUILayout.MaxWidth(100)))
            {
                var graph = new Graph();
                graph.Create(data.groups);
                SetGraph(graph);
            }

            EditorGUIUtility.labelWidth = 100;
            data.displayMode = (DisplayMode)EditorGUILayout.EnumPopup("Display Mode", data.displayMode, GUILayout.MaxWidth(250));
            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndHorizontal();

            mGraphView.Render(position.width, position.height, Repaint);
            Repaint();
        }

        public void SetGroup(GroupInfo group)
        {
            var graph = new Graph();
            graph.Create(new List<GroupInfo>() { group });
            SetGraph(graph);
        }

        public void SetType(TypeInfo type)
        {
            var graph = new Graph();
            graph.Create(type);
            SetGraph(graph);
        }

        void SetGraph(Graph graph)
        {
            mGraphView.SetGraph(graph, position.width, position.height);
        }

        GraphView mGraphView;
    }
}

