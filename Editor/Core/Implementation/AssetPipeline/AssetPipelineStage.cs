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
using UnityEngine;

namespace XDay.AssetPipeline.Editor
{
    public enum StageStatus
    {
        Pending,
        Running,
        Success,
        Fail,
    }

    public class PreBuildReport
    {
        public bool Success = true;
        public string ErrorMessage = "";
        public double TimeCost = 0;
    }

    public class BuildReport
    {
        public bool Success = true;
        public string ErrorMessage = "";
        public double TimeCost = 0;
    }

    public class PostBuildReport
    {
        public bool Success = true;
        public string ErrorMessage = "";
        public double TimeCost = 0;
    }

    [Serializable]
    public abstract class AssetPipelineStage
    {
        public List<int> PreviousStageIDs => m_PreviousStageIDs;
        public int ID => m_ID;
        public string Name { get => m_Name; set => m_Name = value; }
        public string Comment { get => m_Comment; set => m_Comment = value; }
        internal StageStatus Status { get => m_Status; set => m_Status = value; }

        public AssetPipelineStage(int id)
        {
            m_ID = id;
            m_Name = GetType().Name;
        }

        public async UniTask<PreBuildReport> PreBuild(AssetPipeline pipeline)
        {
            Status = StageStatus.Running;
            var report = await OnPreBuild(pipeline);
            if (report.Success)
            {
                Status = StageStatus.Success;
            }
            else
            {
                Status = StageStatus.Fail;
            }
            if (!string.IsNullOrEmpty(report.ErrorMessage))
            {
                Debug.LogError(report.ErrorMessage);
            }
            Debug.Log($"Stage {m_Name} step \"PreBuild\" cost {report.TimeCost} seconds");
            return report;
        }

        public async UniTask<BuildReport> Build(AssetPipeline pipeline)
        {
            Status = StageStatus.Running;
            var report = await OnBuild(pipeline);
            if (report.Success)
            {
                Status = StageStatus.Success;
            }
            else
            {
                Status = StageStatus.Fail;
            }
            if (!string.IsNullOrEmpty(report.ErrorMessage))
            {
                Debug.LogError(report.ErrorMessage);
            }
            Debug.Log($"Stage {m_Name} step \"Build\" cost {report.TimeCost} seconds");
            return report;
        }

        public async UniTask<PostBuildReport> PostBuild(AssetPipeline pipeline)
        {
            Status = StageStatus.Running;
            var report = await OnPostBuild(pipeline);
            if (report.Success)
            {
                Status = StageStatus.Success;
            }
            else
            {
                Status = StageStatus.Fail;
            }
            if (!string.IsNullOrEmpty(report.ErrorMessage))
            {
                Debug.LogError(report.ErrorMessage);
            }
            Debug.Log($"Stage {m_Name} step \"PostBuild\" cost {report.TimeCost} seconds");
            return report;
        }

        public void AddPreviousStage(int stageID)
        {
            if (!m_PreviousStageIDs.Contains(stageID))
            {
                m_PreviousStageIDs.Add(stageID);
            }
        }

        public void RemovePreviousStage(int stageID)
        {
            m_PreviousStageIDs.Remove(stageID);
        }

        protected void Fail(string errorMsg)
        {
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogError(errorMsg);
            }
            Status = StageStatus.Fail;
        }

        protected virtual async UniTask<PreBuildReport> OnPreBuild(AssetPipeline pipeline)
        {
            return await UniTask.FromResult(new PreBuildReport());
        }
        protected abstract UniTask<BuildReport> OnBuild(AssetPipeline pipeline);
        protected virtual async UniTask<PostBuildReport> OnPostBuild(AssetPipeline pipeline)
        {
            return await UniTask.FromResult(new PostBuildReport());
        }

        public abstract void Draw();

        [SerializeField]
        private List<int> m_PreviousStageIDs = new();
        [SerializeField]
        private int m_ID;
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private string m_Comment;
        private StageStatus m_Status = StageStatus.Pending;
        [SerializeField]
        public Vector2 WorldPosition;
        [SerializeField]
        public Vector2 RealPosition;
        [SerializeField]
        public bool ShowComment = false;
        [SerializeField]
        public bool Enable = true;
    }
}
