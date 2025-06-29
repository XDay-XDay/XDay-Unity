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
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Attribute
{
    [Preserve]
    public interface IAttributeSystem : IWorldPlugin
    {
        /// <summary>
        /// 世界坐标转换到某层的格子坐标
        /// </summary>
        /// <param name="layerIndex">哪一层,每一层格子个数和大小可以不同</param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        Vector2Int WorldPositionToCoordinate(int layerIndex, float x, float z);

        /// <summary>
        /// 格子是否是空地
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool IsEmptyGrid(int x, int y);

        /// <summary>
        /// 获取名称对应的层
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        int GetLayerIndex(string name);

        /// <summary>
        /// 获取某层的名称
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        string GetLayerName(int layerIndex);
    }
}