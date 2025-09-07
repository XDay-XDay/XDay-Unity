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

namespace XDay.WorldAPI
{
    /// <summary>
    /// lod setting
    /// </summary>
    public interface IWorldLODSetup
    {
        string Name { get; set; }
        float Altitude { get; set; }
        Vector2 WorldDataRange { get; set; }
    }

    /// <summary>
    /// world lod system
    /// </summary>
    [Preserve]
    public interface IWorldLODSystem : ISerializable
    {
        int LODCount { get; set; }

        IWorldLODSetup GetLOD(int index);
        IWorldLODSetup QueryLOD(string name);

        /// <summary>
        /// check if lod is sorted from low to high
        /// </summary>
        /// <returns></returns>
        bool CheckIfLODIsSorted();

        /// <summary>
        /// sort lod
        /// </summary>
        void Sort();

        /// <summary>
        /// get altitude of lod
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        float GetLODAltitude(int lod);
    }

    /// <summary>
    /// world plugin lod setting
    /// </summary>
    [Preserve]
    public interface IPluginLODSetup : ISerializable
    {
        string Name { get; set; }
        float Altitude { get; set; }
        float Tolerance { get; set; }
        int RenderLOD { get; set; }
    }

    /// <summary>
    /// world plugin lod system
    /// </summary>
    [Preserve]
    public interface IPluginLODSystem : ISerializable
    {
        static IPluginLODSystem Create(int lodCount)
        {
            return new PluginLODSystem(lodCount);
        }

        int LODCount { get; set; }
        int PreviousLOD { get; }
        int CurrentLOD { get; }
        IWorldLODSystem WorldLODSystem { get; }

        void Init(IWorldLODSystem lodSystem);
        bool Update(float altitude);
        void ChangeLODName(string oldName, string newName);
        IPluginLODSetup QueryLOD(string name);
        int QueryLOD(float altitude);
        IPluginLODSetup GetLOD(int index);
        void AddLOD(string name, float altitude, float tolerance = 0);
        int GetRenderLOD(int curLOD);
    }
}
