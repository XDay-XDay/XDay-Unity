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
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    public partial class MyNavigationSystem
    {
        public class Funnel
        {
            public Funnel(Vector3 apex, Vector3 leftVertex, Vector3 rightVertex)
            {
                mApex = apex;
                this.leftVertex = leftVertex;
                this.rightVertex = rightVertex;
            }

            public void ResetDirty()
            {
                mLeftChange = false;
                mRightChange = false;
            }

            public Funnel Clone()
            {
                return new Funnel(mApex, leftVertex, rightVertex);
            }

            public int GetSide(Vector3 pos)
            {
                return Helper.GetSide(pos - mApex, leftDirection, rightDirection);
            }

            public Vector3 leftDirection { get { return leftVertex - apex; } }
            public Vector3 rightDirection { get { return rightVertex - apex; } }
            public Vector3 leftVertex { get { return mLeftVertex; } set { 
                    if (mLeftVertex != value)
                    {
                        mLeftVertex = value;
                        mLeftChange = true;
                    }
                } }

            public Vector3 rightVertex
            {
                get { return mRightVertex; }
                set
                {
                    if (mRightVertex != value)
                    {
                        mRightVertex = value;
                        mRightChange = true;
                    }
                }
            }

            public Vector3 apex { get { return mApex; } }

            public bool leftChange { get { return mLeftChange; } }
            public bool rightChange { get { return mRightChange; } }

            Vector3 mApex;
            Vector3 mLeftVertex;
            Vector3 mRightVertex;
            bool mLeftChange;
            bool mRightChange;
        }

        //debug
        public class Pass
        {
            public List<Funnel> funnels = new List<Funnel>();
            public List<Vector3> checkedVertex = new List<Vector3>();
        }

        //debug
        public class Record
        {
            public List<Pass> passes = new List<Pass>();
        }

        //use simple stupid funnel algorithm
        void OptimizePath(Vector3 start, Vector3 end, List<int> edges)
        {
            mRecord = new Record();

            mFunnelPath.Clear();
            mFunnelPath.Add(start);

            mPathEdges.Clear();
            mPathEdges.AddRange(edges);

            //区分edge的左右顶点
            FindOutEdgesLeftVertexIndex(start, edges);

            DoOptimizePath(start, end, edges, 0);

            mFunnelPath.Add(end);
        }

        void FindOutEdgesLeftVertexIndex(Vector3 start, List<int> edges)
        {
            mEdgesLeftVertexIndex = new List<int>(edges.Count);

            GetNavigationEdgeVertex(edges[0], out Vector3 edgeVertex0, out Vector3 edgeVertex1);
            var vertex0ToStart = edgeVertex0 - start;
            var vertex1ToStart = edgeVertex1 - start;

            bool isLeft = Helper.IsLeftXZ(vertex0ToStart, vertex1ToStart);

            Vector3 leftVertex;
            Vector3 rightVertex;
            if (isLeft)
            {
                leftVertex = edgeVertex0;
                rightVertex = edgeVertex1;
                mEdgesLeftVertexIndex.Add(0);
            }
            else
            {
                leftVertex = edgeVertex1;
                rightVertex = edgeVertex0;
                mEdgesLeftVertexIndex.Add(1);
            }

            for (int i = 1; i < edges.Count; ++i)
            {
                GetNavigationEdgeVertex(edges[i], out Vector3 vertex0, out Vector3 vertex1);
                if (vertex0 == leftVertex)
                {
                    mEdgesLeftVertexIndex.Add(0);
                    rightVertex = vertex1;
                }
                else if (vertex0 == rightVertex)
                {
                    mEdgesLeftVertexIndex.Add(1);
                    leftVertex = vertex1;
                }
                else if (vertex1 == leftVertex)
                {
                    mEdgesLeftVertexIndex.Add(1);
                    rightVertex = vertex0;
                }
                else if (vertex1 == rightVertex)
                {
                    mEdgesLeftVertexIndex.Add(0);
                    leftVertex = vertex0;
                }
                else
                {
                    Debug.Assert(false, "error");
                }
            }
        }

        void DoOptimizePath(Vector3 apex, Vector3 end, List<int> edges, int funnelEdgeIndex)
        {
            var pass = new Pass();
            mRecord.passes.Add(pass);
            Funnel funnel = CreateFunnel(apex, edges[funnelEdgeIndex]);
            pass.funnels.Add(funnel.Clone());

            if (funnelEdgeIndex + 1 >= edges.Count)
            {
                var endToApex = end - funnel.apex;
                if (Helper.IsLeftXZ(endToApex, funnel.leftDirection))
                {
                    mFunnelPath.Add(funnel.leftVertex);
                }
                else if (Helper.IsLeftXZ(funnel.rightDirection, endToApex))
                {
                    mFunnelPath.Add(funnel.rightVertex);
                }   
            }
            else
            {
                SearchFunnel(pass, funnel, end, edges, funnelEdgeIndex + 1);
            }
        }

        void SearchFunnel(Pass pass, Funnel funnel, Vector3 end, List<int> edges, int startSearchEdgeListIndex)
        {
            int i;
            int updatedLeftEdgeIndex = startSearchEdgeListIndex;
            int updatedRightEdgeIndex = startSearchEdgeListIndex;
            for (i = startSearchEdgeListIndex; i < edges.Count; ++i)
            {
                //find out which edge vertex is left vertex
                GetNavigationEdgeLeftRightVertex(edges[i], mEdgesLeftVertexIndex[i], out Vector3 edgeLeftVertex, out Vector3 edgeRightVertex);

                funnel.ResetDirty();

                if (edgeLeftVertex != funnel.leftVertex)
                {
                    pass.checkedVertex.Add(edgeLeftVertex);
                    int side = Helper.GetSide(funnel.leftDirection, edgeLeftVertex - funnel.apex);
                    if (side == -1)
                    {
                        //funnel left get smaller, update funnel left vertex
                        funnel.leftVertex = edgeLeftVertex;
                        if (funnel.leftChange)
                        {
                            updatedLeftEdgeIndex = i;
                            pass.funnels.Add(funnel.Clone());
                        }
                    }
                }

                if (funnel.rightVertex != edgeRightVertex)
                {
                    pass.checkedVertex.Add(edgeRightVertex);
                    var side = Helper.GetSide(edgeRightVertex - funnel.apex, funnel.rightDirection);
                    if (side == -1)
                    {
                        //funnel right get smaller, update funnel right vertex
                        funnel.rightVertex = edgeRightVertex;
                        if (funnel.rightChange)
                        {
                            updatedRightEdgeIndex = i;
                            pass.funnels.Add(funnel.Clone());
                        }
                    }
                }

                bool borderCross = Helper.IsLeftXZ(funnel.leftDirection, funnel.rightDirection) == false;

                if (borderCross == false && i == edges.Count - 1)
                {
                    var side = funnel.GetSide(end);
                    if (side != 0)
                    {
                        Vector3 newApex;
                        int updatedIndex = -1;
                        if (side == -1)
                        {
                            updatedIndex = updatedLeftEdgeIndex;
                            newApex = funnel.leftVertex;
                        }
                        else
                        {
                            updatedIndex = updatedRightEdgeIndex;
                            newApex = funnel.rightVertex;
                        }

                        mFunnelPath.Add(newApex);

                        for (int k = updatedIndex; k < edges.Count; ++k)
                        {
                            GetNavigationEdgeVertex(edges[k], out var edgeStart, out var edgeEnd);
                            if (edgeStart != newApex && edgeEnd != newApex)
                            {
                                DoOptimizePath(newApex, end, edges, k);
                                return;
                            }
                        }
                    }
                }

                if (borderCross)
                {
                    pass.funnels.RemoveAt(pass.funnels.Count - 1);

                    Vector3 newApex = Vector3.zero;
                    //funnel失效
                    int updatedIndex = -1;
                    if (funnel.leftChange)
                    {
                        updatedIndex = updatedRightEdgeIndex;
                        newApex = funnel.rightVertex;
                        mFunnelPath.Add(funnel.rightVertex);
                    }
                    else
                    {
                        updatedIndex = updatedLeftEdgeIndex;
                        newApex = funnel.leftVertex;
                        mFunnelPath.Add(funnel.leftVertex);
                    }

                    //从更新funnel的edge开始, 找到下一条和newApex不重合的path edge
                    for (int k = updatedIndex; k < edges.Count; ++k)
                    {
                        GetNavigationEdgeVertex(edges[k], out var edgeStart, out var edgeEnd);
                        if (edgeStart != newApex && edgeEnd != newApex)
                        {
                            DoOptimizePath(newApex, end, edges, k);
                            break;
                        }
                    }
                    //开始下一个funnel
                    return;
                }
            }
        }

        Funnel CreateFunnel(Vector3 apex, int edgeIndex)
        {
            GetNavigationEdgeVertex(edgeIndex, out Vector3 edgeVertex0, out Vector3 edgeVertex1);
            var vertex0ToStart = edgeVertex0 - apex;
            var vertex1ToStart = edgeVertex1 - apex;

            bool isLeft = Helper.IsLeftXZ(vertex0ToStart, vertex1ToStart);

            if (isLeft)
            {
                return new Funnel(apex, edgeVertex0, edgeVertex1);
            }
            else
            {
                return new Funnel(apex, edgeVertex1, edgeVertex0);
            }
        }

        public Record record { get { return mRecord; } }
        public List<int> edgesLeftVertexIndex { get { return mEdgesLeftVertexIndex; } }
        public List<int> pathEdges { get { return mPathEdges; } }

        List<Vector3> mFunnelPath = new List<Vector3>();
        //找到的navigation edge里哪个顶点是left vertex
        List<int> mEdgesLeftVertexIndex = new List<int>();
        //debug
        List<int> mPathEdges = new List<int>();
        //debug
        Record mRecord = new Record();
    }
}
