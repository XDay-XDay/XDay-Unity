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

namespace XDay.AssetPipeline.Editor
{
    internal partial class AssetPipelineView : GraphNodeEditor
    {
        public AssetPipeline Pipeline => m_Pipeline;

        public AssetPipelineView() : base(100000, 100000)
        {
            m_SuccessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/SuccessState.png"));
            m_FailTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/FailState.png"));
            m_RunningTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/RunningState.png"));
        }

        public void Init(AssetPipeline pipeline, float windowWidth, float windowHeight)
        {
            ResetViewPosition();
            m_AlignToGrid = true;
            Reset();
            m_Viewer.SetWorldPosition(-windowWidth * 0.5f, -windowHeight * 0.5f);

            SetPipeline(pipeline);
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
            SetPipeline(null);
            Object.DestroyImmediate(m_ButtonBackground);
            m_ButtonBackground = null;
            foreach (var stage in m_AllStages)
            {
                stage.OnDestroy();
            }
            m_AllStages.Clear();
            m_SelectionInfo.Stages.Clear();
            m_SelectionInfo.Part = Part.None;
        }

        private void CreateStageView(AssetPipelineStage stage, Vector2 worldPosition)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorldHelper.GetIconPath($"behaviourtree/{stage.GetType().Name}.png"));
            var nodeView = new StageView(stage, texture, AlignToGrid)
            {
                WorldPosition = worldPosition
            };
            m_AllStages.Add(nodeView);
        }

        private void OnCreateStage(AssetPipelineStage stage)
        {
            var pos = Window2World(m_CreateWindowPosition);
            CreateStageView(stage, pos);
        }

        private void OnDestroyStage(AssetPipelineStage stage)
        {
            var nodeView = GetStageView(stage.ID);
            nodeView?.OnDestroy();
            m_AllStages.Remove(nodeView);
        }

        private StageView GetStageView(int stageID)
        {
            foreach (var nodeView in m_AllStages)
            {
                if (nodeView.Stage.ID == stageID)
                {
                    return nodeView;
                }
            }
            return null;
        }

        private void SetPipeline(AssetPipeline pipeline)
        {
            if (m_Pipeline != pipeline)
            {
                if (pipeline != null)
                {
                    Reset();
                }

                m_Pipeline = pipeline;
                if (m_Pipeline != null)
                {
                    m_Pipeline.EventCreateStage += OnCreateStage;
                    m_Pipeline.EventDestroyStage += OnDestroyStage;
                }

                if (m_Pipeline != null)
                {
                    //删除Stage为null的数据
                    m_Pipeline.RemoveInvalidStages();

                    foreach (var stage in m_Pipeline.Stages)
                    {
                        CreateStageView(stage, stage.WorldPosition);
                    }

                    m_Viewer.SetZoom(pipeline.Zoom);
                    m_Viewer.SetWorldPosition(pipeline.ViewPosition.x, pipeline.ViewPosition.y);
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

        protected override void OnZoomChanged()
        {
            for (var i = 0; i < m_AllStages.Count; i++)
            {
                m_AllStages[i].Move(Vector2.zero);
            }
        }

        private void Update()
        {
        }

        private AssetPipeline m_Pipeline;
        private readonly List<StageView> m_AllStages = new();
        private readonly SelectionInfo m_SelectionInfo = new();
        private readonly Texture2D m_SuccessTexture;
        private readonly Texture2D m_FailTexture;
        private readonly Texture2D m_RunningTexture;

        private class SelectionInfo
        {
            public List<StageView> Stages = new();
            public List<StageConnection> Connections = new();
            public Part Part = Part.None;
        };

        private class StageConnection
        {
            public StageView StageView;
            public StageView PrevStageView;
        }
    };

    internal enum Part
    {
        None,
        Top,
        Bottom,
        Center,
    }
}
