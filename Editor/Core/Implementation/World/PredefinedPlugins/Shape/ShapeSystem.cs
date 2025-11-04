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
        public ShapeSystemRenderer Renderer => m_Renderer;
        public override int FileIDOffset => WorldDefine.SHAPE_SYSTEM_FILE_ID_OFFSET;

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

            m_Renderer = new ShapeSystemRenderer(World.Root.transform, this);

            foreach (var layer in m_Layers)
            {
                layer.Init(World);
            }
        }

        protected override void UninitInternal()
        {
            m_Renderer.OnDestroy();

            foreach (var layer in m_Layers)
            {
                layer.Uninit();
            }

            Selection.selectionChanged -= OnSelectionChanged;
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var shape = World.QueryObject<ShapeObject>(objectID);
            if (shape != null)
            {
                DestroyShape(objectID);
            }
            else
            {
                var layer = World.QueryObject<ShapeSystemLayer>(objectID);
                if (layer != null)
                {
                    DestroyLayer(layer);
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            if (obj is ShapeObject shape)
            {
                AddShape(shape);
            }
            else if (obj is ShapeSystemLayer layer)
            {
                AddLayer(layer);
            }
            else
            {
                Debug.Assert(false, $"unknown object {obj.GetType()}");
            }
        }

        public void SetEnabled(int objectID, bool enabled, bool forceSet)
        {
            var obj = World.QueryObject<ShapeObject>(objectID);
            if (obj != null)
            {
                if (forceSet ||
                    obj.SetEnabled(enabled))
                {
                    foreach (var layer in m_Layers)
                    {
                        if (layer.Contains(objectID))
                        {
                            layer.Renderer.ToggleVisibility(obj);
                            break;
                        }
                    }
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

                if (name == "Layer Name")
                {
                    var layer = QueryObjectUndo(objectID) as ShapeSystemLayer;
                    layer.Name = aspect.GetString();
                    layer.Renderer.SetAspect(objectID, name);
                }
                else if (name == "Layer Visibility")
                {
                    var layer = QueryObjectUndo(objectID) as ShapeSystemLayer;
                    layer.SetEnabled(aspect.GetBoolean());
                    layer.Renderer.SetAspect(objectID, name);
                }
                else
                {
                    var obj = World.QueryObject<ShapeObject>(objectID);
                    if (obj != null)
                    {
                        var ok = obj.SetAspect(objectID, name, aspect);
                        if (ok)
                        {
                            foreach (var layer in m_Layers)
                            {
                                if (layer.Contains(objectID))
                                {
                                    layer.Renderer.SetAspect(objectID, name);
                                    break;
                                }
                            }
                        }
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

            if (name == "Layer Name")
            {
                var layer = QueryObjectUndo(objectID) as ShapeSystemLayer;
                return IAspect.FromString(layer.Name);
            }

            if (name == "Layer Visibility")
            {
                var layer = QueryObjectUndo(objectID) as ShapeSystemLayer;
                return IAspect.FromBoolean(layer.IsEnabled());
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

        private void AddShape(ShapeObject shape)
        {
            shape.Layer.AddObject(shape);

            if (shape.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                shape.SetVisibility(WorldObjectVisibility.Visible);
            }

            shape.Layer.Renderer.Create(shape);
        }

        private void AddLayer(ShapeSystemLayer layer)
        {
            m_Layers.Add(layer);
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "ShapeSystem.Version");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");
            serializer.WriteBoolean(m_ShowVertexIndex, "Show Vertex Index");
            serializer.WriteSingle(m_VertexDisplaySize, "Vertex Display Size");
            serializer.WriteObjectID(m_CurrentLayerID, "Current Layer ID", converter);

            serializer.WriteList(m_Layers, "Layers", (layer, index) =>
            {
                serializer.WriteSerializable(layer, $"Layer {index}", converter, false);
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
            m_CurrentLayerID = deserializer.ReadInt32("Current Layer ID");

            m_Layers = deserializer.ReadList("Layer", (index) =>
            {
                return deserializer.ReadSerializable<ShapeSystemLayer>($"Layer {index}", false);
            });
        }

        private bool DestroyShape(int shapeID)
        {
            var shape = World.QueryObject<ShapeObject>(shapeID);
            DestroyObjectRenderer(shape);

            foreach (var layer in m_Layers)
            {
                if (layer.DestroyObject(shapeID))
                {
                    RemovePickInfo(shapeID);
                    return true;
                }
            }
            return false;
        }

        private void DestroyLayer(ShapeSystemLayer layer)
        {
            layer.Uninit();
            m_Layers.Remove(layer);
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
            var layer = GetLayer("Obstacle");
            if (layer != null)
            {
                return layer.GetObstacles();
            }
            return new();
        }

        private ShapeSystemLayer GetLayer(string name)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.Name == name)
                {
                    return layer;
                }
            }
            return null;
        }

        internal ShapeSystemLayer GetLayer(int layerID)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.ID == layerID)
                {
                    return layer;
                }
            }
            return null;
        }

        public int GetLayerIndex(int layerID)
        {
            var idx = 0;
            foreach (var layer in m_Layers)
            {
                if (layer.ID == layerID)
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        private void DestroyObjectRenderer(ShapeObject obj)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.Renderer.Destroy(obj))
                {
                    break;
                }
            }
        }

        private ShapeSystemLayer GetCurrentLayer()
        {
            if (m_CurrentLayerID == 0)
            {
                return null;
            }

            return World.QueryObject<ShapeSystemLayer>(m_CurrentLayerID);
        }

        private void SetCurrentLayer(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < m_Layers.Count)
            {
                var idx = 0;
                foreach (var layer in m_Layers)
                {
                    if (idx == layerIndex)
                    {
                        m_CurrentLayerID = layer.ID;
                        return;
                    }
                    ++idx;
                }
            }
            else
            {
                m_CurrentLayerID = 0;
            }
        }

        private string m_Name;
        private ShapeSystemRenderer m_Renderer;
        private Bounds m_Bounds;
        private List<ShapeSystemLayer> m_Layers = new();
        private float m_VertexDisplaySize = 5f;
        private bool m_ShowVertexIndex = false;
        private int m_CurrentLayerID = 0;
        private const int m_Version = 1;
    }
}
