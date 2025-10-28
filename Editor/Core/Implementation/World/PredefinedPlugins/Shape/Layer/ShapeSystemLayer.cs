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

namespace XDay.WorldAPI.Shape.Editor
{
    internal class ShapeSystemLayer : WorldObject
    {
        public ShapeSystemLayerRenderer Renderer => m_Renderer;
        public ShapeSystem System => m_System;
        public override string TypeName => "ShapeSystemLayer";
        public string Name => m_Name;
        public Dictionary<int, ShapeObject> Shapes => m_Shapes;
        public int ShapeCount => Shapes.Count;

        public ShapeSystemLayer()
        {
        }

        public ShapeSystemLayer(int id, int index, string name)
            : base(id, index)
        {
            m_Name = name;
        }

        public virtual void Init(IWorld world, Transform parent, ShapeSystem system)
        {
            base.Init(world);

            m_System = system;

            foreach (var kv in m_Shapes)
            {
                kv.Value.Init(world);
            }

            m_Renderer = new ShapeSystemLayerRenderer(parent, this);

            ShowObjects();
        }

        protected override void OnInit()
        {
        }

        protected override void OnUninit()
        {
            m_Renderer.OnDestroy();
            foreach (var kv in m_Shapes)
            {
                kv.Value.Uninit();
            }
        }

        public List<IObstacle> GetObstacles()
        {
            List<IObstacle> obstacles = new();
            foreach (var kv in m_Shapes)
            {
                obstacles.Add(kv.Value);
            }
            return obstacles;
        }

        private void ShowObjects()
        {
            foreach (var kv in m_Shapes)
            {
                m_Renderer.ToggleVisibility(kv.Value);
            }
        }

        internal void Update()
        {
            m_Renderer?.Update();
        }

        internal bool Contains(int objectID)
        {
            return m_Shapes.ContainsKey(objectID);
        }

        internal void AddObject(ShapeObject shape)
        {
            m_Shapes.Add(shape.ID, shape);
        }

        internal bool DestroyObject(int shapeID)
        {
            foreach (var shape in m_Shapes.Values)
            {
                if (shape.ID == shapeID)
                {
                    shape.Uninit();
                    m_Shapes.Remove(shape.ID);
                    return true;
                }
            }
            Debug.Assert(false, $"Destroy object {shapeID} failed!");
            return false;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_EditorVersion, "ShapeSystemLayer.Version");
            serializer.WriteString(m_Name, "Name");
            
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

            deserializer.ReadInt32("ShapeSystemLayer.Version");

            m_Name = deserializer.ReadString("Name");

            var allObjects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<ShapeObject>($"Object {index}", false);
            });
            foreach (var obj in allObjects)
            {
                m_Shapes.Add(obj.ID, obj);
            }
        }

        private string m_Name;
        private ShapeSystem m_System;
        private Dictionary<int, ShapeObject> m_Shapes = new();
        private ShapeSystemLayerRenderer m_Renderer = null;
        private const int m_EditorVersion = 1;
    }
}
