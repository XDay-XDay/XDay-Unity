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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Decoration.Editor
{
    [WorldPluginMetadata("装饰层", "decoration_editor_data", typeof(DecorationSystemCreateWindow), true)]
    internal partial class DecorationSystem : EditorWorldPlugin, IObstacleSource
    {
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "decoration" };
        public override IPluginLODSystem LODSystem => m_PluginLODSystem;
        public int LODCount => m_PluginLODSystem.LODCount;
        public override WorldPluginUsage Usage => WorldPluginUsage.BothInEditorAndGame;
        public int CurrentLOD => m_PluginLODSystem.CurrentLOD;
        public int ActiveLOD => m_ActiveLOD;
        public override string Name
        {
            get=>m_Name;
            set
            {
                Root.name = value;
                m_Name = value;
            }
        }
        public override Bounds Bounds => m_Bounds;
        public override string TypeName => "EditorDecorationSystem";

        public DecorationSystem()
        {
        }

        public DecorationSystem(int id, int objectIndex, Vector2 runtimeGridSize, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_GameGridSize = runtimeGridSize;
            m_PluginLODSystem = IPluginLODSystem.Create(1);
            m_Bounds = bounds;
            m_Name = name;
            m_ResourceDescriptorSystem = IEditorResourceDescriptorSystem.Create();
        }

        protected override void InitInternal()
        {
            UndoSystem.AddUndoRedoCallback(UndoRedo);

            m_ResourceDescriptorSystem.Init(World);
            foreach (var kv in m_Decorations)
            {
                kv.Value.Init(World);
            }
            m_PluginLODSystem.Init(World.WorldLODSystem);

            m_Indicator = IMeshIndicator.Create(World);
            m_AreaUpdater = ICameraVisibleAreaUpdater.Create(World.CameraVisibleAreaCalculator);
            m_Renderer = new DecorationSystemRenderer(World.Root.transform, World.GameObjectPool, this);
            m_ResourceGroupSystem.Init(null);

            ShowObjects();
        }

        protected override void UninitInternal()
        {
            UndoSystem.RemoveUndoRedoCallback(UndoRedo);

            m_Indicator.OnDestroy();
            m_ResourceDescriptorSystem.Uninit();
            m_Renderer.OnDestroy();
            foreach (var kv in m_Decorations)
            {
                kv.Value.Uninit();
            }
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var obj = World.QueryObject<DecorationObject>(objectID);
            if (obj != null &&
                obj.IsActive)
            {
                m_Renderer.Destroy(obj, CurrentLOD, true);
            }
            DestroyObject(objectID);
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            AddObjectInternal(obj as DecorationObject);

            UpdateObjectLOD(obj.ID, m_ActiveLOD);
        }

        public void SetEnabled(int objectID, bool enabled, int lod, bool forceSet)
        {
            var obj = World.QueryObject<DecorationObject>(objectID);
            if (obj != null)
            {
                if (forceSet ||
                    obj.SetEnabled(enabled))
                {
                    m_Renderer.ToggleVisibility(obj, lod);
                }
            }
            else
            {
                Debug.LogError($"SetObjectEnabled {objectID} failed!");
            }
        }

        public void ClearDirtyObjects()
        {
            m_DirtyObjectIDs.Clear();
        }

        public void NotifyObjectDirty(int objectID)
        {
            if (!m_DirtyObjectIDs.Contains(objectID))
            {
                m_DirtyObjectIDs.Add(objectID);
            }
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (!base.SetAspect(objectID, name, aspect))
            {
                if (name == DecorationDefine.LOD_NAME)
                {
                    ChangeActiveLOD(aspect.GetInt32(), false);
                    return true;
                }

                var obj = World.QueryObject<DecorationObject>(objectID);
                if (obj != null)
                {
                    var ok = obj.SetAspect(objectID, name, aspect);
                    if (ok)
                    {
                        m_Renderer.SetAspect(objectID, name);
                    }
                }
            }
            return true;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (name == DecorationDefine.LOD_NAME)
            {
                return IAspect.FromInt32(m_ActiveLOD);
            }

            var obj = World.QueryObject<DecorationObject>(objectID);
            if (obj != null)
            {
                return obj.GetAspect(objectID, name);
            }

            return null;
        }

        private void UpdateObjectLOD(int objectID, int lod)
        {
            m_Renderer.UpdateObjectLOD(objectID, lod);
        }

        private void AddObjectInternal(DecorationObject decoration)
        {
            m_Decorations.Add(decoration.ID, decoration);

            if (decoration.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                decoration.SetVisibility(WorldObjectVisibility.Visible);
            }

            if (decoration.IsActive)
            {
                m_Renderer.Create(decoration, CurrentLOD);
            }
        }

        private int QueryObjectCountInLOD(int lod)
        {
            var count = 0;
            foreach (var obj in m_Decorations.Values)
            {
                if (obj.ExistsInLOD(lod))
                {
                    ++count;
                }
            }
            return count;
        }

        private List<DecorationObject> QueryObjectsInRectangle(Vector3 center, float width, float height)
        {
            var objects = new List<DecorationObject>();
            foreach (var kv in m_Decorations)
            {
                var freeObject = kv.Value;
                var positionDelta = center - freeObject.Position;
                if (Mathf.Abs(positionDelta.x) <= width * 0.5f &&
                    Mathf.Abs(positionDelta.z) <= height * 0.5f)
                {
                    objects.Add(freeObject);
                }
            }
            return objects;
        }

        private bool DestroyObject(int objectID)
        {
            var ok = m_Decorations.TryGetValue(objectID, out var decoration);
            if (decoration != null)
            {
                decoration.Uninit();
                m_Decorations.Remove(objectID);
            }
            return ok;
        }

        private DecorationObject CreateObject(int id, string assetPath, Vector3 userPosition, Quaternion userRotation, Vector3 userScale)
        {
            var descriptor = m_ResourceDescriptorSystem.CreateDescriptorIfNotExists(assetPath, World);
            var obj = new DecorationObject(id, m_Decorations.Count, false, LODLayerMask.AllLOD, userPosition, userRotation, userScale, descriptor);
            return obj;
        }

        private DecorationObject CloneObject(int id, int objectIndex, DecorationObject obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, $"Clone object failed: {id}");
                return null;
            }
            var bytes = UndoSystem.Serialize(obj);
            var newObj = UndoSystem.Deserialize(id, objectIndex, bytes, World.ID, typeof(DecorationObject).FullName, false) as DecorationObject;
            return newObj;
        }

        private List<int> GetDirtyObjectIDs()
        {
            return m_DirtyObjectIDs;
        }

        private enum ObjectCreateMode
        {
            Single = 0,
            Multiple,
        }

        private string m_Name;
        private ObjectCreateMode m_CreateMode = ObjectCreateMode.Single;
        private DecorationSystemRenderer m_Renderer;
        private IMeshIndicator m_Indicator;
        private Vector2 m_GameGridSize;
        private IPluginLODSystem m_PluginLODSystem;
        private Bounds m_Bounds;
        private ICameraVisibleAreaUpdater m_AreaUpdater;
        private CoordinateGenerateSetting m_CoordinateGenerateSetting = new();
        private Dictionary<int, DecorationObject> m_Decorations = new();
        private int m_ActiveLOD = -1;
        private IEditorResourceDescriptorSystem m_ResourceDescriptorSystem;
        private float m_RemoveRange = 5;
        private List<int> m_DirtyObjectIDs = new();
        private const int m_Version = 1;
    }
}

//XDay