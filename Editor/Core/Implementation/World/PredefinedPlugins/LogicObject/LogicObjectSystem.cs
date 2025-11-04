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
using UnityEditor;
using UnityEngine;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.LogicObject.Editor
{
    [WorldPluginMetadata("逻辑层", "logic_object_editor_data", typeof(LogicObjectSystemCreateWindow), true)]
    public partial class LogicObjectSystem : EditorWorldPlugin
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
        public override int FileIDOffset => WorldDefine.LOGIC_OBJECT_SYSTEM_FILE_ID_OFFSET;

        public LogicObjectSystem()
        {
        }

        public LogicObjectSystem(int id, int objectIndex, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_Bounds = bounds;
            m_Name = name;
            m_ResourceDescriptorSystem = IEditorResourceDescriptorSystem.Create();
            m_ResourceGroupSystem = IResourceGroupSystem.Create(false);
        }

        protected override void InitInternal()
        {
            m_CreateMode = (ObjectCreateMode)EditorPrefs.GetInt(LogicObjectDefine.CREATE_MODE, (int)ObjectCreateMode.Single);
            m_RemoveRange = EditorPrefs.GetFloat(LogicObjectDefine.REMOVE_RANGE, 5);
            m_CoordinateGenerateSetting.CircleRadius = EditorPrefs.GetFloat(LogicObjectDefine.CIRCLE_RADIUS, 10);
            m_CoordinateGenerateSetting.RectWidth = EditorPrefs.GetFloat(LogicObjectDefine.RECT_WIDTH, 5);
            m_CoordinateGenerateSetting.RectHeight = EditorPrefs.GetFloat(LogicObjectDefine.RECT_HEIGHT, 5);
            m_CoordinateGenerateSetting.Count = EditorPrefs.GetInt(LogicObjectDefine.OBJECT_COUNT, 10);
            m_CoordinateGenerateSetting.Space = EditorPrefs.GetFloat(LogicObjectDefine.SPACE, 1);
            m_CoordinateGenerateSetting.Random = EditorPrefs.GetBool(LogicObjectDefine.RANDOM, false);
            m_CoordinateGenerateSetting.BorderSize = EditorPrefs.GetFloat(LogicObjectDefine.BORDER_SIZE, 0);
            m_CoordinateGenerateSetting.LineEquidistant = EditorPrefs.GetBool(LogicObjectDefine.LINE_EQUIDISTANT, false);

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

            Selection.selectionChanged += OnSelectionChanged;
        }

        protected override void UninitInternal()
        {
            Selection.selectionChanged -= OnSelectionChanged;

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

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            SyncObjectTransforms();

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "LogicObjectSystem.Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");

            //sync object names
            foreach (var group in m_Groups)
            {
                foreach (var obj in group.Objects)
                {
                    var gameObject = m_Renderer.QueryObject(obj.ID);
                    obj.Name = gameObject.name;
                }
            }

            serializer.WriteList(m_Groups, "Groups", (group, index) =>
            {
                serializer.WriteSerializable(group, $"Group {index}", converter, false);
            });

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, false);
            serializer.WriteSerializable(m_ResourceGroupSystem, "Resource Group System", converter, false);

            EditorPrefs.SetInt(LogicObjectDefine.CREATE_MODE, (int)m_CreateMode);
            EditorPrefs.SetFloat(LogicObjectDefine.REMOVE_RANGE, m_RemoveRange);
            EditorPrefs.SetFloat(LogicObjectDefine.CIRCLE_RADIUS, m_CoordinateGenerateSetting.CircleRadius);
            EditorPrefs.SetFloat(LogicObjectDefine.RECT_WIDTH, m_CoordinateGenerateSetting.RectWidth);
            EditorPrefs.SetFloat(LogicObjectDefine.RECT_HEIGHT, m_CoordinateGenerateSetting.RectHeight);
            EditorPrefs.SetInt(LogicObjectDefine.OBJECT_COUNT, m_CoordinateGenerateSetting.Count);
            EditorPrefs.SetFloat(LogicObjectDefine.SPACE, m_CoordinateGenerateSetting.Space);
            EditorPrefs.SetBool(LogicObjectDefine.RANDOM, m_CoordinateGenerateSetting.Random);
            EditorPrefs.SetFloat(LogicObjectDefine.BORDER_SIZE, m_CoordinateGenerateSetting.BorderSize);
            EditorPrefs.SetBool(LogicObjectDefine.LINE_EQUIDISTANT, m_CoordinateGenerateSetting.LineEquidistant);
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            var version = deserializer.ReadInt32("LogicObjectSystem.Version");

            m_Name = deserializer.ReadString("Name");
            m_Bounds = deserializer.ReadBounds("Bounds");

            m_Groups = deserializer.ReadList("Groups", (index) =>
            {
                return deserializer.ReadSerializable<LogicObjectGroup>($"Group {index}", false);
            });

            m_ResourceDescriptorSystem = deserializer.ReadSerializable<EditorResourceDescriptorSystem>("Resource Descriptor System", false);
            m_ResourceGroupSystem = deserializer.ReadSerializable<IResourceGroupSystem>("Resource Group System", false);
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
                    if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP)
                    {
                        ChangeGroup(obj, aspect.GetString());
                        return true;
                    }

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

        private void ChangeGroup(LogicObject obj, string groupName)
        {
            var newGroup = GetGroup(groupName);
            var oldGroup = obj.Group;

            if (newGroup != oldGroup) {

                oldGroup.RemoveObject(obj);
                obj.Group = newGroup;
                newGroup.AddObject(obj, 0);
                m_Renderer.MoveObjectToGroup(obj.ID, newGroup.ID);
            }
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
        private const int m_Version = 3;
        private int m_CurrentGroupID = 0;
    }
}

//XDay