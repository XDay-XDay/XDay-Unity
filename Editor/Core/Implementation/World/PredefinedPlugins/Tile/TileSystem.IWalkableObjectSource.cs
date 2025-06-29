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
using XDay.NavigationAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystem
    {
        //实现IWalkableObjectSource接口
        List<MeshSource> IWalkableObjectSource.GetAllWalkableMeshes()
        {
            List<MeshSource> ret = new();
            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    m_Renderer.GetTerrainTileMeshAndGameObject(j, i, out var mesh, out var gameObject);
                    if (mesh != null && gameObject != null)
                    {
                        var navMeshSetting = gameObject.GetComponent<NavMeshSetting>();
                        ret.Add(new MeshSource() { AreaID = navMeshSetting == null ? 0 : navMeshSetting.AreaID, GameObject = gameObject, Mesh = mesh });
                    }
                }
            }
            return ret;
        }

        List<ColliderSource> IWalkableObjectSource.GetAllWalkableColliders()
        {
            return new();
        }

    }
}
