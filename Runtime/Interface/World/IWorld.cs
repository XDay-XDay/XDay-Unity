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

using Cysharp.Threading.Tasks;
using XDay.CameraAPI;
using XDay.UtilityAPI;
using System.Collections.Generic;
using UnityEngine;
using XDay.AssetAPI;
using System;
using UnityEngine.Scripting;

namespace XDay.WorldAPI
{
    public enum WorldObjectVisibility
    {
        Undefined,
        Invisible,
        Visible,
    }

    public enum WorldPluginUsage
    {
        BothInEditorAndGame,
        OnlyGenerateData,
        OnlyInEditor,
    }

    [Preserve]
    public interface IWorldObject : ISerializable
    {
        int ID { get; }
        int WorldID { get; }
        int ObjectIndex { get; }
        IWorld World { get; }
        bool AllowUndo => true;
    }

    public interface IWorldPlugin
    {
        int ID { get; }
        string Name { get; }

        void Update(float dt);
    }

    public delegate void LODChangeCallback(int oldLOD, int newLOD);

    [Preserve]
    public interface IWorld
    {
        int ID { get; }
        string Name { get; }
        float Width { get; }
        float Height { get; }
        int CurrentLOD { get; }
        Bounds Bounds { get; }
        int PluginCount { get; }
        string GameFolder { get; }
        string EditorFolder { get; }
        IAssetLoader AssetLoader { get; }
        ICameraManipulator CameraManipulator { get; set; }
        IWorldManager WorldManager { get; }
        GameObject Root { get; }
        IGameObjectPool GameObjectPool { get; }
        ICameraVisibleAreaCalculator CameraVisibleAreaCalculator { get; }
        IWorldLODSystem WorldLODSystem { get; }

        int AllocateObjectID();
        T QueryObject<T>(int objectID) where T : class, IWorldObject;
        T QueryPlugin<T>() where T : class, IWorldPlugin;
        T QueryPlugin<T>(string name) where T : class, IWorldPlugin;
        int QueryPluginIndex(IWorldPlugin plugin);
        List<T> QueryPlugins<T>() where T : class, IWorldPlugin;
        IWorldPlugin GetPlugin(int index);
        bool HasPlugin(System.Type type);
        IDeserializer QueryGameDataDeserializer(int worldID, string gameDataFileName);
        void RegisterLODChangeEvent(LODChangeCallback callback);
        void UnregisterLODChangeEvent(LODChangeCallback callback);
    }

    public interface IWorldManager
    {
        IWorld FirstWorld { get; }
        IAssetLoader WorldAssetLoader { get; }
        ITaskSystem TaskSystem { get; }

        UniTask<IWorld> LoadWorldAsync(string name, Func<Camera> cameraQueryFunc = null, bool createManipulator = true);
        UniTask<IWorld> LoadWorldAsync(int worldID, Func<Camera> cameraQueryFunc = null, bool createManipulator = true);
        IWorld LoadWorld(string name, Func<Camera> cameraQueryFunc = null, bool createManipulator = true);
        void UnloadWorld(string name);
        void UnloadWorld(int worldID);
        void LoadWorldRenderer(string name);
        void UnloadWorldRenderer(string name);
        IWorld QueryWorld(int worldID);
    }

    public interface ICameraVisibleAreaCalculator
    {
        Rect VisibleArea { get; }
        Rect ExpandedArea { get; }
        Vector2 ExpandSize { get; set; }

        void Update(Camera camera);
        Rect GetVisibleAreas(Camera camera);
        void DebugDraw();
    }

    public interface ICameraVisibleAreaUpdater
    {
        static ICameraVisibleAreaUpdater Create(ICameraVisibleAreaCalculator calculator)
        {
            return new CameraVisibleAreaUpdater(calculator);
        }

        Rect CurrentArea { get; }
        Rect PreviousArea { get; }

        void Reset();
        bool BeginUpdate();
        void EndUpdate();
    }

    public interface IResourceDescriptor : IWorldObject
    {
        int LODCount { get; }
        bool IsValid { get; }

        void Init(IWorld world);
        void Uninit();
        string GetPath(int lod);
        int QueryLODGroup(int lod);
    }

    [Preserve]
    public interface IResourceDescriptorSystem : ISerializable
    {
        void Init(IWorld world);
        void Uninit();
        IResourceDescriptor QueryDescriptor(string prefabPath);
    }
}


//XDay