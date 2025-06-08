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

#if UNITY_EDITOR

//#define USE_THREAD

using UnityEngine;
using System.Collections.Generic;
using XDay.UtilityAPI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XDay.NavigationAPI
{
    [ExecuteInEditMode]
    public class MyNavigationSystemDebugger : MonoBehaviour
    {
        public bool Show = false;
        public bool ShowNavigationEdges = false;
        public bool ShowAllVertices = false;
        public bool ShowAllEdges = false;
        public bool ShowTriangles = true;
        public bool ShowNeighbourEdges = true;
        public bool OptimizePath = true;
        public bool ShowFunnels = false;
        public bool ShowPathEdges = true;
        public bool ShowCheckedVertices = true;
        public bool ShowLeftVertex = false;
        public bool ShowPathResult = true;
        public float EdgeColliderSize = 0.5f;
        public float VertexSize = 1.0f;
        public float RandomOffset = 20.0f;
        public int PathFindingCount = 1000;
        public Transform StartTransform;
        public Transform EndTransform;
        public MyNavigationSystem System => m_System;

        public void Init(MyNavigationSystem system)
        {
            m_System = system;
        }

        private MyNavigationSystem m_System;
    }

    [CustomEditor(typeof(MyNavigationSystemDebugger))]
    class MyNavigationSystemDebuggerEditor : Editor
    {
        List<Vector3> mPath = new List<Vector3>();
        List<Vector3> mCustomNavigationPath = new List<Vector3>();
        string[] mFunnelNames;
        string[] mPassNames;
        int mSelectedFunnel = -1;
        int mSelectedPass = -1;
        int mProcessedRequest = 0;
        List<List<Vector3>> mPaths = new List<List<Vector3>>();
        IObjectPool<List<Vector3>> mPathPool = IObjectPool<List<Vector3>>.Create(() => { return new List<Vector3>(); }, capacity:100);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var debugger = target as MyNavigationSystemDebugger;
            var navigationSystem = debugger.System;
            if (navigationSystem != null)
            {
                var record = navigationSystem.record;

                int passCount = record.passes.Count;
                if (mPassNames == null || mPassNames.Length != passCount)
                {
                    mPassNames = new string[passCount];
                    for (int i = 0; i < passCount; ++i)
                    {
                        mPassNames[i] = $"Pass {i}";
                    }
                }
                if (mSelectedPass >= passCount)
                {
                    mSelectedPass = -1;
                }
                if (mPassNames.Length > 0 && mSelectedPass == -1)
                {
                    mSelectedPass = 0;
                }
                mSelectedPass = EditorGUILayout.Popup("Pass", mSelectedPass, mPassNames);

                if (mSelectedPass >= 0)
                {
                    int funnelCount = record.passes[mSelectedPass].funnels.Count;
                    if (mFunnelNames == null || mFunnelNames.Length != funnelCount)
                    {
                        mFunnelNames = new string[funnelCount];
                        for (int i = 0; i < funnelCount; ++i)
                        {
                            mFunnelNames[i] = $"funnel {i}";
                        }
                    }
                    if (mSelectedFunnel >= funnelCount)
                    {
                        mSelectedFunnel = -1;
                    }
                    if (mFunnelNames.Length > 0 && mSelectedFunnel == -1)
                    {
                        mSelectedFunnel = 0;
                    }
                    mSelectedFunnel = EditorGUILayout.Popup("Funnel", mSelectedFunnel, mFunnelNames);
                }
            }

            if (GUILayout.Button("Find Path"))
            {
                var start = new Vector3(139.66f, 0, 194.47f);
                var end = new Vector3(165.23f, 0, 191.51f);

                if (start != null)
                {
                    start = debugger.StartTransform.position;
                }
                if (end != null)
                {
                    end = debugger.EndTransform.position;
                }

#if USE_THREAD
                mProcessedRequest = 0;
                mPaths.Clear();
                for (int i = 0; i < debugger.pathFindingCount; ++i)
                {
                    int id = i;
                    //var path = mPathPool.Require();
                    var path = new List<Vector3>();
                    var startOffset = MathToolkit.ToVector3(Random.insideUnitCircle) * debugger.randomOffset;
                    var endOffset = MathToolkit.ToVector3(Random.insideUnitCircle) * debugger.randomOffset;
                    navigationSystem.RequestFindPath(id, start + startOffset, end + endOffset, path, PathFindingOption.SimplifyPath, (MyNavigationSystem.PathFindingRequest request) =>
                    {
                        //mPathPool.Release(path);
                        ++mProcessedRequest;
                        mPaths.Add(request.result);
                        //UnityEngine.Debug.Log($"{id} path finding finish");
                    });
                }
#else
                PathFindingOption option = PathFindingOption.XYZAxis;
                if (debugger.OptimizePath)
                {
                    option |= PathFindingOption.SimplifyPath;
                }
                navigationSystem.FindPath(start, end, mPath, null, option);
#endif
            }

            if (GUILayout.Button("Show Processed Request"))
            {
                UnityEngine.Debug.LogError(mProcessedRequest);
            }
        }

        void OnSceneGUI()
        {
            var debugger = target as MyNavigationSystemDebugger;
            if (!debugger.Show)
            {
                return;
            }

            var navigationSystem = debugger.System;

            if (navigationSystem != null)
            {
                var originalColor = Handles.color;
                var originalTextColor = GUI.skin.label.normal.textColor;

                var e = Event.current;
                var worldPos = EditorHelper.MousePositionToWorldRay(e.mousePosition, out _);

                if (debugger.ShowTriangles)
                {
                    Triangle triangle = navigationSystem.FindTriangleContains3D(worldPos, out _);
                    if (triangle != null)
                    {
                        int edgeCount = triangle.GetNavigationEdgeCount();
                        for (int i = 0; i < edgeCount; ++i)
                        {
                            int edgeIndex = triangle.GetNavigationEdgeIndex(i);
                            navigationSystem.GetNavigationEdgeVertex(edgeIndex, out var start, out var end);

                            Handles.color = Color.blue;
                            Handles.DrawLine(start, end);

                            navigationSystem.GetTriangleVertexPositions(triangle.Index, out Vector3 v0, out Vector3 v1, out Vector3 v2);
                            var pos = (v0 + v1 + v2) / 3;
                            Handles.color = Color.red;
                            
                            GUI.skin.label.normal.textColor = Color.red;
                            Handles.Label(pos, triangle.Index.ToString());
                            Handles.Label(v0, triangle.V0.ToString());
                            Handles.Label(v1, triangle.V1.ToString());
                            Handles.Label(v2, triangle.V2.ToString());
                            GUI.skin.label.normal.textColor = Color.magenta;
                            Handles.Label((start + end) / 2, edgeIndex.ToString());
                        }
                    }
                }

                if (debugger.ShowNavigationEdges)
                {
                    var edges = navigationSystem.NavigationEdges;
                    for (int i = 0; i < edges.Count; ++i)
                    {
                        var a = navigationSystem.GetVertex(edges[i].VertexIndex0);
                        var b = navigationSystem.GetVertex(edges[i].VertexIndex1);
                        Handles.color = Color.red;
                        Handles.DrawLine(a, b);
                    }
                }

                if (debugger.ShowNeighbourEdges)
                {
                    List<int> neighbourEdges = navigationSystem.GetNeighbourNavigationEdges(worldPos, debugger.EdgeColliderSize);
                    if (neighbourEdges != null)
                    {
                        for (int i = 0; i < neighbourEdges.Count; ++i)
                        {
                            navigationSystem.GetNavigationEdgeVertex(neighbourEdges[i], out var start, out var end);
                            Handles.color = Color.magenta;
                            Handles.DrawLine(start, end);
                        }
                    }
                }

                if (debugger.ShowFunnels)
                {
                    if (mSelectedPass >= 0)
                    {
                        var record = navigationSystem.record;
                        var pass = record.passes[mSelectedPass];
                        if (mSelectedFunnel >= 0)
                        {
                            var funnel = pass.funnels[mSelectedFunnel];

                            Handles.color = Color.black;
                            Handles.DrawLine(funnel.apex, funnel.leftVertex);
                            Handles.DrawLine(funnel.leftVertex, funnel.rightVertex);
                            Handles.DrawLine(funnel.rightVertex, funnel.apex);
                        }
                    }
                }

                if (debugger.ShowLeftVertex)
                {
                    if (mSelectedPass >= 0)
                    {
                        var edgesLeftVertexIndex = navigationSystem.edgesLeftVertexIndex;
                        var pathEdges = navigationSystem.pathEdges;
                        for (int i = 0; i < edgesLeftVertexIndex.Count; ++i)
                        {
                            navigationSystem.GetNavigationEdgeVertex(pathEdges[i], out var edgeStart, out var edgeEnd);
                            Handles.color = Color.red;
                            Vector3 pos;
                            if (edgesLeftVertexIndex[i] == 0)
                            {
                                pos = edgeStart;
                            }
                            else
                            {
                                pos = edgeEnd;
                            }
                            Handles.DrawSolidDisc(pos, Vector3.up, debugger.VertexSize);
                        }
                    }
                }

                if (debugger.ShowPathEdges)
                {
                    var pathEdges = navigationSystem.pathEdges;
                    for (int i = 0; i < pathEdges.Count; ++i)
                    {
                        navigationSystem.GetNavigationEdgeVertex(pathEdges[i], out var start, out var end);
                        Handles.color = Color.blue;
                        Handles.DrawLine(start, end);
                    }
                }

                if (debugger.ShowCheckedVertices)
                {
                    var record = navigationSystem.record;
                    if (mSelectedPass >= 0)
                    {
                        var pass = record.passes[mSelectedPass];
                        int idx = 0;
                        foreach (var vertex in pass.checkedVertex)
                        {
                            Handles.color = Color.blue;
                            Handles.DrawWireDisc(vertex, Vector3.up, debugger.VertexSize);
                            GUI.skin.label.normal.textColor = Color.black;
                            Handles.Label(vertex, $"{idx}");
                            ++idx;
                        }
                    }
                }

                if (debugger.ShowPathResult)
                {
#if USE_THREAD
                    for (int i = 0; i < mPaths.Count; ++i)
                    {
                        GUIModule.EditorGUIToolkit.DrawLineStrip(mPaths[i], Color.red, false);
                    }
#else
                    if (mPath != null)
                    {
                        EditorHelper.DrawLineStrip(mPath, Color.red, false);
                    }
#endif
                }

                if (debugger.ShowAllVertices)
                {
                    int idx = 0;
                    foreach (var vertex in navigationSystem.Vertices)
                    {
                        Handles.color = Color.red;
                        Handles.DrawWireDisc(vertex, Vector3.up, debugger.VertexSize);
                        GUI.skin.label.normal.textColor = Color.black;
                        Handles.Label(vertex, $"{idx}");
                        ++idx;
                    }
                }

#if false
                if (debugger.showAllEdges)
                {
                    int idx = 0;
                    var indices = navigationSystem.indices;
                    int triangleCount = indices.Length / 3;
                    for (int i = 0; i < triangleCount; ++i)
                    {

                        Handles.color = Color.blue;
                        var a = navigationSystem.GetVertex(edges[i].vertexIndex0);
                        var b = navigationSystem.GetVertex(edges[i].vertexIndex1);
                        Handles.color = Color.red;
                        Handles.DrawLine(a, b);


                        GUI.skin.label.normal.textColor = Color.black;
                        Handles.Label(vertex, $"{idx}");
                        ++idx;
                    }
                }
#endif

                GUI.skin.label.normal.textColor = originalTextColor;
                Handles.color = originalColor;
            }
        }
    }
}


#endif