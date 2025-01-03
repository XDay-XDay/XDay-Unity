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

using System.Text;
using UnityEngine;

namespace XDay.AnimationAPI.Editor
{
    internal partial class GPUAnimationBaker
    {
        private bool Check(GameObject prefab)
        {
            StringBuilder msg = new();
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0)
            {
                msg.AppendLine($"SkinnedMeshRenderer not found at prefab {prefab.name}");
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                {
                    msg.AppendLine($"Mesh not found at renderer {renderer.name}");
                }
            }

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null)
                {
                    msg.AppendLine($"Material not found at renderer {renderer.name}");
                }
            }

            var str = msg.ToString();
            if (string.IsNullOrEmpty(str))
            {
                return true;
            }
            Debug.LogError(str);
            return false;
        }
    }
}

//XDay