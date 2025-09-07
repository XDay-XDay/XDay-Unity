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

namespace XDay.WorldAPI.Shape.Editor
{
    [WorldPluginMetadata("形状层", "shape_editor_data", typeof(ShapeSystemCreateWindow), singleton:true)]
    internal partial class ShapeSystem : EditorWorldPlugin, IObstacleSource
    {
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "shape" };
        public override IPluginLODSystem LODSystem => null;
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
        public override string TypeName => "EditorShapeSystem";
        public bool ShowVertexIndex => m_ShowVertexIndex;
        public float VertexDisplaySize => m_VertexDisplaySize;

        public ShapeSystem()
        {
        }

        public ShapeSystem(int id, int objectIndex, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_Bounds = bounds;
            m_Name = name;
        }

        protected override void InitInternal()
        {
            Selection.selectionChanged += OnSelectionChanged;

            foreach (var kv in m_Shapes)
            {
                kv.Value.Init(World);
            }

            m_Renderer = new ShapeSystemRenderer(World.Root.transform, this);

            ShowObjects();
        }

        protected override void UninitInternal()
        {
            m_Renderer.OnDestroy();
            foreach (var kv in m_Shapes)
            {
                kv.Value.Uninit();
            }

            Selection.selectionChanged -= OnSelectionChanged;
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                m_Renderer.Destroy(obj);
            }
            DestroyObject(objectID);
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            AddObjectInternal(obj as ShapeObject);
        }

        public void SetEnabled(int objectID, bool enabled, bool forceSet)
        {
            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                if (forceSet ||
                    obj.SetEnabled(enabled))
                {
                    m_Renderer.ToggleVisibility(obj);
                }
            }
            else
            {
                Debug.LogError($"SetObjectEnabled {objectID} failed!");
            }
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (!base.SetAspect(objectID, name, aspect))
            {
                if (name == ShapeDefine.SHAPE_VERTEX_INDEX_NAME)
                {
                    m_ShowVertexIndex = aspect.GetBoolean();
                    return true;
                }

                if (name == ShapeDefine.SHAPE_VERTEX_DISPLAY_SIZE)
                {
                    m_VertexDisplaySize = aspect.GetSingle();
                    return true;
                }

                var obj = World.QueryObject<ShapeObject>(objectID);
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

            if (name == ShapeDefine.SHAPE_VERTEX_DISPLAY_SIZE)
            {
                return IAspect.FromSingle(m_VertexDisplaySize);
            }

            if (name == ShapeDefine.SHAPE_VERTEX_INDEX_NAME)
            {
                return IAspect.FromBoolean(m_ShowVertexIndex);
            }

            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                return obj.GetAspect(objectID, name);
            }

            return null;
        }

        private void AddObjectInternal(ShapeObject shape)
        {
            m_Shapes.Add(shape.ID, shape);

            if (shape.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                shape.SetVisibility(WorldObjectVisibility.Visible);
            }

            m_Renderer.Create(shape);
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "ShapeSystem.Version");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");
            serializer.WriteBoolean(m_ShowVertexIndex, "Show Vertex Index");
            serializer.WriteSingle(m_VertexDisplaySize, "Vertex Display Size");

            var allObjects = new List<ShapeObject>();
            foreach (var p in m_Shapes)
            {
                allObjects.Add(p.Value);
            }

            serializer.WriteList(allObjects, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("ShapeSystem.Version");

            m_Name = deserializer.ReadString("Name");
            m_Bounds = deserializer.ReadBounds("Bounds");
            m_ShowVertexIndex = deserializer.ReadBoolean("Show Vertex Index");
            m_VertexDisplaySize = deserializer.ReadSingle("Vertex Display Size");

            var allObjects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<ShapeObject>($"Object {index}", false);
            });
            foreach (var obj in allObjects)
            {
                m_Shapes.Add(obj.ID, obj);
            }
        }

        private bool DestroyObject(int shapeID)
        {
            foreach (var shape in m_Shapes.Values)
            {
                if (shape.ID == shapeID)
                {
                    shape.Uninit();
                    m_Shapes.Remove(shape.ID);
                    RemovePickInfo(shapeID);
                    return true;
                }
            }
            Debug.Assert(false, $"Destroy object {shapeID} failed!");
            return false;
        }

        private void RemovePickInfo(int shapeID)
        {
            for (var i = m_PickedShapes.Count - 1; i >= 0; i--)
            {
                if (m_PickedShapes[i].ShapeID == shapeID)
                {
                    m_PickedShapes.RemoveAt(i);
                    break;
                }
            }
        }

        private ShapeObject CloneObject(int id, int objectIndex, ShapeObject obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, $"Clone object failed: {id}");
                return null;
            }
            var bytes = UndoSystem.Serialize(obj);
            var newObj = UndoSystem.Deserialize(id, objectIndex, bytes, World.ID, typeof(ShapeObject).FullName, false) as ShapeObject;
            return newObj;
        }

        List<IObstacle> IObstacleSource.GetObstacles()
        {
            List<IObstacle> obstacles = new();
            foreach (var kv in m_Shapes)
            {
                obstacles.Add(kv.Value);
            }
            return obstacles;
        }

        private string m_Name;
        private ShapeSystemRenderer m_Renderer;
        private Bounds m_Bounds;
        private Dictionary<int, ShapeObject> m_Shapes = new();
        private float m_VertexDisplaySize = 5f;
        private bool m_ShowVertexIndex = false;
        private const int m_Version = 1;
    }
}
