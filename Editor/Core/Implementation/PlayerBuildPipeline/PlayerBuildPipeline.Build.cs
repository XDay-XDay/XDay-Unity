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

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace XDay.PlayerBuildPipeline.Editor
{
    public partial class PlayerBuildPipeline
    {
        public async UniTask Build(Action refresh)
        {
            SortStages();

            foreach (var stage in m_SortedStages)
            {
                var report = await stage.PreBuild(this);

                refresh?.Invoke();

                if (!report.Success)
                {
                    return;
                }
            }

            foreach (var stage in m_SortedStages)
            {
                var report = await stage.Build(this);

                refresh?.Invoke();

                if (!report.Success)
                {
                    return;
                }
            }

            AssetDatabase.Refresh();

            foreach (var stage in m_SortedStages)
            {
                var report = await stage.PostBuild(this);

                refresh?.Invoke();

                if (!report.Success)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 按照依赖关系排序
        /// </summary>
        private void SortStages()
        {
            TopologySort<int> depGraph = new();
            foreach (var stage in m_Stages)
            {
                foreach (var prevStageID in stage.PreviousStageIDs)
                {
                    depGraph.AddEdge(prevStageID, stage.ID);
                }
            }

            List<int> sortedStageIDs = new();
            depGraph.Sort(sortedStageIDs);

            m_SortedStages.Clear();
            foreach (var stage in m_Stages)
            {
                if (stage.Enable)
                {
                    m_SortedStages.Add(stage);
                }
            }

            m_SortedStages.Sort((a, b) =>
            {
                var ai = sortedStageIDs.IndexOf(a.ID);
                var bi = sortedStageIDs.IndexOf(b.ID);
                return ai.CompareTo(bi);
            });
        }
    }
}
