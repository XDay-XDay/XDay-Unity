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

using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.Build.Editor
{
    internal partial class PlayerBuildPipelineView
    {
        private void Save()
        {
            if (m_Pipeline == null)
            {
                return;
            }

            m_Pipeline.ViewPosition = m_Viewer.GetWorldPosition();
            m_Pipeline.Zoom = m_Viewer.GetZoom();

            var filePath = AssetDatabase.GetAssetPath(m_Pipeline);
            if (string.IsNullOrEmpty(filePath))
            {
                string path = EditorUtility.SaveFilePanel("保存", "Assets/", "Player Build Pipeline", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(m_Pipeline, Helper.ToUnityPath(path));
                }
            }
            else
            {
                EditorUtility.SetDirty(m_Pipeline);
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
        }

        private void CreateNew(string name)
        {
            Reset();

            var pipeline = ScriptableObject.CreateInstance<PlayerBuildPipeline>();
            pipeline.name = name;
            SetPipeline(pipeline);
        }
    }
}
