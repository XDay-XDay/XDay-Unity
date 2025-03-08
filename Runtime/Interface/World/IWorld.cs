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

    public interface IWorldObject : ISerializable
    {
        int ID { get; }
        int WorldID { get; }
        int ObjectIndex { get; }
        IWorld World { get; }
    }

    public interface IWorldPlugin
    {
        int ID { get; }
        string Name { get; }

        void Update();
    }

    public interface IWorld
    {
        int ID { get; }
        string Name { get; }
        float Width { get; }
        float Height { get; }
        Bounds Bounds { get; }
        int PluginCount { get; }
        string GameFolder { get; }
        string EditorFolder { get; }
        IAssetLoader AssetLoader { get; }
        ICameraManipulator CameraManipulator { get; set; }
        GameObject Root { get; }
        IGameObjectPool GameObjectPool { get; }
        ICameraVisibleAreaCalculator CameraVisibleAreaCalculator { get; }
        IWorldLODSystem WorldLODSystem { get; }

        int AllocateObjectID();
        T QueryObject<T>(int objectID) where T : class, IWorldObject;
        T QueryPlugin<T>() where T : class, IWorldPlugin;
        T QueryPlugin<T>(string name) where T : class, IWorldPlugin;
        List<T> QueryPlugins<T>() where T : class, IWorldPlugin;
        IDeserializer QueryGameDataDeserializer(int worldID, string gameDataFileName);
    }

    public interface IWorldManager
    {
        IWorld FirstWorld { get; }
        IAssetLoader WorldAssetLoader { get; }

        UniTask<IWorld> LoadWorldAsync(string name, Camera camera = null);
        UniTask<IWorld> LoadWorldAsync(int worldID, Camera camera = null);
        IWorld LoadWorld(string name, Camera camera = null);
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

        void Update(Camera camera);
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

        void Init(IWorld world);
        void Uninit();
        string GetPath(int lod);
        int QueryLODGroup(int lod);
    }

    public interface IResourceDescriptorSystem : ISerializable
    {
        void Init(IWorld world);
        void Uninit();
        IResourceDescriptor QueryDescriptor(string prefabPath);
    }

    /// <summary>
    /// decoration system interface
    /// </summary>
    public interface IDecorationSystem : IWorldPlugin
    {
        /// <summary>
        /// play animation on decoration object
        /// </summary>
        /// <param name="decorationID"></param>
        /// <param name="animationName"></param>
        /// <param name="alwaysPlay"></param>
        void PlayAnimation(int decorationID, string animationName, bool alwaysPlay = false);

        /// <summary>
        /// find decorations in a circle
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="decorationIDs"></param>
        void QueryDecorationIDsInCircle(Vector3 center, float radius, List<int> decorationIDs);

        /// <summary>
        /// show/hide decoration
        /// </summary>
        /// <param name="decorationID"></param>
        /// <param name="show"></param>
        void ShowDecoration(int decorationID, bool show);
    }
}


//XDay