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
using UnityEngine;
using XDay.NavigationAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Decoration.Editor
{
    internal partial class DecorationSystem
    {
        List<MeshSource> IWalkableObjectSource.GetAllWalkableMeshes()
        {
            List<MeshSource> sources = new();
            foreach (var kv in m_Decorations)
            {
                var decoration = kv.Value;
                var gameObject = m_Renderer.GetGameObject(kv.Key);
                //根节点挂了NavMeshSetting的物体才生成NavMesh
                if (gameObject.TryGetComponent<NavMeshSetting>(out var navMeshSetting))
                {
                    var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
                    foreach (var filter in meshFilters)
                    {
                        var source = new MeshSource()
                        {
                            AreaID = navMeshSetting.AreaID,
                            GameObject = filter.gameObject,
                            Mesh = filter.sharedMesh,
                        };
                        sources.Add(source);
                    }
                }
            }
            return sources;
        }

        List<ColliderSource> IWalkableObjectSource.GetAllWalkableColliders()
        {
            List<ColliderSource> colliders = new();
            foreach (var kv in m_Decorations)
            {
                var decoration = kv.Value;
                var gameObject = m_Renderer.GetGameObject(kv.Key);
                //根节点挂了NavMeshSetting的物体才生成NavMesh
                if (gameObject.TryGetComponent<NavMeshSetting>(out var navMeshSetting))
                {
                    foreach (var collider in gameObject.GetComponentsInChildren<UnityEngine.Collider>(true))
                    {
                        colliders.Add(new ColliderSource() { AreaID = navMeshSetting.AreaID, Collider = collider });
                    }
                }
            }
            return colliders;
        }
    }
}
