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

using System;
using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    public partial class NavMap
    {
        public bool FindPath(Vector3 start, Vector3 end, List<Vector3> path)
        {
            path.Clear();

            NavArea startArea = m_Areas[0];// FindNavArea(start);
            NavArea endArea = m_Areas[m_Areas.Count - 1];//FindNavArea(end);
            if (startArea == endArea)
            {
                path.Add(start);
                path.Add(end);
                return true;
            }

            m_Start = start;
            m_End = end;

            FindRouteBorders();

            if (m_RouteBorders.Count == 0)
            {
                return false;
            }

            FindVertices(path);

            return true;
        }

        /// <summary>
        /// 核心思想:
        ///     1.漏斗变大就不更新漏洞形状
        ///     2.漏斗变小,更新漏斗
        ///     3.左边超过右边,将漏斗的右顶点加入路径点,并且从右顶点开始生成新的漏斗
        ///       右边超过左边,将漏斗的左顶点加入路径点,并且从左顶点开始生成新的漏斗
        ///     4.当所有Border访问完时,判断End-Apex的向量和左右边的关系,如果在更左边,将漏斗左顶点加入路径点,如果在更右边,将漏斗右顶点加入路径点.最后将End加入路径点
        /// </summary>
        /// <param name="path"></param>
        private void FindVertices(List<Vector3> path)
        {
            path.Add(m_Start);

            InitFunnel(m_Start, 0);

            for (var i = 1; i < m_RouteBorders.Count;)
            {
                GetBorderLeftRight(m_RouteBorders[i], out Vector3 leftVertex, out Vector3 rightVertex);

                //check left
                if (m_LeftVertex != leftVertex)
                {
                    var leftBorder = leftVertex - m_Apex;
                    BorderRelation leftRelation = GetLeftBorderRelation(leftBorder);
                    if (leftRelation == BorderRelation.Shrink)
                    {
                        m_LeftVertex = leftVertex;
                        m_CurrentLeftBorder = leftBorder;
                        m_LeftVertexBorderIndex = i;
                    }
                    else if (leftRelation == BorderRelation.Expand)
                    {
                        //扩大,什么也不做
                    }
                    else
                    {
                        //左边超过右边,将右顶点添加到路径,并且从右顶点生成新的funnel
                        path.Add(m_RightVertex);
                        InitFunnel(m_RightVertex, m_RightVertexBorderIndex);
                        i = m_RightVertexBorderIndex;
                        continue;
                    }
                }
                else
                {
                    m_LeftVertexBorderIndex = i;
                }

                //check right
                if (m_RightVertex != rightVertex)
                {
                    var rightBorder = rightVertex - m_Apex;
                    BorderRelation rightRelation = GetRightBorderRelation(rightBorder);
                    if (rightRelation == BorderRelation.Shrink)
                    {
                        m_RightVertex = rightVertex;
                        m_CurrentRightBorder = rightBorder;
                        m_RightVertexBorderIndex = i;
                    }
                    else if (rightRelation == BorderRelation.Expand)
                    {
                        //扩大,什么也不做
                    }
                    else
                    {
                        //右边超过左边,将左顶点添加到路径,并且从左顶点生成新的funnel
                        path.Add(m_LeftVertex);
                        InitFunnel(m_LeftVertex, m_LeftVertexBorderIndex);
                        i = m_LeftVertexBorderIndex;
                        continue;
                    }
                }
                else
                {
                    m_RightVertexBorderIndex = i;
                }

                ++i;
            }

            var side = GetSide(m_End - m_Apex, m_CurrentLeftBorder, m_CurrentRightBorder);
            if (side == -1)
            {
                //left
                path.Add(m_LeftVertex);
            }
            else if (side == 1)
            {
                //right
                path.Add(m_RightVertex);
            }
            path.Add(m_End);
        }

        private int GetSide(Vector3 dir, Vector3 leftBorder, Vector3 rightBorder)
        {
            var sl = Helper.GetSide(dir, leftBorder);
            var sr = Helper.GetSide(dir, rightBorder);
            if (sl == sr)
            {
                if (sl < 0)
                {
                    return -1;
                }
                return 1;
            }
            return 0;
        }

        private BorderRelation GetLeftBorderRelation(Vector3 dir)
        {
            var sl = Helper.GetSide(dir, m_CurrentLeftBorder);
            if (sl < 0)
            {
                return BorderRelation.Expand;
            }
            else
            {
                var sr = Helper.GetSide(dir, m_CurrentRightBorder);
                if (sr > 0)
                {
                    return BorderRelation.Pass;
                }
                return BorderRelation.Shrink;
            }
        }

        private BorderRelation GetRightBorderRelation(Vector3 dir)
        {
            var sr = Helper.GetSide(dir, m_CurrentRightBorder);
            if (sr > 0)
            {
                return BorderRelation.Expand;
            }
            else
            {
                var sl = Helper.GetSide(dir, m_CurrentLeftBorder);
                if (sl < 0)
                {
                    return BorderRelation.Pass;
                }
                return BorderRelation.Shrink;
            }
        }

        private void GetBorderLeftRight(NavBorder border, out Vector3 leftVertex, out Vector3 rightVertex)
        {
            leftVertex = Vector3.zero; rightVertex = Vector3.zero;
            throw new NotImplementedException();
        }

        private void InitFunnel(Vector3 apex, int borderIndex)
        {
            m_Apex = apex;
            GetBorderLeftRight(m_RouteBorders[borderIndex], out m_LeftVertex, out m_RightVertex);
            m_CurrentLeftBorder = m_LeftVertex - m_Apex;
            m_CurrentRightBorder = m_RightVertex - m_Apex;
            m_LeftVertexBorderIndex = borderIndex;
            m_RightVertexBorderIndex = borderIndex;
        }

        private void FindRouteBorders()
        {
            m_RouteBorders.Clear();
            m_RouteBorders.AddRange(m_Borders);
        }

        private NavArea FindNavArea(Vector3 start)
        {
            throw new NotImplementedException();
        }

        private Vector3 m_CurrentLeftBorder;
        private Vector3 m_CurrentRightBorder;
        private Vector3 m_LeftVertex;
        private Vector3 m_RightVertex;
        //左顶点所在的border序号
        private int m_LeftVertexBorderIndex;
        //右顶点所在的border序号
        private int m_RightVertexBorderIndex;
        private Vector3 m_Apex;
        private Vector3 m_Start;
        private Vector3 m_End;
        private readonly List<NavBorder> m_RouteBorders = new();
        private enum BorderRelation
        {
            //外扩
            Expand,
            //内收
            Shrink,
            //左边超过右边或右边超过左边
            Pass,
        }
    }
}
