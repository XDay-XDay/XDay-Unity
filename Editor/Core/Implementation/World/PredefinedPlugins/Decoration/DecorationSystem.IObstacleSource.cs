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
using XDay.UtilityAPI.Editor;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Decoration.Editor
{
    public partial class DecorationSystem
    {
        public List<IObstacle> GetObstacles()
        {
            List<IObstacle> obstacles = new();
            var polygonBuilder = new BoundingPolygonBuilder();
            foreach (var kv in m_Decorations)
            {
                var decoration = kv.Value;
                var descriptor = decoration.ResourceDescriptor;
                if (descriptor != null)
                {
                    var gameObject = m_Renderer.QueryGameObject(decoration.ID);
                    if (gameObject.CompareTag("Obstacle"))
                    {
                        var polygon = polygonBuilder.Build(gameObject);
                        if (polygon.Count > 0)
                        {
                            obstacles.Add(new Obstacle(polygon, gameObject.name));
                        }
                    }
                }
            }
            return obstacles;
        }

        private class Obstacle : IObstacle
        {
            public List<Vector3> WorldPolygon => m_WorldPolygon;
            public Rect WorldBounds => m_WorldBounds;
            public int AreaID => 0;
            public float Height => 0;
            public bool Walkable => false;
            public ObstacleAttribute Attribute => ObstacleAttribute.None;
            public bool IsValid => true;

            public Obstacle(List<Vector3> polygon, string name)
            {
                m_WorldPolygon = polygon;
                m_WorldBounds = Helper.CalculateBounds(polygon).ToRect();
#if false
                //debug
                var obj = new GameObject(name);
                var dp = obj.AddComponent<DrawPolygon>();
                dp.Init(polygon);
#endif
            }

            private List<Vector3> m_WorldPolygon;
            private Rect m_WorldBounds;
        }
    }
}
