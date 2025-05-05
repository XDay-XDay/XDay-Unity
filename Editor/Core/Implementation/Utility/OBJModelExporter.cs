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
using System.Text;

namespace XDay.UtilityAPI.Editor
{
    public static class OBJModelExporter
    {
        public static void Create(string filePath, Vector3[] vertices, Vector2[] uvs, Color[] colors, int[] indices)
        {
            if (vertices != null && indices != null)
            {
                StringBuilder textBuilder = new();
                for (int i = 0; i < vertices.Length; ++i)
                {
                    if (colors != null && colors.Length > 0)
                    {
                        textBuilder.Append($"v {vertices[i].x} {vertices[i].y} {vertices[i].z} {colors[i].r} {colors[i].g} {colors[i].b}\n");
                    }
                    else
                    {
                        textBuilder.Append($"v {vertices[i].x} {vertices[i].y} {vertices[i].z}\n");
                    }
                }

                if (uvs != null)
                {
                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        textBuilder.Append($"vt {uvs[i].x} {uvs[i].y}\n");
                    }
                }

                if (uvs != null)
                {
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        int a = indices[i] + 1;
                        int b = indices[i + 1] + 1;
                        int c = indices[i + 2] + 1;
                        textBuilder.Append($"f {a}/{a} {b}/{b} {c}/{c}\n");
                    }
                }
                else
                {
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        textBuilder.Append($"f {indices[i] + 1} {indices[i+1] + 1} {indices[i+2] + 1}\n");
                    }
                }

                var str = textBuilder.ToString();
                EditorHelper.WriteFile(str, filePath);
            }
        }
    }
}
