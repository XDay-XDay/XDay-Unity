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
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    internal class ChangeTextureSize
    {
        [MenuItem("XDay/Other/修改贴图高度")]
        static void Process()
        {
            var items = new List<ParameterWindow.Parameter> 
            {
                new ParameterWindow.ObjectParameter("图片", "", null, typeof(Texture2D), allowSceneObject: false),
                new ParameterWindow.IntParameter("修改的高度", "", 100),
                new ParameterWindow.BoolParameter("大小限制到2的N次方", "", false),
            };

            ParameterWindow.Open("修改图片高度", items, (parameters) =>
            {
                var ok = ParameterWindow.GetObject<Texture2D>(parameters[0], out var texture);
                ok &= ParameterWindow.GetInt(parameters[1], out var changeSize);
                ok &= ParameterWindow.GetBool(parameters[2], out var potSize);
                if (ok)
                {
                    Generate(texture, changeSize, potSize);
                    return false;
                }
                else
                {
                    EditorUtility.DisplayDialog("出错了", "出错了,修改图片大小失败", "确定");
                }
                return false;
            });
        }

        public static void Generate(Texture2D texture, int extraHeight, bool potSize)
        {
            var texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.Assert(false, "can't generate texture!");
                return;
            }

            var oldWidth = texture.width;
            var oldHeight = texture.height;
            var newHeight = oldHeight + extraHeight;
            if (newHeight <= 0)
            {
                Debug.Assert(false, "texture height is <= 0!");
                return;
            }

            var copy = EditorHelper.CreateReadableTexture(texture);
            var pixels = copy.GetPixels();
            
            var biggerTexture = new Texture2D(oldWidth, newHeight);

            var newPixels = new Color[oldWidth * newHeight];
            var length = Mathf.Min(pixels.Length, newPixels.Length);
            System.Array.Copy(pixels, newPixels, length);
            biggerTexture.SetPixels(newPixels);
            biggerTexture.Apply();
            var bytes = biggerTexture.EncodeToTGA();
            System.IO.File.WriteAllBytes(texturePath, bytes);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (potSize)
            {
                importer.npotScale = TextureImporterNPOTScale.ToNearest;
            }
            else
            {
                importer.npotScale = TextureImporterNPOTScale.None;
            }
            importer.isReadable = false;
            importer.SaveAndReimport();

            Object.DestroyImmediate(copy);
            Object.DestroyImmediate(biggerTexture);
        }
    }
}
