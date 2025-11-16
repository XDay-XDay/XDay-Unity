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
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Decoration.Editor
{
    public partial class DecorationSystem
    {
        bool IWorldBakeable.EnableBake => false;

        List<GameObject> IWorldBakeable.GetObjectsInRangeAtHeight(Vector3 minPos, Vector3 maxPos, float cameraHeight)
        {
            List<GameObject> gameObjects = new();
            int lod = m_PluginLODSystem.QueryLOD(cameraHeight);

            var bakeBounds = new Bounds();
            bakeBounds.SetMinMax(minPos, maxPos);
            var bakeRect = bakeBounds.ToRect();

            foreach (var kv in m_Decorations)
            {
                var gameObject = m_Renderer.GetGameObject(kv.Key);
                if (gameObject != null)
                {
                    var objectBounds = kv.Value.QueryWorldBounds();
                    if (objectBounds.Overlaps(bakeRect))
                    {
                        if (kv.Value.ExistsInLOD(lod))
                        {
                            gameObjects.Add(Object.Instantiate(gameObject));
                        }
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
