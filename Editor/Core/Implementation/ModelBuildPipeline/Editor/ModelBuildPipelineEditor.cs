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
using XDay.UtilityAPI.Editor;

namespace XDay.ModelBuildPipeline.Editor
{
    internal class ModelBuildPipelineEditor : EditorWindow
    {
        [MenuItem("XDay/Model/Build Pipeline")]
        public static ModelBuildPipelineEditor Open()
        {
            return GetWindow<ModelBuildPipelineEditor>("Model Build Pipeline");
        }

        public ModelBuildPipeline GetActivePipeline()
        {
            return m_PipelineView == null ? null : m_PipelineView.Pipeline;
        }

        private void OnDisable()
        {
            m_PipelineView?.OnDestroy();
        }

        private void OnGUI()
        {
            if (m_PipelineView == null)
            {
                m_PipelineView = new();
                m_PipelineView.Init(null, position.width, position.height);
            }

            m_PipelineView.Render(position.width, position.height, Repaint);
            Repaint();
        }

        private ModelBuildPipelineView m_PipelineView;
    }
}
