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

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 使用独立的Mesh文件来替换fbx中的Mesh
    /// </summary>
    [Serializable]
    [StageDescription("使用独立Mesh", "使用独立的Mesh文件")]
    internal class CreateMesh : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateMeshStageSetting);

        public CreateMesh(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<CreateMeshStageSetting>(rootFolder);

            m_OriginalMeshToNewMesh = new();

            CreateMeshes(model, $"{rootFolder}/{ModelBuildPipeline.MESH_FOLDER_NAME}", setting);

            ReplaceMesh(root);

            m_OriginalMeshToNewMesh = null;

            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
        }

        private void CreateMeshes(GameObject model, string meshFolder, CreateMeshStageSetting setting)
        {
            var meshFilters = model.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in meshFilters)
            {
                var newMesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                newMesh.name = filter.sharedMesh.name;
                m_OriginalMeshToNewMesh[filter.sharedMesh] = newMesh;
                var path = $"{meshFolder}/{newMesh.name}.asset";
                AssetDatabase.CreateAsset(newMesh, path);
                ReimportMesh(newMesh, setting);
            }

            var skinnedMeshRenderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in skinnedMeshRenderers)
            {
                var newMesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                newMesh.name = renderer.sharedMesh.name;
                m_OriginalMeshToNewMesh[renderer.sharedMesh] = newMesh;

                var path = $"{meshFolder}/{newMesh.name}.asset";
                AssetDatabase.CreateAsset(newMesh, path);

                ReimportMesh(newMesh, setting);
            }
        }

        private void ReplaceMesh(GameObject prefab)
        {
            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in meshFilters)
            {
                var newMesh = m_OriginalMeshToNewMesh[filter.sharedMesh];
                filter.sharedMesh = newMesh;
            }

            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in skinnedMeshRenderers)
            {
                var newMesh = m_OriginalMeshToNewMesh[renderer.sharedMesh];
                renderer.sharedMesh = newMesh;
            }
        }

        private void ReimportMesh(Mesh mesh, CreateMeshStageSetting setting)
        {
            SerializedObject serializedMesh = new SerializedObject(mesh);
            SerializedProperty isReadableProperty = serializedMesh.FindProperty("m_IsReadable");

            if (isReadableProperty != null)
            {
                isReadableProperty.boolValue = setting.Readable; 
                serializedMesh.ApplyModifiedProperties();
                EditorUtility.SetDirty(mesh);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError("找不到 m_IsReadable 属性");
            }
        }

        private Dictionary<Mesh, Mesh> m_OriginalMeshToNewMesh = new();
    }
}
