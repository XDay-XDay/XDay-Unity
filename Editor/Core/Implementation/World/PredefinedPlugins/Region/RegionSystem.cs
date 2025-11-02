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

namespace XDay.WorldAPI.Region.Editor
{
    [WorldPluginMetadata("区域层", "region_editor_data", typeof(RegionSystemCreateWindow), singleton: true)]
    internal partial class RegionSystem : EditorWorldPlugin
    {
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "region" };
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
        public override string TypeName => "EditorRegionSystem";
        public RegionSystemRenderer Renderer => m_Renderer;

        public RegionSystem()
        {
        }

        public RegionSystem(int id, int objectIndex, Bounds bounds, string name)
            : base(id, objectIndex)
        {
            m_Bounds = bounds;
            m_Name = name;
        }

        protected override void InitInternal()
        {
            Selection.selectionChanged += OnSelectionChanged;

            m_Renderer = new RegionSystemRenderer(World.Root.transform, this);

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
            var region = World.QueryObject<RegionObject>(objectID);
            if (region != null)
            {
                DestroyRegion(objectID);
            }
            else
            {
                var layer = World.QueryObject<RegionSystemLayer>(objectID);
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
            if (obj is RegionObject region)
            {
                AddRegion(region);
            }
            else if (obj is RegionSystemLayer layer)
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
            var obj = World.QueryObject<RegionObject>(objectID);
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
                if (name == "Layer Name")
                {
                    var layer = QueryObjectUndo(objectID) as RegionSystemLayer;
                    layer.Name = aspect.GetString();
                    layer.Renderer.SetAspect(objectID, name);
                }
                else if (name == "Layer Visibility")
                {
                    var layer = QueryObjectUndo(objectID) as RegionSystemLayer;
                    layer.SetEnabled(aspect.GetBoolean());
                    layer.Renderer.SetAspect(objectID, name);
                }
                else
                {
                    var obj = World.QueryObject<RegionObject>(objectID);
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
                var layer = QueryObjectUndo(objectID) as RegionSystemLayer;
                return IAspect.FromString(layer.Name);
            }

            if (name == "Layer Visibility")
            {
                var layer = QueryObjectUndo(objectID) as RegionSystemLayer;
                return IAspect.FromBoolean(layer.IsEnabled());
            }

            var obj = World.QueryObject<RegionObject>(objectID);
            if (obj != null)
            {
                return obj.GetAspect(objectID, name);
            }

            return null;
        }

        private void AddRegion(RegionObject region)
        {
            region.Layer.AddObject(region);

            if (region.GetVisibility() == WorldObjectVisibility.Undefined)
            {
                region.SetVisibility(WorldObjectVisibility.Visible);
            }

            region.Layer.Renderer.Create(region);
        }

        private void AddLayer(RegionSystemLayer layer)
        {
            m_Layers.Add(layer);
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "RegionSystem.Version");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");
            serializer.WriteObjectID(m_CurrentLayerID, "Current Layer ID", converter);

            serializer.WriteList(m_Layers, "Layers", (layer, index) =>
            {
                serializer.WriteSerializable(layer, $"Layer {index}", converter, false);
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("RegionSystem.Version");

            m_Name = deserializer.ReadString("Name");
            m_Bounds = deserializer.ReadBounds("Bounds");
            m_CurrentLayerID = deserializer.ReadInt32("Current Layer ID");

            m_Layers = deserializer.ReadList("Layer", (index) =>
            {
                return deserializer.ReadSerializable<RegionSystemLayer>($"Layer {index}", false);
            });
        }

        private bool DestroyRegion(int regionID)
        {
            var region = World.QueryObject<RegionObject>(regionID);
            DestroyObjectRenderer(region);

            foreach (var layer in m_Layers)
            {
                if (layer.DestroyObject(regionID))
                {
                    return true;
                }
            }
            return false;
        }

        private void DestroyLayer(RegionSystemLayer layer)
        {
            layer.Uninit();
            m_Layers.Remove(layer);
        }

        private RegionObject CloneObject(int id, int objectIndex, RegionObject obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, $"Clone object failed: {id}");
                return null;
            }
            var bytes = UndoSystem.Serialize(obj);
            var newObj = UndoSystem.Deserialize(id, objectIndex, bytes, World.ID, typeof(RegionObject).FullName, false) as RegionObject;
            return newObj;
        }

        private RegionSystemLayer GetLayer(string name)
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

        internal RegionSystemLayer GetLayer(int layerID)
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

        private void DestroyObjectRenderer(RegionObject obj)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.Renderer.Destroy(obj))
                {
                    break;
                }
            }
        }

        private RegionSystemLayer GetCurrentLayer()
        {
            if (m_CurrentLayerID == 0)
            {
                return null;
            }

            return World.QueryObject<RegionSystemLayer>(m_CurrentLayerID);
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
        private RegionSystemRenderer m_Renderer;
        private Bounds m_Bounds;
        private List<RegionSystemLayer> m_Layers = new();
        private int m_CurrentLayerID = 0;
        private const int m_Version = 1;
    }
}
