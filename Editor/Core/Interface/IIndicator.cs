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

namespace XDay.WorldAPI.Editor
{
    public interface IGizmoCubeIndicator
    {
        static IGizmoCubeIndicator Create()
        {
            return new GizmoCubeIndicator();
        }

        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        float Size { set; get; }
        bool Visible { get; set; }

        void Draw(Color color, bool centerAlignment);
    }

    public interface IMeshIndicator
    {
        static IMeshIndicator Create(IWorld world)
        {
            return new MeshIndicator(world);
        }

        string Prefab { set; }
        bool Visible { get; set; }
        Vector3 Position { get; set; }
        Vector3 Scale { get; set; }
        Quaternion Rotation { get; set; }

        void OnDestroy();
    }

    public interface IQuadMeshIndicator
    {
        static IQuadMeshIndicator Create()
        {
            return new QuadMeshIndicator();
        }

        bool Visible { get; set; }
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        float Scale { set; }

        void OnDestroy();
    }
}

//XDay