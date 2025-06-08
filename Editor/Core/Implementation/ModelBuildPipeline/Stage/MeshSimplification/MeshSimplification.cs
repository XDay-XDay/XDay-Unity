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
using BrainFailProductions.PolyFewRuntime;
using UnityEditor;
using UnityEngine;

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 模型减面,必须在CreateMesh后使用
    /// </summary>
    [Serializable]
    [StageDescription("模型减面", "模型减面")]
    internal class MeshSimplification : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(MeshSimplificationStageSetting);

        public MeshSimplification(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<MeshSimplificationStageSetting>(rootFolder);

            var objectMeshPairs = PolyfewRuntime.GetObjectMeshPairs(root, true);
            int triangle = PolyfewRuntime.CountTriangles(true, root);

            PolyfewRuntime.SimplificationOptions options = new PolyfewRuntime.SimplificationOptions();

            options.simplificationStrength = Mathf.Clamp01(setting.SimplificationStrength) * 100.0f;
            options.enableSmartlinking = true;
            options.preserveBorderEdges = false;
            options.preserveUVSeamEdges = false;
            options.preserveUVFoldoverEdges = false;
            options.recalculateNormals = false;
            options.regardCurvature = false;

            //if (preserveFace.isOn)
            //{
            //    options.regardPreservationSpheres = true;
            //    options.preservationSpheres.Add(new PolyfewRuntime.PreservationSphere(preservationSphere.position, preservationSphere.lossyScale.x, preservationStrength.value));
            //}
            //else { options.regardPreservationSpheres = false; }

            int after = PolyfewRuntime.SimplifyObjectDeep(objectMeshPairs,
                options,
                (GameObject go, PolyfewRuntime.MeshRendererPair mInfo) =>
                {
                    Debug.Log("Simplified mesh  " + mInfo.mesh.name + " on GameObject  " + go.name);
                });

            foreach (var pair in objectMeshPairs)
            {
                var originalMesh = pair.Value.mesh;
                var gameObject = pair.Key;
                Mesh newMesh;
                if (pair.Value.attachedToMeshFilter)
                {
                    newMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                }
                else
                {
                    newMesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                }
                var path = AssetDatabase.GetAssetPath(originalMesh);
                if (string.IsNullOrEmpty(path))
                {
                    Fail($"Mesh{originalMesh.name}不是文件");
                    return false;
                }
                SaveMesh(newMesh, path);
            }

            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
        }

        private void SaveMesh(Mesh newMesh, string path)
        {
            AssetDatabase.CreateAsset(newMesh, path);
            //让mesh不是readable
            SerializedObject serializedMesh = new SerializedObject(newMesh);
            SerializedProperty isReadableProperty = serializedMesh.FindProperty("m_IsReadable");
            if (isReadableProperty != null)
            {
                bool isReadable = isReadableProperty.boolValue;
                if (isReadable)
                {
                    isReadableProperty.boolValue = false;
                    serializedMesh.ApplyModifiedProperties();
                    EditorUtility.SetDirty(newMesh);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
