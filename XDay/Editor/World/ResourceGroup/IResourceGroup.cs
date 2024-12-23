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
using System;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    public interface IResourceData : ISerializable
    {
    }

    public interface IResourceGroup
    {
        GameObject SelectedPrefab { get; }
        string SelectedPath { get; }
        string RandomPath { get; }
        IResourceData SelectedData { get; }
        int Count { get; }

        string GetResourcePath(int index);
    }

    [Flags]
    public enum ResourceDisplayFlags
    {
        None = 0,
        CanRemove = 1,
        AddLOD0Only = 2,
    }

    public interface IResourceGroupSystem
    {
        static IResourceGroupSystem Create(bool checkTransformIsIdentity)
        {
            return new ResourceGroupSystem(checkTransformIsIdentity);
        }

        string SelectedResourcePath { get; }
        string RandomResourcePath { get; }
        GameObject SelectedPrefab { get; }
        IResourceGroup SelectedGroup { get; }

        void Init(Func<IResourceData> modelDataCreator);
        void InspectorGUI(ResourceDisplayFlags displayFlags);
    }
}

//XDay