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
using System.IO;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.ModelBuildPipeline.Editor
{
    /// <summary>
    /// 渲染模型头像
    /// </summary>
    [Serializable]
    [StageDescription("渲染头像", "渲染模型头像图标")]
    internal class CreateModelIcon : ModelBuildPipelineStage
    {
        public override Type SettingType => typeof(CreateModelIconStageSetting);

        public CreateModelIcon(int id) : base(id)
        {
        }

        protected override bool OnBuild(GameObject model, GameObject root, string rootFolder, ModelBuildPipeline pipeline)
        {
            var setting = GetStageSetting<CreateModelIconStageSetting>(rootFolder);

            CreateCamera(setting);

            SetModel(root, $"{rootFolder}/{ModelBuildPipeline.TEXTURE_FOLDER_NAME}", setting);

            DestroyCamera();

            Helper.DestroyUnityObject(m_Model);

            return true;
        }

        public override void SyncSetting(GameObject root, string rootFolder)
        {
        }

        private void CreateCamera(CreateModelIconStageSetting setting)
        {
            var obj = new GameObject("Actor Camera");
            m_ActorCamera = obj.AddComponent<Camera>();
            m_ActorCamera.nearClipPlane = 0.01f;
            m_ActorRenderTexture = new RenderTexture(setting.IconSize, setting.IconSize, 24);
            m_ActorCamera.targetTexture = m_ActorRenderTexture;
            m_ActorCamera.clearFlags = CameraClearFlags.SolidColor;
            m_ActorCamera.backgroundColor = new Color(0, 0, 0, 0);
            m_ActorCamera.cullingMask = 1 << LayerMask.NameToLayer(setting.ObjectLayerName);
        }

        private void DestroyCamera()
        {
            Helper.DestroyUnityObject(m_ActorCamera.gameObject);
            Helper.DestroyUnityObject(m_ActorRenderTexture);
            m_ActorRenderTexture = null;
            m_ActorCamera = null;
        }

        private void SetModel(GameObject prefab, string outputFolder, CreateModelIconStageSetting setting)
        {
            m_Model = UnityEngine.Object.Instantiate(prefab);
            m_Model.name = prefab.name;
            m_Model.transform.forward = Vector3.forward;

            var bounds = Helper.QueryBounds(m_Model);

            var cameraShotNode = prefab.transform.Find(setting.CameraShotNodeName);
            if (cameraShotNode == null)
            {
                m_ActorCamera.gameObject.transform.position = setting.CameraShotOffset;
                m_ActorCamera.transform.forward = setting.CameraShotForward;
            }
            else
            {
                m_ActorCamera.gameObject.transform.position = cameraShotNode.position;
                m_ActorCamera.transform.forward = cameraShotNode.forward;
            }
            Helper.Traverse(m_Model.transform, false, (transform) =>
            {
                transform.gameObject.layer = LayerMask.NameToLayer(setting.ObjectLayerName);
            });
            m_ActorCamera.Render();

            RenderTexture.active = m_ActorRenderTexture;
            var copy = new Texture2D(m_ActorRenderTexture.width, m_ActorRenderTexture.height)
            {
                name = $"{m_ActorRenderTexture.name}"
            };
            copy.ReadPixels(new Rect(0, 0, m_ActorRenderTexture.width, m_ActorRenderTexture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = null;
            var bytes = copy.EncodeToPNG();

            var path = $"{outputFolder}/{prefab.name}_icon.png";
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            if (setting.OutputAsSprite)
            {
                EditorHelper.ImportTextureAsSprite(path);
            }
        }

        private Transform FindTransform(Transform transform, string name)
        {
            if (transform.name.IndexOf(name) >= 0)
            {
                return transform;
            }

            var n = transform.childCount;
            for (var i = 0; i < n; i++)
            {
                var t = FindTransform(transform.GetChild(i), name);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        private Camera m_ActorCamera;
        private RenderTexture m_ActorRenderTexture;
        private GameObject m_Model;
    }
}
