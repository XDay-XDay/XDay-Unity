/*
 * Copyright (c) 2024 XDay
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

using XDay.SerializationAPI;
using UnityEngine;

namespace XDay.WorldAPI
{
    public interface IWorldLODSetup
    {
        string Name { get; set; }
        float Altitude { get; set; }
        Vector2 WorldDataRange { get; set; }
    }

    public interface IWorldLODSystem : ISerializable
    {
        int LODCount { get; set; }

        IWorldLODSetup GetLOD(int index);
        IWorldLODSetup QueryLOD(string name);
        bool CheckIfLODIsSorted();
        void Sort();
        float GetLODAltitude(int lod);
    }

    public interface IPluginLODSetup : ISerializable
    {
        string Name { get; set; }
        float Altitude { get; set; }
        float Tolerance { get; set; }
    }

    public interface IPluginLODSystem : ISerializable
    {
        static IPluginLODSystem Create(int lodCount)
        {
            return new PluginLODSystem(lodCount);
        }

        void Init(IWorldLODSystem lodSystem);

        int LODCount { get; set; }
        int PreviousLOD { get; }
        int CurrentLOD { get; }
        IWorldLODSystem WorldLODSystem { get; }
        bool Update(float altitude);

        void ChangeLODName(string oldName, string newName);
        IPluginLODSetup QueryLOD(string name);
        IPluginLODSetup GetLOD(int index);
    }
}
