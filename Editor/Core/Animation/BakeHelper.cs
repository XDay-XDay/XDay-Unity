/*
 * Copyright (c) 2024 XDay
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

namespace XDay.AnimationAPI.Editor
{
    internal static class BakeHelper
    {
        public static bool IsChild(this GameObject root, GameObject child)
        {
            if (root == child)
            {
                return true;
            }

            foreach (Transform childTransform in root.transform)
            {
                if (childTransform.gameObject.IsChild(child))
                {
                    return true;
                }
            }
            return false;
        }

        public static GameObject QueryChild(this GameObject gameObject, string childName)
        {
            if (string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (childName == gameObject.name)
            {
                return gameObject;
            }

            foreach (Transform child in gameObject.transform)
            {
                var ret = child.gameObject.QueryChild(childName);
                if (ret != null)
                {
                    return ret;
                }
            }
            return null;
        }

        public static void AssignRenderQueue(Material target, Material source, int renderQueue)
        {
            target.renderQueue = renderQueue > 0 ? renderQueue : source.renderQueue;
        }

        public static void CopyRendererProperties(Renderer src, Renderer dst)
        {
            dst.reflectionProbeUsage = src.reflectionProbeUsage;
            dst.motionVectorGenerationMode = src.motionVectorGenerationMode;
            dst.lightProbeUsage = src.lightProbeUsage;
            dst.receiveShadows = src.receiveShadows;
            dst.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;
            dst.shadowCastingMode = src.shadowCastingMode;
        }

        public static void CopyMaterialProperties(Material target, Material source)
        {
            Dictionary<string, string> nameTranslation = new()
            {
                { "_MetallicParaMap", "_MetallicGlossMap"},
                { "_Normal", "_BumpMap"},
                { "_Albedo", "_BaseMap"},
            };

            var sourceShader = source.shader;
            var propertyCount = ShaderUtil.GetPropertyCount(sourceShader);
            for (var i = 0; i < propertyCount; i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(sourceShader, i);
                if (target.HasProperty(propertyName))
                {
                    var propertyType = ShaderUtil.GetPropertyType(sourceShader, i);
                    switch (propertyType)
                    {
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                            target.SetTexture(propertyName, source.GetTexture(propertyName));
                            break;
                        case ShaderUtil.ShaderPropertyType.Vector:
                            target.SetVector(propertyName, source.GetVector(propertyName));
                            break;
                        case ShaderUtil.ShaderPropertyType.Range:
                        case ShaderUtil.ShaderPropertyType.Float:
                            target.SetFloat(propertyName, source.GetFloat(propertyName));
                            break;
                            case ShaderUtil.ShaderPropertyType.Color:
                            target.SetColor(propertyName, source.GetColor(propertyName));
                            break;
                        default:
                            Debug.LogError($"todo: {propertyName}, type: {propertyType}");
                            break;
                    }
                }
            }

            foreach (var kv in nameTranslation)
            {
                var fromName = kv.Key;
                var toName = kv.Value;
                if (target.HasProperty(toName) &&
                    source.HasProperty(fromName))
                {
                    var sourceTexture = source.GetTexture(fromName);
                    if (target.GetTexture(toName) == null &&
                        sourceTexture != null)
                    {
                        target.SetTexture(toName, sourceTexture);
                    }
                }
            }
        }

        public static Texture2D CreateTexture(Color[] textureData, Vector2Int textureSize, string path, FilterMode filterMode, TextureFormat format = TextureFormat.RGBAHalf)
        {
            var texture = new Texture2D(textureSize.x, textureSize.y, format, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = filterMode;
            texture.SetPixels(textureData);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        public static GameObject QueryAnimationSampleGameObject(GameObject root)
        {
            var sampleObject = QueryTaggedSampleAnimationGameObject(root);
            if (sampleObject != null)
            {
                return sampleObject;
            }

            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                Debug.LogError($"No GameObject tagged as \"AnimationRoot\", will sample animation from animator game object {animator.gameObject}!");
                return animator.gameObject;
            }

            Debug.LogError($"Can't sample animation, no game object tagged as \"AnimationRoot\" or animator!");
            return null;
        }

        private static GameObject QueryTaggedSampleAnimationGameObject(GameObject gameObject)
        {
            if (gameObject.tag == AnimationDefine.ANIM_SAMPLE_NAME)
            {
                return gameObject;
            }

            foreach (Transform transform in gameObject.transform)
            {
                var obj = QueryTaggedSampleAnimationGameObject(transform.gameObject);
                if (obj != null)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}

//XDay