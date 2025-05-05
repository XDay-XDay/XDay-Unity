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
using System;

namespace XDay.WorldAPI.Editor
{
    internal class PluginLODSystemEditorWindow : EditorWindow
    {
        public void Open(
            string selectedLODName,
            IPluginLODSystem pluginLODSystem,
            IWorldLODSystem worldLODSystem, 
            Action<string, string> onLODNameChanged)
        {
            var lodCount = worldLODSystem.LODCount;

            m_SelectedLODName = selectedLODName;
            m_PluginLODSystem = pluginLODSystem;
            m_OnLODNameChanged = onLODNameChanged;

            m_Names = new string[lodCount];
            m_Labels = new string[lodCount];

            for (var i = 0; i < lodCount; i++)
            {
                var lod = worldLODSystem.GetLOD(i);

                if (selectedLODName == lod.Name)
                {
                    m_ActiveLOD = i;
                }

                m_Labels[i] = $"{lod.Name}: @{lod.Altitude}m";
                m_Names[i] = lod.Name;
            }

            Show();
        }

        private void OnGUI()
        {
            m_ActiveLOD = EditorGUILayout.Popup("LOD", m_ActiveLOD, m_Labels);

            if (GUILayout.Button("确定"))
            {
                if (m_PluginLODSystem.QueryLOD(m_Names[m_ActiveLOD]) != null)
                {
                    EditorUtility.DisplayDialog("出错了", $"{m_Names[m_ActiveLOD]}已经存在!", "确定");
                }
                else
                {
                    m_OnLODNameChanged(m_SelectedLODName, m_Names[m_ActiveLOD]);
                    Close();
                }
            }
        }

        private IPluginLODSystem m_PluginLODSystem;
        private int m_ActiveLOD;
        private string m_SelectedLODName;
        private string[] m_Names;
        private string[] m_Labels;
        private Action<string, string> m_OnLODNameChanged;
    }
}

//XDay