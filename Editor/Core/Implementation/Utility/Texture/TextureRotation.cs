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
using XDay.WorldAPI;

namespace XDay.UtilityAPI.Editor
{
    public class TextureRotation
    {
        public void OnDestroy()
        {
            if (m_BakeCamera != null)
            {
                Helper.DestroyUnityObject(m_BakeCamera.gameObject);
            }
            Helper.DestroyUnityObject(m_Quad);
            Helper.DestroyUnityObject(m_RGBAMaterial);
            Helper.DestroyUnityObject(m_AlphaMaterial);
        }

        public Texture2D Rotate(float rotation, Texture2D texture, bool onlyAlpha)
        {
            if (m_Quad == null)
            {
                Init();
            }

            SetMaterial(onlyAlpha, texture);

            var sizeAfterRotation = CalculateRotatedSize(rotation, texture);

            SetQuad(rotation, sizeAfterRotation);

            SetCamera(sizeAfterRotation);

            m_BakeCamera.Render();
            m_BakeCamera.enabled = false;         
            m_Quad.SetActive(false);

            return ReadRenderTarget(texture.name);
        }

        private void Init()
        {
            m_RGBAMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(WorldHelper.GetShaderPath("Transparent.shader")));
            m_AlphaMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(WorldHelper.GetShaderPath("Brush.shader")));

            var gameObject = new GameObject("Texture Rotation Camera");
            gameObject.tag = "EditorOnly";
            m_BakeCamera = gameObject.AddComponent<Camera>();
            m_BakeCamera.clearFlags = CameraClearFlags.SolidColor;
            m_BakeCamera.transform.position = new Vector3(0, 10.0f, 0);
            //m_BakeCamera.cullingMask = LayerMask.GetMask(m_BakeObjectLayerName);
            m_BakeCamera.backgroundColor = new Color32(0, 0, 0, 0);
            m_BakeCamera.orthographic = true;
            m_BakeCamera.transform.LookAt(Vector3.zero, worldUp: new Vector3(0, 0, -1.0f));
            m_BakeCamera.enabled = false;
            Helper.HideGameObject(gameObject);

            m_Quad = GameObject.CreatePrimitive(PrimitiveType.Plane);
            m_Quad.tag = "EditorOnly";
            //m_Quad.layer = LayerMask.NameToLayer(m_BakeObjectLayerName);
            m_Quad.DestroyComponent<UnityEngine.Collider>();
            m_Quad.SetActive(false);
            Helper.HideGameObject(m_Quad);
        }

        private Texture2D ReadRenderTarget(string textureName)
        {
            var targetTexture = m_BakeCamera.targetTexture;
            var rotatedTextureWidth = targetTexture.width;
            var rotatedTextureHeight = targetTexture.height;
            var rotatedTexture = new Texture2D(rotatedTextureWidth, rotatedTextureHeight)
            {
                name = $"{textureName}_rotated"
            };
            RenderTexture.active = targetTexture;
            rotatedTexture.ReadPixels(new Rect(0, 0, rotatedTextureWidth, rotatedTextureHeight), 0, 0);
            rotatedTexture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(targetTexture);
            return rotatedTexture;
        }

        private void SetCamera(Vector2Int size)
        {
            m_BakeCamera.transform.position = m_BakeCamera.transform.position + m_BakePosition;
            m_BakeCamera.orthographicSize = size.y * 0.5f;
            m_BakeCamera.aspect = size.x / (float)size.y;
            m_BakeCamera.enabled = true;
            m_BakeCamera.targetTexture = RenderTexture.GetTemporary(size.x, size.y, 24, RenderTextureFormat.ARGB32);
        }
        
        private void SetQuad(float rotation, Vector2Int size)
        {
            m_Quad.SetActive(true);
            m_Quad.transform.SetPositionAndRotation(m_Quad.transform.position + m_BakePosition, Quaternion.Euler(0, rotation, 0));
            m_Quad.transform.localScale = new Vector3(size.x * 0.1f, 1, size.y * 0.1f);
        }

        private Vector2Int CalculateRotatedSize(float rotation, Texture2D texture)
        {
            var rect = new Rect(Vector2.zero, new Vector2(texture.width, texture.height));
            var rotatedRect = rect.Rotate(Quaternion.Euler(0, rotation, 0));

            var rotatedWidth = Mathf.FloorToInt(rotatedRect.width);
            var rotatedHeight = Mathf.FloorToInt(rotatedRect.height);
            if (rotatedWidth <= 0)
            {
                rotatedWidth = m_InvalidTextureSize;
            }
            if (rotatedHeight <= 0)
            {
                rotatedHeight = m_InvalidTextureSize;
            }

            return new Vector2Int(rotatedWidth, rotatedHeight);
        }

        private void SetMaterial(bool onlyAlpha, Texture2D texture)
        {
            Color color;
            var renderer = m_Quad.GetComponent<MeshRenderer>();
            if (onlyAlpha)
            {
                renderer.sharedMaterial = m_AlphaMaterial;
                color = new Color(0, 0, 0, 0);
            }
            else
            {
                renderer.sharedMaterial = m_RGBAMaterial;
                color = Color.white;
            }
            renderer.sharedMaterial.mainTexture = texture;
            renderer.sharedMaterial.SetColor("_Color", color);
        }

        private const int m_InvalidTextureSize = 16;
        //private const string m_BakeObjectLayerName = "Bake Texture";
        private Camera m_BakeCamera;
        private GameObject m_Quad;
        private Material m_AlphaMaterial;
        private Material m_RGBAMaterial;
        private Vector3 m_BakePosition = new(-25000, 0, 0);
    }
}

//XDay