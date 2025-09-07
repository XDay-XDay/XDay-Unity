﻿/*
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

namespace XDay.AssetPipeline.Editor
{
    [CreateAssetMenu(menuName = "XDay/Build/Asset Pipeline")]
    public partial class AssetPipeline : ScriptableObject
    {
        public int NextID => ++m_NextID;
        public int StageCount => m_Stages.Count;
        public List<AssetPipelineStage> Stages => m_Stages;
        public event Action<AssetPipelineStage> EventCreateStage
        {
            add
            {
                m_EventCreateStage -= value;
                m_EventCreateStage += value;
            }
            remove
            {
                m_EventCreateStage -= value;
            }
        }
        public event Action<AssetPipelineStage> EventDestroyStage
        {
            add
            {
                m_EventDestroyStage -= value;
                m_EventDestroyStage += value;
            }
            remove
            {
                m_EventDestroyStage -= value;
            }
        }

        public bool ContainsStage(Type type)
        {
            foreach (var stage in m_Stages)
            {
                if (stage.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

        public AssetPipelineStage CreateStage(Type type)
        {
            if (GetStage(type) != null)
            {
                Debug.LogError($"Stage{type.Name}已经存在");
                return null;
            }

            Debug.Assert(type.IsSubclassOf(typeof(AssetPipelineStage)));

            var id = NextID;
            object[] args = { id };
            var node = Activator.CreateInstance(type, args) as AssetPipelineStage;
            AddStage(node);
            return node;
        }

        public T GetStage<T>() where T : AssetPipelineStage
        {
            return GetStage(typeof(T)) as T;
        }

        public AssetPipelineStage GetStage(Type type)
        {
            foreach (var stage in m_Stages)
            {
                if (stage.GetType() == type)
                {
                    return stage;
                }
            }
            return null;
        }

        public void AddStage(AssetPipelineStage stage)
        {
            if (stage == null)
            {
                Debug.LogError("stage无效!");
                return;
            }

            if (GetStage(stage.GetType()) != null)
            {
                Debug.LogError("stage已经创建!");
                return;
            }

            m_Stages.Add(stage);
            m_EventCreateStage?.Invoke(stage);
        }

        public void RemoveStage(int index)
        {
            if (index >= 0 && index < m_Stages.Count)
            {
                var stage = m_Stages[index];

                foreach (var otherStage in m_Stages)
                {
                    if (otherStage != stage)
                    {
                        otherStage.RemovePreviousStage(stage.ID);
                    }
                }

                m_Stages.RemoveAt(index);
                m_EventDestroyStage?.Invoke(stage);
            }
        }

        public void RemoveStage(AssetPipelineStage stage)
        {
            RemoveStage(m_Stages.IndexOf(stage));
        }

        public AssetPipelineStage GetStage(int index)
        {
            if (index >= 0 && index < m_Stages.Count)
            {
                return m_Stages[index];
            }
            return null;
        }

        public bool ContainsStage(int stageID)
        {
            foreach (var stage in m_Stages)
            {
                if (stage != null && stage.ID == stageID)
                {
                    return true;
                }
            }
            return false;
        }

        public int GetStageIndex(AssetPipelineStage stage)
        {
            return m_Stages.IndexOf(stage);
        }

        public string GetCommandLineArgValue(string argName)
        {
            if (!argName.StartsWith("-"))
            {
                argName = $"-{argName}";
            }
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == argName)
                {
                    if (i == args.Length - 1)
                    {
                        return "";
                    }
                    var next = args[i + 1];
                    if (next.StartsWith("-"))
                    {
                        return "";
                    }
                    return next;
                }
            }
            return "";
        }

        internal void RemoveInvalidStages()
        {
            for (var i = m_Stages.Count - 1; i >= 0; --i)
            {
                if (m_Stages[i] == null)
                {
                    m_Stages.RemoveAt(i);
                }
            }

            foreach (var stage in m_Stages)
            {
                if (stage != null)
                {
                    for (var i = stage.PreviousStageIDs.Count - 1; i >= 0; --i)
                    {
                        if (!ContainsStage(stage.PreviousStageIDs[i]))
                        {
                            stage.RemovePreviousStage(stage.PreviousStageIDs[i]);
                        }
                    }
                }
            }
        }

        internal void Draw()
        {
            foreach (var stage in m_Stages)
            {
                stage.Draw();
            }
        }

        [SerializeReference]
        private List<AssetPipelineStage> m_Stages = new();
        private List<AssetPipelineStage> m_SortedStages = new();
        [SerializeField]
        private int m_NextID;
        //小的排在更前面
        [SerializeField]
        private int m_SortOrder = 0;
        private event Action<AssetPipelineStage> m_EventCreateStage;
        private event Action<AssetPipelineStage> m_EventDestroyStage;
        //编辑器显示
        public float Zoom = 1;
        public Vector2 ViewPosition;
    }
}