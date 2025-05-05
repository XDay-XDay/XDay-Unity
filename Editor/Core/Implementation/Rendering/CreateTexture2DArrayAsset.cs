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

namespace XDay.RenderingAPI
{
    public static class CreateTexture2DArrayAsset
    {
        [MenuItem("XDay/Rendering/Create Texture2DArray Asset")]
        private static void CreateAsset()
        {
            string folder = "Assets";
            List<Texture2D> textures = new();
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D tex)
                {
                    var texPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(texPath))
                    {
                        folder = Helper.GetFolderPath(texPath);
                    }
                    textures.Add(tex);
                }
            }
            Create(textures, $"{folder}/texture2darray.asset");
        }

        public static void Create(List<Texture2D> textures, string outputFilePath)
        {
            bool ok = Validate(textures);
            if (!ok)
            {
                EditorUtility.DisplayDialog("Error", "Invalid texture", "OK");
                return;
            }

            var handle = new Texture2DArray(
                textures[0].width, textures[0].height, textures.Count,
                TextureFormat.RGBA32,
                true
            );

            for (var i = 0; i < textures.Count; i++)
            {
                var tex = textures[i];
                if (tex.isReadable)
                {
                    handle.SetPixels(tex.GetPixels(), i);
                }
                else
                {
                    var readableTex = EditorHelper.CreateReadableTexture(tex);
                    handle.SetPixels(readableTex.GetPixels(), i);
                    Object.DestroyImmediate(readableTex);
                }
            }
            handle.Apply();

            AssetDatabase.CreateAsset(handle, outputFilePath);
            AssetDatabase.Refresh();
        }

        private static bool Validate(List<Texture2D> textures)
        {
            if (textures == null || 
                textures.Count == 0)
            {
                return false;
            }

            var width = textures[0].width;
            var height = textures[0].height;
            foreach (var tex in textures)
            {
                if (tex == null)
                {
                    return false;
                }
                if (tex.width != width || tex.height != height)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
