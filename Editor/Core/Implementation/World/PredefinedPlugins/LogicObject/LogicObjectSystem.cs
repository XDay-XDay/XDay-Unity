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

namespace XDay.WorldAPI.LogicObject.Editor
{
    [WorldPluginMetadata("LogicObjectSystem", "logic_object_editor_data", typeof(LogicObjectSystemCreateWindow), true)]
    internal partial class LogicObjectSystem : EditorWorldPlugin
    {
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "logic_object" };
        public override WorldPluginUsage Usage => WorldPluginUsage.BothInEditorAndGame;
        public override string Name
        {
            get => m_Name;
            set
            {
                Root.name = value;
                m_Name = value;
            }
        }
        public override Bounds Bounds => m_Bounds;
        public override string TypeName => "EditorLogicObjectSystem";

        public LogicObjectSystem()
        {
        }

        public LogicObjectSystem(int id, int objectIndex, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_Bounds = bounds;
            m_Name = name;
            m_ResourceDescriptorSystem = IEditorResourceDescriptorSystem.Create();
        }

        protected override void InitInternal()
        {
            if (m_Groups.Count == 0)
            {
                var group = new LogicObjectGroup(World.AllocateObjectID(), 0, "Default Group");
                m_Groups.Add(group);
                SetCurrentGroup(group.ID);
            }

            UndoSystem.AddUndoRedoCallback(UndoRedo);

            m_ResourceDescriptorSystem.Init(World);
            m_Indicator = IMeshIndicator.Create(World);
            m_Renderer = new LogicObjectSystemRenderer(World.Root.transform, World.GameObjectPool, this);
            m_ResourceGroupSystem.Init(null);

            foreach (var group in m_Groups)
            {
                group.Init(World);
            }

            ShowObjects();

            if (m_Groups.Count > 0)
            {
                SetCurrentGroup(m_Groups[0].ID);
            }
        }

        protected override void UninitInternal()
        {
            UndoSystem.RemoveUndoRedoCallback(UndoRedo);

            m_Indicator.OnDestroy();
            m_ResourceDescriptorSystem.Uninit();
            m_Renderer.OnDestroy();
            foreach (var group in m_Groups)
            {
                group.Uninit();
            }
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var obj = World.QueryObject<WorldObject>(objectID);
            if (obj != null)
            {
                if (obj is LogicObject logicObject)
                {
                    m_Renderer.Destroy(logicObject, 0, true);
                    DestroyObject(objectID);
                }
                else if (obj is LogicObjectGroup logicObjectGroup)
                {
                    m_Renderer.DestroyGroup(logicObjectGroup.ID);
                    logicObjectGroup.Uninit();
                    m_Groups.Remove(logicObjectGroup);
                    if (m_Groups.Count > 0)
                    {
                        SetCurrentGroup(m_Groups[0].ID);
                    }
                    else
                    {
                        SetCurrentGroup(0);
                    }
                }
                else
                {
                    Debug.Assert(false, "todo");
                }
            }
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            if (obj is LogicObject)
            {
                AddLogicObjectInternal(obj as LogicObject, objectIndex);
            }
            else if (obj is LogicObjectGroup)
            {
                AddLogicObjectGroupInternal(obj as LogicObjectGroup, objectIndex);
            }
            else
            {
                Debug.Assert(false, $"todo {obj.GetType()}");
            }
        }

        public void SetEnabled(int objectID, bool enabled, int lod, bool forceSet)
        {
            var obj = World.QueryObject<LogicObject>(objectID);
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
                var obj = World.QueryObject<LogicObject>(objectID);
                if (obj != null)
                {
                    var ok = obj.SetAspect(objectID, name, aspect);
                    if (ok)
                    {
                        m_Renderer.SetAspect(objectID, name);
                        return true;
                    }
                }

                var group = World.QueryObject<LogicObjectGroup>(objectID);
                if (group != null)
                {
                    var ok = group.SetAspect(objectID, name, aspect);
                    if (ok)
                    {
                        m_Renderer.SetAspect(objectID, name);
                        return true;
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

            var obj = World.QueryObject<LogicObject>(objectID);
            if (obj != null)
            {
                return obj.GetAspect(objectID, name);
            }

            var group = World.QueryObject<LogicObjectGroup>(objectID);
            if (group != null)
            {
                return group.GetAspect(objectID, name);
            }

            return null;
        }

        private void AddLogicObjectInternal(LogicObject obj, int objectIndex)
        {
            obj.Group.AddObject(obj, objectIndex);

            if (obj.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                obj.SetVisibility(WorldObjectVisibility.Visible);
            }

            if (obj.IsActive)
            {
                m_Renderer.Create(obj, 0, objectIndex);
            }
        }

        private void AddLogicObjectGroupInternal(LogicObjectGroup group, int objectIndex)
        {
            m_Groups.Add(group);
            m_Renderer.Create(group, objectIndex);
            SetCurrentGroup(group.ID);
        }

        private List<LogicObject> QueryObjectsInRectangle(Vector3 center, float width, float height)
        {
            var objects = new List<LogicObject>();
            foreach (var group in m_Groups)
            {
                foreach (var obj in group.Objects)
                {
                    var positionDelta = center - obj.Position;
                    if (Mathf.Abs(positionDelta.x) <= width * 0.5f &&
                        Mathf.Abs(positionDelta.z) <= height * 0.5f)
                    {
                        objects.Add(obj);
                    }
                }
            }
            return objects;
        }

        private bool DestroyObject(int objectID)
        {
            foreach (var group in m_Groups)
            {
                if (group.DestroyObject(objectID))
                {
                    return true;
                }
            }
            return false;
        }

        private LogicObject CreateObject(int id, string assetPath, Vector3 userPosition, Quaternion userRotation, Vector3 userScale, LogicObjectGroup group)
        {
            var descriptor = m_ResourceDescriptorSystem.CreateDescriptorIfNotExists(assetPath, World);
            var obj = new LogicObject(id, group.ObjectCount, userPosition, userRotation, userScale, descriptor, group);
            return obj;
        }

        private List<int> GetDirtyObjectIDs()
        {
            return m_DirtyObjectIDs;
        }

        private LogicObjectGroup GetCurrentGroup()
        {
            for (var i = 0; i < m_Groups.Count; i++)
            {
                if (m_Groups[i].ID == m_CurrentGroupID)
                {
                    return m_Groups[i];
                }
            }
            return null;
        }

        private int GetCurrentGroupIndex()
        {
            for (var i = 0; i < m_Groups.Count; i++)
            {
                if (m_Groups[i].ID == m_CurrentGroupID)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool HasGroup(string name)
        {
            foreach (var group in m_Groups)
            {
                if (group.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private enum ObjectCreateMode
        {
            Single = 0,
            Multiple,
        }

        private string m_Name;
        private ObjectCreateMode m_CreateMode = ObjectCreateMode.Single;
        private LogicObjectSystemRenderer m_Renderer;
        private IMeshIndicator m_Indicator;
        private Bounds m_Bounds;
        private CoordinateGenerateSetting m_CoordinateGenerateSetting = new();
        private List<LogicObjectGroup> m_Groups = new();
        private IEditorResourceDescriptorSystem m_ResourceDescriptorSystem;
        private float m_RemoveRange = 5;
        private List<int> m_DirtyObjectIDs = new();
        private const int m_Version = 2;
        private int m_CurrentGroupID = 0;
    }
}

//XDay