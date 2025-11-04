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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystem
    {
        bool IWorldBakeable.EnableBake => true;

        List<GameObject> IWorldBakeable.GetObjectsInRangeAtHeight(Vector3 minPos, Vector3 maxPos, float cameraHeight)
        {
            List<GameObject> gameObjects = new();
            int lod = m_PluginLODSystem.QueryLOD(cameraHeight);

            var minCoord = UnrotatedPositionToCoordinate(minPos.x, minPos.z);
            var maxCoord = UnrotatedPositionToCoordinate(maxPos.x, maxPos.z);

            for (int i = minCoord.y; i <= maxCoord.y; ++i)
            {
                for (int j = minCoord.x; j <= maxCoord.x; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null)
                    {
                        var prefabPath = tile.ResourceDescriptor.GetPath(lod);
                        var pos = CoordinateToUnrotatedPosition(j, i);
                        GameObject tileObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                        tileObject.transform.position = pos;
                        tileObject.name = prefabPath;
                        gameObjects.Add(tileObject);
                    }
                }
            }

            return gameObjects;
        }

        void IWorldBakeable.DestroyGameObjects(List<GameObject> gameObjects)
        {
            foreach (var obj in gameObjects)
            {
                Helper.DestroyUnityObject(obj);
            }
        }
    }
}
