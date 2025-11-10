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

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Tile
{
    public enum TileMaterialUpdaterTiming 
    {
        Start,
        End,
        Running,
    }

    public interface ITextureLoader
    {
        void Load(string path, Action<Texture2D> onTextureLoaded);
        void Unload(Texture texture);
    }

    /// <summary>
    /// 更新tile的材质参数
    /// </summary>
    public interface ITileMaterialUpdater
    {
        /// <summary>
        /// 烘培tile出现时
        /// </summary>
        /// <param name="material"></param>
        void OnBakeLayerShowTile(Material material);

        /// <summary>
        /// lod0 tile出现时
        /// </summary>
        /// <param name="material"></param>
        void OnNormalLayerShowTile(Material material);

        /// <summary>
        /// 更新Normal Layer的材质参数
        /// </summary>
        /// <param name="material"></param>
        /// <param name="timing">更新时机</param>
        void OnUpdateNormalLayerMaterial(Material material, TileMaterialUpdaterTiming timing);

        /// <summary>
        /// 更新Bake Layer的材质参数
        /// </summary>
        /// <param name="material"></param>
        /// <param name="timing">更新时机</param>
        void OnUpdateBakedLayerMaterial(Material material, TileMaterialUpdaterTiming timing);
    }

    [Preserve]
    public interface ITileSystem : IWorldPlugin
    {
        float GetHeightAtPos(float x, float z);

        /// <summary>
        /// 设置地表材质更新器
        /// </summary>
        /// <param name="updater"></param>
        void SetTileMaterialUpdater(ITileMaterialUpdater updater);

        /// <summary>
        /// 触发格子范围内的地表材质更新器
        /// </summary>
        /// <param name="minX">世界坐标</param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        /// <param name="timing">更新时机</param>
        void UpdateMaterialInRange(float minX, float minZ, float maxX, float maxZ, TileMaterialUpdaterTiming timing);

        /// <summary>
        /// 使用动态Mask贴图加载
        /// </summary>
        /// <param name="loader"></param>
        void EnableDynamicMaskLoading(ITextureLoader loader);
    }
}
