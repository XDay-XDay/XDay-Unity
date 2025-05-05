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
    /// 给mesh增加normal
    /// </summary>
    internal class AddNormalToMesh
    {
        [MenuItem("XDay/Other/Add Normal To Mesh")]
        static void AddNormal()
        {
            var mesh = Selection.activeObject as Mesh;
            if (!mesh)
            {
                Debug.LogError("Selection is not a mesh");
                return;
            }

            var adder = new AddNormalToMesh();
            adder.Generate(mesh);
        }

        private void Generate(Mesh mesh)
        {
            var meshPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(meshPath))
            {
                Debug.LogError("no mesh path");
                return;
            }

            var newMesh = Object.Instantiate(mesh);
            newMesh.RecalculateNormals();
            newMesh.UploadMeshData(!mesh.isReadable);
            AssetDatabase.CreateAsset(newMesh, meshPath);
            AssetDatabase.Refresh();
        }
    }
}
