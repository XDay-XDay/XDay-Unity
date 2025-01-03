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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Math;

namespace XDay.RenderingAPI.BRG.Editor
{
    [CustomEditor(typeof(GPUBatchManagerDebugger))]
    internal class GPUBatchManagerDebuggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBatchesList();
        }

        private void OnSceneGUI()
        {
            if (m_FocusPosition != null)
            {
                SceneView.currentDrawingSceneView.pivot = m_FocusPosition.GetValueOrDefault();
                m_FocusPosition = null;
            }
        }

        private void DrawBatchesList()
        {
            var batchManager = (target as GPUBatchManagerDebugger).BatchManager;
            if (batchManager != null)
            {
                var instanceCount = 0;
                for (var i = 0; i < batchManager.BatchCount; ++i)
                {
                    instanceCount += batchManager.GetBatch(i).InstanceCount;
                }

                EditorGUILayout.IntField("Instance Count", instanceCount);

                Sort(batchManager);

                for (var i = 0; i < m_SortedBatches.Count; ++i)
                {
                    DrawBatchInfo(m_SortedBatches[i], i);
                }
            }
        }

        private void DrawBatchInfo(GPUBatch batch, int index)
        {
            EditorHelper.IndentLayout(() =>
            {
                EditorGUILayout.ObjectField("Mesh", batch.Mesh, typeof(Mesh), false);
                EditorGUILayout.ObjectField("Material", batch.Material, typeof(Material), false);
                EditorGUILayout.IntField("ID", batch.ID);

                DrawInstanceList(batch, index);
            });
        }

        private void DrawInstanceList(GPUBatch batch, int index)
        {
            var instanceCount = batch.InstanceCount;
            EditorGUILayout.IntField($"Batch {index} Instance Count", instanceCount);
            EditorGUILayout.LabelField("Instance List");
            EditorHelper.IndentLayout(() =>
            {
                for (var i = 0; i < instanceCount; ++i)
                {
                    bool removed = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.IntField($"Instance {i}", batch.UsedInstances[i]);

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Locate", GUILayout.MaxWidth(60)))
                    {
                        PackedMatrix[] object2World = new PackedMatrix[1];
                        batch.GetData(object2World, batch.UsedInstances[i], propertyIndex: 0);
                        var pos = object2World[0].Translation;
                        m_FocusPosition = pos;
                    }

                    if (GUILayout.Button("Create", GUILayout.MaxWidth(60)))
                    {
                        PackedMatrix[] object2World = new PackedMatrix[1];
                        batch.GetData(object2World, batch.UsedInstances[i], propertyIndex: 0);
                        var pos = object2World[0].Translation;
                        var rot = object2World[0].Rotation;
                        var scale = object2World[0].Scale;
                        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.SetPositionAndRotation(pos, rot);
                        cube.transform.localScale = scale;
                    }

                    if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                    {
                        batch.RemoveInstance(batch.UsedInstances[i]);
                        removed = true;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (removed)
                    {
                        break;
                    }
                }
            });
        }

        private void Sort(IGPUBatchManager batchManager)
        {
            m_SortedBatches.Clear();

            for (var i = 0; i < batchManager.BatchCount; i++)
            {
                m_SortedBatches.Add(batchManager.GetBatch(i));
            }

            m_SortedBatches.Sort((a, b) =>
            {
                return
                (a.Mesh.GetInstanceID() + a.Material.GetInstanceID()) -
                (b.Mesh.GetInstanceID() + b.Material.GetInstanceID());
            });
        }

        private List<GPUBatch> m_SortedBatches = new();
        private Vector3? m_FocusPosition;
    }
}


//XDay