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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    [Serializable]
    public abstract class ModelBuildPipelineStageSetting : ScriptableObject
    {
        public virtual void OnCreate() { }
        public virtual void CopyFrom(ModelBuildPipelineStageSetting setting) { }
    }

    public enum StageStatus
    {
        Pending,
        Running,
        Success,
        Fail,
    }

    [Serializable]
    public abstract class ModelBuildPipelineStage
    {
        public List<int> PreviousStageIDs => m_PreviousStageIDs;
        public int ID => m_ID;
        public string Name { get => m_Name; set => m_Name = value; }
        public string Comment { get => m_Comment; set => m_Comment = value; }
        public abstract Type SettingType { get; }
        internal StageStatus Status { get => m_Status; set => m_Status = value; }

        public ModelBuildPipelineStage(int id)
        {
            m_ID = id;
            m_Name = GetType().Name;
        }

        public void Build(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            Status = StageStatus.Running;
            bool success = OnBuild(model, root, rootFolder, pipeline);
            if (success)
            {
                Status = StageStatus.Success;
            }
            else
            {
                Status = StageStatus.Fail;
            }
        }

        /// <summary>
        /// prefab文件已经生成后调用
        /// </summary>
        public void PostBuild(GameObject prefab, ModelBuildPipeline pipeline)
        {
            if (Status == StageStatus.Success)
            {
                bool success = OnPostBuild(prefab, pipeline);
                if (!success)
                {
                    Status = StageStatus.Fail;
                }
            }
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

        /// <summary>
        /// 将root的设置同步到setting文件
        /// </summary>
        /// <param name="root"></param>
        public abstract void SyncSetting(GameObject root, string rootFolder);

        protected T GetStageSetting<T>(string rootFolder) where T : ModelBuildPipelineStageSetting
        {
            var settingFolder = $"{rootFolder}/{ModelBuildPipeline.SETTING_FOLDER_NAME}";
            var settingFilePath = EditorHelper.QueryAssetFilePath<T>(new string[] { settingFolder });
            var setting = AssetDatabase.LoadAssetAtPath<T>(settingFilePath);
            if (setting == null)
            {
                Debug.LogError($"没有找到配置{typeof(T)}");
            }
            else
            {
                EditorUtility.SetDirty(setting);
            }
            return setting;
        }

        protected void Fail(string errorMsg)
        {
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Debug.LogError(errorMsg);
            }
            Status = StageStatus.Fail;
        }

        protected abstract bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline);
        protected virtual bool OnPostBuild(GameObject prefab, ModelBuildPipeline pipeline) { return true; }

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
    }
}
