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
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using XDay.RenderingAPI;

namespace XDay.GUIAPI.Editor
{
    /// <summary>
    /// 将UI prefab转换成3D prefab
    /// </summary>
    public static partial class UITo3D
    {
        public static void Convert(GameObject uiRoot, UITo3DConvertSetting setting)
        {
            var path = GetFolderPath(AssetDatabase.GetAssetPath(uiRoot));
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }

            m_Clone = Object.Instantiate(uiRoot);
            m_Clone.name = uiRoot.name;

            m_Setting = setting;

            BeforeConvert(m_Clone);

            ConvertComponents(m_Clone.transform);

            AfterConvert(m_Clone);

            DestroyUIComponents(m_Clone);

            PrefabUtility.SaveAsPrefabAsset(m_Clone.gameObject, $"{path}/{uiRoot.name}_Sprite.prefab");
            AssetDatabase.Refresh();

            Object.DestroyImmediate(m_Clone);
            m_Clone = null;
        }

        private static void BeforeConvert(GameObject clone)
        {
            m_Z = 0;
            var transforms = clone.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                m_GameObjectWorldPositions.Add(t.gameObject, t.position);
            }
        }

        private static void AfterConvert(GameObject clone)
        {
            var transforms = clone.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                t.position = m_GameObjectWorldPositions[t.gameObject] * 0.01f;
            }
            m_GameObjectWorldPositions.Clear();

            foreach (var obj in m_ConvertedGameObjects)
            {
                var pos = obj.transform.position;
                pos.z += m_Z;
                obj.transform.position = pos;
                m_Z += 0.02f;
            }
            m_ConvertedGameObjects.Clear();

            var animations = clone.GetComponentsInChildren<Animation>(true);
            foreach (var anim in animations)
            {
                anim.enabled = m_Setting.EnableAnimation;
            }

            var animators = clone.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                anim.enabled = m_Setting.EnableAnimation;
            }

            m_Setting = null;
        }

        private static void DestroyUIComponents(GameObject clone)
        {
            var buttons = clone.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                Object.DestroyImmediate(button);
            }
        }

        private static void ConvertComponents(Transform clone)
        {
            clone.gameObject.layer = 0;
            var childCount = clone.childCount;
            for (var i = childCount - 1; i >= 0; i--)
            {
                ConvertComponents(clone.GetChild(i));
            }

            clone.gameObject.layer = LayerMask.NameToLayer(m_Setting.GameObjectLayerName);

            SpriteRenderer spriteRenderer = null;
            var image = clone.GetComponent<UIImage>();
            Vector2 spriteScale = Vector2.one;
            if (image != null)
            {
                spriteRenderer = CreateSpriteRenderer(image, out spriteScale);
            }

            TextMeshPro textMesh = null;
            var text = clone.GetComponent<UIText>();
            if (text != null)
            {
                textMesh = CreateTextMesh(text);
            }

            Object.DestroyImmediate(clone.GetComponent<CanvasRenderer>());

            Transform transform;
            if (textMesh == null)
            {
                transform = clone.gameObject.AddComponent<Transform>();
            }
            else
            {
                transform = clone.gameObject.GetComponent<Transform>();
            }
            
            if (textMesh != null)
            {
                var rectTransform = textMesh.transform as RectTransform;
                rectTransform.anchoredPosition = Vector3.zero;
                rectTransform.sizeDelta = rectTransform.sizeDelta * 0.01f;
            }

            if (spriteRenderer != null)
            {
                var scale = transform.localScale;
                transform.localScale = new Vector3(scale.x * spriteScale.x, scale.y * spriteScale.y, 1);
            }
        }

        private static void AddConverted(GameObject gameObject)
        {
            if (!m_ConvertedGameObjects.Contains(gameObject))
            {
                m_ConvertedGameObjects.Add(gameObject);
            }
        }

        private static TextMeshPro CreateTextMesh(UIText textComp)
        {
            AddConverted(textComp.gameObject);

            var gameObject = textComp.gameObject;
            var text = textComp.text;
            var font = textComp.font;
            var color = textComp.color;
            var raycastTarget = textComp.raycastTarget;
            var alignment = textComp.alignment;

            Object.DestroyImmediate(textComp);

            var textMesh = gameObject.AddComponent<TextMeshPro3D>();
            gameObject.AddComponent<MeshRenderer>();
            textMesh.text = text;
            SetAlignment(textMesh, alignment);
            textMesh.fontSize = 2.3f;
            textMesh.font = GetFont(font);
            textMesh.raycastTarget = raycastTarget;
            textMesh.color = color;
            textMesh.UnderlayColor = textMesh.fontSharedMaterial.GetColor(ShaderUtilities.ID_UnderlayColor);
            textMesh.UnderlayDilate = textMesh.fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlayDilate);
            textMesh.UnderlaySoftness = textMesh.fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlaySoftness);

            var setting = m_Setting.GetTextMeshSetting(GetHierarchyPath(textMesh.transform, m_Clone.transform));
            if (setting != null)
            {
                if (!string.IsNullOrEmpty(setting.SortOrder.SortingLayerName))
                {
                    var layer = SortingLayer.NameToID(setting.SortOrder.SortingLayerName);
                    textMesh.sortingLayerID = layer;
                }
                textMesh.sortingOrder = setting.SortOrder.OrderInLayer;
                textMesh.characterSpacing = setting.CharacterSpacing;
                textMesh.fontStyle = setting.IsBold ? FontStyles.Bold : FontStyles.Normal;
            }

            return textMesh;
        }

        private static void SetAlignment(TextMeshPro textMesh, TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Top;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    break;
                case TextAnchor.UpperCenter:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Top;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
                    break;
                case TextAnchor.UpperRight:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Top;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Right;
                    break;
                case TextAnchor.LowerLeft:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    break;
                case TextAnchor.LowerCenter:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
                    break;
                case TextAnchor.LowerRight:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Bottom;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Right;
                    break;
                case TextAnchor.MiddleLeft:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Left;
                    break;
                case TextAnchor.MiddleCenter:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
                    break;
                case TextAnchor.MiddleRight:
                    textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
                    textMesh.horizontalAlignment = HorizontalAlignmentOptions.Right;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private static TMP_FontAsset GetFont(Font font)
        {
            var fontPath = AssetDatabase.GetAssetPath(font);
            var sdfFontPath = $"{RemoveExtension(fontPath)} SDF.asset";
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfFontPath);
            if (fontAsset == null)
            {
                var colorFontPath = $"{RemoveExtension(fontPath)} Color.asset";
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(colorFontPath);
                if (fontAsset == null)
                {
                    Debug.LogError($"font asset not found {fontPath}");
                }
            }
            return fontAsset;
        }

        private static SpriteRenderer CreateSpriteRenderer(UIImage image, out Vector2 spriteScale)
        {
            AddConverted(image.gameObject);

            var spriteRenderer = image.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = image.sprite;
            spriteRenderer.color = image.color;
            spriteRenderer.drawMode = GetDrawMode(image.type, out var isFill);
            spriteRenderer.sharedMaterial = image.material;

            //计算屏幕缩放
            float screenScale = 1.0f;
            if (m_Setting.OrthoSize > 0)
            {
                screenScale = (m_Setting.OrthoSize * 2f) / (m_Setting.ReferenceResolution.y * 0.01f);
            }

            //计算sprite和rectTransform的缩放比例
            var rectTransform = image.gameObject.GetComponent<RectTransform>();
            var scaleX = rectTransform.rect.width / image.sprite.rect.width;
            var scaleY = rectTransform.rect.height / image.sprite.rect.height;
            spriteScale = new Vector2(scaleX, scaleY) * screenScale;

            if (isFill)
            {
                if (image.fillMethod == Image.FillMethod.Horizontal)
                {
                    spriteRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(m_HorizontalFillMaterialPath);
                }
                else if (image.fillMethod == Image.FillMethod.Radial360)
                {
                    spriteRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(m_CirculeFillMaterialPath);
                }
                else
                {
                    Debug.Assert(false, $"Todo: {image.fillMethod}");
                }
                //设置SpriteRenderer数据
                var renderer = spriteRenderer.gameObject.AddComponent<SpriteRendererEx>();
                renderer.FillMethod = image.fillMethod;
            }
            else
            {
                if (spriteRenderer.sharedMaterial == null || spriteRenderer.sharedMaterial.shader == Shader.Find("UI/Default"))
                {
                    spriteRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(m_DefaultMaterialPath);
                }
            }

            if (image.raycastTarget)
            {
                spriteRenderer.gameObject.AddComponent<UnityEngine.BoxCollider>();
            }

            Object.DestroyImmediate(image);

            var setting = m_Setting.GetSpriteRendererSetting(GetHierarchyPath(spriteRenderer.transform, m_Clone.transform));
            if (setting != null)
            {
                if (!string.IsNullOrEmpty(setting.SortOrder.SortingLayerName))
                {
                    spriteRenderer.sortingLayerName = setting.SortOrder.SortingLayerName;
                }
                spriteRenderer.sortingOrder = setting.SortOrder.OrderInLayer;
            }
            return spriteRenderer;
        }

        private static string GetHierarchyPath(Transform transform, Transform root)
        {
            string path = "";
            while (true)
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = transform.name;
                }
                else
                {
                    path = transform.name + "/" + path;
                }
                transform = transform.parent;
                if (transform == null)
                {
                    break;
                }

                if (root != null && transform == root.transform)
                {
                    path = transform.name + "/" + path;
                    break;
                }
            }
            return path;
        }

        private static SpriteDrawMode GetDrawMode(Image.Type type, out bool isFill)
        {
            isFill = false;
            if (type == Image.Type.Tiled) 
            {
                return SpriteDrawMode.Tiled;
            }

            if (type == Image.Type.Sliced)
            {
                return SpriteDrawMode.Sliced;
            }

            if (type == Image.Type.Simple)
            {
                return SpriteDrawMode.Simple;
            }

            isFill = true;
            return SpriteDrawMode.Simple;
        }

        private static string RemoveExtension(string path)
        {
            var idx = path.LastIndexOf('.');
            if (idx == -1)
            {
                return path;
            }
            return path[..idx];
        }

        private static string GetFolderPath(string path)
        {
            int idx = path.LastIndexOf("/", System.StringComparison.Ordinal);
            if (idx != -1)
            {
                path = path.Substring(0, idx);
            }

            return path;
        }

        private const string m_CirculeFillMaterialPath = "Assets/Game/Res/Shader/SpriteCircularFill.mat";
        private const string m_HorizontalFillMaterialPath = "Assets/Game/Res/Shader/SpriteHorizontalFill.mat";
        private const string m_DefaultMaterialPath = "Assets/Game/Res/Shader/SpriteDefault.mat";
        private static float m_Z;
        private static Dictionary<GameObject, Vector3> m_GameObjectWorldPositions = new();
        private static List<GameObject> m_ConvertedGameObjects = new();
        private static UITo3DConvertSetting m_Setting;
        private static GameObject m_Clone;
    }
}
