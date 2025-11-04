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

namespace XDay.UtilityAPI.Editor
{
    /// <summary>
    /// 将物体渲染到RenderTexture上
    /// </summary>
    public class BakeToRenderTexture
    {
        public RenderTexture TargetTexture { get { return m_TargetTexture; } }
        public int TextureSize { set { m_TextureSize = value; } get { return m_TextureSize; } }

        public BakeToRenderTexture(string name, Camera camera, bool enableDepthTexture, GameObject parent = null)
        {
            m_Root = new GameObject(name);
            if (parent != null)
            {
                m_Root.transform.parent = parent.transform;
            }
            Helper.HideGameObject(m_Root);

            m_DestroyCamera = false;
            if (camera == null)
            {
                var obj = new GameObject("Camera");
                obj.transform.parent = m_Root.transform;
                camera = obj.AddComponent<Camera>();
                m_DestroyCamera = true;
            }

            m_Camera = camera;
            m_Camera.clearFlags = CameraClearFlags.Color;
            m_Camera.cullingMask = 1 << m_Layer;
            m_Camera.nearClipPlane = 0.1f;
            m_Camera.farClipPlane = 100000f;
            m_Camera.depth = 1;
            m_Camera.orthographic = true;
            m_Camera.backgroundColor = new Color(0, 0, 0, 0);
            m_Camera.enabled = false;
            m_Camera.depthTextureMode = enableDepthTexture ? DepthTextureMode.Depth : DepthTextureMode.None;
        }

        public void OnDestroy(bool isEditor)
        {
            if (m_TargetTexture)
            {
                if (isEditor)
                {
                    Object.DestroyImmediate(m_TargetTexture);
                }
                else
                {
                    Object.Destroy(m_TargetTexture);
                }
                m_TargetTexture = null;
            }

            m_Camera.depthTextureMode = DepthTextureMode.None;

            if (m_DestroyCamera)
            {
                if (m_Camera != null)
                {
                    Helper.DestroyUnityObject(m_Camera.gameObject);
                }
            }
            m_Camera = null;
            m_Light = null;

            Helper.DestroyUnityObject(m_Root);
        }

        //overrideBounds:强制使用overrideBounds作为视野的范围
        public void Render(GameObject obj, bool renderFront, float cameraExtraDistance, RenderTexture customRenderTexture, bool hasLight, Bounds overrideBounds)
        {
            if (hasLight)
            {
                CreateLight();
                m_Light.enabled = true;
            }

            m_Camera.transform.gameObject.SetActive(true);
            m_Camera.enabled = true;

            if (overrideBounds.extents == Vector3.zero)
            {
                overrideBounds = Helper.QueryBounds(obj, false);
            }
            var objectSize = overrideBounds.size;

            float cameraDistance;
            float objectWidth;
            float objectHeight;
            if (renderFront)
            {
                objectWidth = objectSize.x;
                objectHeight = objectSize.y;
                cameraDistance = cameraExtraDistance;
            }
            else
            {
                objectWidth = objectSize.x;
                objectHeight = objectSize.z;
                cameraDistance = cameraExtraDistance;
            }

            RenderTexture texture = customRenderTexture;

            if (customRenderTexture == null)
            {
                SetRenderTextureSize(TextureSize, TextureSize);
                texture = m_TargetTexture;
            }

            texture.wrapMode = TextureWrapMode.Clamp;

            SetCameraPosition(overrideBounds.center, objectWidth, objectHeight, cameraDistance, renderFront);

            //set object layer to render layer
            var layerChanger = new LayerChanger();
            layerChanger.Start(obj, m_Layer);

            Vector3 offset = new Vector3(-10000, 0, 0);
            var originalPos = obj.transform.position;
            obj.transform.position = obj.transform.position + offset;
            m_Camera.transform.position = m_Camera.transform.position + offset;

            m_Camera.targetTexture = texture;
            m_Camera.Render();

            obj.transform.position = originalPos;

            layerChanger.Stop(obj);

            m_Camera.enabled = false;
            m_Camera.transform.gameObject.SetActive(false);
            if (hasLight)
            {
                m_Light.enabled = false;
            }
        }

        public void SaveTexture(RenderTexture renderTexture, string path, bool hasAlpha)
        {
            var texture = ToTexture2D(renderTexture, hasAlpha);
            byte[] bytes = texture.EncodeToTGA();
            System.IO.File.WriteAllBytes(path, bytes);
            Helper.DestroyUnityObject(texture);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = hasAlpha;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        public Texture2D ToTexture2D(RenderTexture renderTexture, bool hasAlpha)
        {
            TextureFormat format = TextureFormat.RGB24;
            if (hasAlpha)
            {
                format = TextureFormat.ARGB32;
            }
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, format, false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        private void CreateLight()
        {
            if (m_Light == null)
            {
                var lightObj = new GameObject("Light");
                m_Light = lightObj.AddComponent<UnityEngine.Light>();
                lightObj.transform.parent = m_Root.transform;
                m_Light.enabled = false;
                m_Light.cullingMask = 1 << m_Layer;
                m_Light.type = LightType.Directional;
                m_Light.color = new Color(233 / 255.0f, 212 / 255.0f, 166 / 255.0f, 1.0f);
                lightObj.transform.position = new Vector3(424.6f, 10.6f, 63.9f);
                lightObj.transform.rotation = Quaternion.Euler(66.703f, -56.319f, 16.689f);
            }
        }

        private void SetRenderTextureSize(int width, int height)
        {
            while (true)
            {
                if (m_TargetTexture == null)
                {
                    //添加图片生成大小数值检测处理
                    if (width <= 0)
                        width = 256;
                    if (height <= 0)
                        height = 256;

                    m_TargetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                }

                if (m_TargetTexture.width != width || m_TargetTexture.height != height)
                {
                    GameObject.Destroy(m_TargetTexture);
                    m_TargetTexture = null;
                }

                if (m_TargetTexture)
                {
                    break;
                }
            }
        }

        private void SetCameraPosition(Vector3 objCenter, float objectWidth, float objectHeight, float cameraDistance, bool renderFront)
        {
            m_Camera.orthographicSize = objectHeight * 0.5f;
            m_Camera.aspect = objectWidth / (float)objectHeight;

            if (renderFront)
            {
                m_Camera.transform.position = objCenter + Vector3.forward * cameraDistance;
                m_Camera.transform.LookAt(objCenter, Vector3.up);
            }
            else
            {
                m_Camera.transform.position = objCenter + Vector3.up * cameraDistance;
                m_Camera.transform.LookAt(objCenter, Vector3.forward);
            }
        }

        private GameObject m_Root;
        private Camera m_Camera;
        private Light m_Light;
        private int m_TextureSize = 1024;
        private RenderTexture m_TargetTexture;
        private bool m_DestroyCamera = false;
        private readonly int m_Layer = LayerMask.NameToLayer("RenderToTexture");
    }
}