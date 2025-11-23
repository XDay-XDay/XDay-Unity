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

namespace XDay.WorldAPI.FOW.Editor
{
    [WorldPluginMetadata("迷雾层", "fog_editor_data", typeof(FogSystemCreateWindow), true)]
    public partial class FogSystem : EditorWorldPlugin
    {
        public struct CreateInfo
        {
            public int ID;
            public int ObjectIndex;
            public string Name;
            public int HorizontalGridCount;
            public int VerticalGridCount;
            public float GridWidth;
            public float GridHeight;
            public Vector2 Origin;
            public int MaxGridCountPerBlock;
            public int Layer0ID;
        }

        public override string Name
        {
            set
            {
                Root.name = value;
                m_Name = value;
            }
            get => m_Name;
        }
        public override List<string> GameFileNames => new() { "fog" };
        public override string TypeName => "EditorFogSystem";
        public override IPluginLODSystem LODSystem => m_LODSystem;
        public int LODCount => m_LODSystem.LODCount;
        public override GameObject Root => m_Renderer?.Root;
        public FogSystemRenderer Renderer => m_Renderer;
        public SortedSet<LayerBase> Layers => m_Layers;
        public int ActiveLayerID
        {
            get => m_ActiveLayerID;
            set
            {
                if (m_ActiveLayerID != value)
                {
                    m_ActiveLayerID = value;
                    m_Renderer.OnActiveLayerChange();
                }
            }
        }
        public override int FileIDOffset => WorldDefine.FOG_SYSTEM_FILE_ID_OFFSET;

        public FogSystem()
        {
        }

        public FogSystem(CreateInfo createInfo)
            : base(createInfo.ID, createInfo.ObjectIndex)
        {
            m_Name = createInfo.Name;

            m_LODSystem = IPluginLODSystem.Create(1);

            var horizontalBlockCount = Mathf.CeilToInt((float)createInfo.HorizontalGridCount / createInfo.MaxGridCountPerBlock);
            var verticalBlockCount = Mathf.CeilToInt((float)createInfo.VerticalGridCount / createInfo.MaxGridCountPerBlock);

            var layer = new Layer(createInfo.Layer0ID, objectIndex: 0, createInfo.ID, "迷雾", createInfo.HorizontalGridCount, createInfo.VerticalGridCount,
                createInfo.GridWidth, createInfo.GridHeight, createInfo.Origin, horizontalBlockCount, verticalBlockCount, LayerType.UserDefined, Color.white);
            m_Layers.Add(layer);
        }

        protected override void InitInternal()
        {
            m_LODSystem.Init(World.WorldLODSystem);

            foreach (var layer in m_Layers)
            {
                layer.Init(World);
            }

            m_Renderer = new FogSystemRenderer(this);

            SearchHooks();
        }

        protected override void UninitInternal()
        {
            m_Renderer?.Uninitialize();

            foreach (var layer in m_Layers)
            {
                layer.Uninit();
            }
        }

        protected override void UpdateInternal(float dt)
        {
            if (m_Layers.Count > 0 && m_ActiveLayerID == 0)
            {
                m_ActiveLayerID = m_Layers.Min.ID;
            }

            m_Renderer?.Update();
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            var layer = obj as Layer;
            m_Layers.Add(layer);

            m_Renderer.OnAddLayer(layer);
            ActiveLayerID = layer.ID;
        }

        public override void DestroyObjectUndo(int objectID)
        {
            var layer = QueryObjectUndo(objectID) as LayerBase;
            m_Renderer.OnRemoveLayer(layer.ID);
            layer.Uninit();
            m_Layers.Remove(layer);
            if (m_Layers.Count > 0)
            {
                m_ActiveLayerID = m_Layers.Min.ID;
            }
            else
            {
                m_ActiveLayerID = 0;
            }
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            return World.QueryObject<IWorldObject>(objectID);
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(writer, label, converter);

            writer.WriteInt32(m_EditorVersion, "EditorFogSystem.Version");

            writer.WriteString(m_Name, "Name");
            writer.WriteObjectID(m_ActiveLayerID, "Active Layer ID", converter);
            writer.WriteSerializable(m_LODSystem, "Plugin LOD System", converter, false);

            List<LayerBase> layers = new(m_Layers);
            writer.WriteList(layers, "Layers", (LayerBase layer, int index) =>
            {
                writer.WriteSerializable(layer, $"Layer {index}", converter, gameData: false);
            });
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            base.EditorDeserialize(reader, label);

            var version = reader.ReadInt32("EditorFogSystem.Version");

            m_Name = reader.ReadString("Name");
            m_ActiveLayerID = reader.ReadInt32("Active Layer ID");
            m_LODSystem = reader.ReadSerializable<IPluginLODSystem>("Plugin LOD System", false);

            var layers = reader.ReadList("Layers", (int index) =>
            {
                return reader.ReadSerializable<LayerBase>($"Layer {index}", gameData: false);
            });
            foreach (var layer in layers)
            {
                m_Layers.Add(layer);
            }
        }

        public LayerBase QueryLayer(string name)
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

        internal Layer GetActiveLayer()
        {
            return QueryLayer(m_ActiveLayerID) as Layer;
        }

        internal LayerBase QueryLayer(int layerID)
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

        internal LayerBase GetLayer(LayerType type)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.Type == type)
                {
                    return layer;
                }
            }
            return null;
        }

        [SerializeField]
        private string m_Name;
        [SerializeField]
        private int m_BrushSize = 10;
        [SerializeField]
        private int m_ActiveLayerID = 0;
        [SerializeField]
        private SortedSet<LayerBase> m_Layers = new(Comparer<LayerBase>.Create((x, y) => x.ObjectIndex.CompareTo(y.ObjectIndex)));
        private FogSystemRenderer m_Renderer;
        private IPluginLODSystem m_LODSystem;
        private const int m_EditorVersion = 1;
    }
}

