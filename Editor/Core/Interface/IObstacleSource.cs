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

namespace XDay.WorldAPI.Editor
{
    [System.Flags]
    public enum ObstacleAttribute
    {
        None = 0,
        Walkable = 1,
    }

    public interface IObstacle
    {
        int AreaID => 0;
        //for 3d navmesh
        float Height => 0;
        bool Walkable => false;
        ObstacleAttribute Attribute => ObstacleAttribute.None;
        List<Vector3> WorldPolygon { get; }
        Rect WorldBounds { get; }
    }

    public interface IObstacleSource
    {
        List<IObstacle> GetObstacles();
    }

    public interface IWalkableObjectSource
    {
        List<MeshSource> GetAllWalkableMeshes();
        List<ColliderSource> GetAllWalkableColliders();
    }

    public class MeshSource
    {
        public int AreaID = 0;
        public Mesh Mesh;
        public GameObject GameObject;
    }

    public class ColliderSource
    {
        public int AreaID = 0;
        public UnityEngine.Collider Collider;
    }
}
