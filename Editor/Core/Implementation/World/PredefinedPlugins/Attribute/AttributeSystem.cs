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
using UnityEditor.SceneManagement;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Shape;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Attribute.Editor
{
    internal enum ObstacleCalculationMode : byte
    {
        Disable,
        ByObstacleTag,
        ByShapeColliders,
        All,
    }

    [WorldPluginMetadata("属性层", "attribute_editor_data", typeof(AttributeSystemCreateWindow), true)]
    public partial class AttributeSystem : EditorWorldPlugin
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
        public override List<string> GameFileNames => new() { "attribute" };
        public override string TypeName => "EditorAttributeSystem";
        public int LODCount => 1;
        public override GameObject Root => m_Renderer?.Root;
        public AttributeSystemRenderer Renderer => m_Renderer;
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
        public override int FileIDOffset => WorldDefine.ATTRIBUTE_SYSTEM_FILE_ID_OFFSET;

        public AttributeSystem()
        {
        }

        public AttributeSystem(CreateInfo createInfo)
            : base(createInfo.ID, createInfo.ObjectIndex)
        {
            m_Name = createInfo.Name;

            var horizontalBlockCount = Mathf.CeilToInt((float)createInfo.HorizontalGridCount / createInfo.MaxGridCountPerBlock);
            var verticalBlockCount = Mathf.CeilToInt((float)createInfo.VerticalGridCount / createInfo.MaxGridCountPerBlock);

            var layer = new Layer(createInfo.Layer0ID, objectIndex: 0, createInfo.ID, "障碍", createInfo.HorizontalGridCount, createInfo.VerticalGridCount,
                createInfo.GridWidth, createInfo.GridHeight, createInfo.Origin, horizontalBlockCount, verticalBlockCount, LayerType.AutoObstacle, Color.white);
            m_Layers.Add(layer);
        }

        protected override void InitInternal()
        {
            foreach (var layer in m_Layers)
            {
                layer.Init(World);
                //修复旧的AttributeSystem
                layer.AttributeSystemID = ID;
            }

            m_Renderer = new AttributeSystemRenderer(this);

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

        /// <summary>
        /// 将实现了IObstacleSource的接口作为障碍物来源，将障碍物覆盖范围内的格子设置为非空地
        /// </summary>
        public void CalculateIfGridHasObstacles(bool prompt)
        {
            if (m_ObstacleMode == ObstacleCalculationMode.Disable)
            {
                return;
            }

            if (!prompt || EditorUtility.DisplayDialog("注意", "确定计算?", "确定", "取消"))
            {
                var layer = GetLayer(LayerType.AutoObstacle);

                if (layer == null)
                {
                    Debug.LogError("没有自动碰撞层,先设置层的类型为自动碰撞");
                    return;
                }

                var hasObstacle = new bool[layer.VerticalGridCount, layer.HorizontalGridCount];

                List<IObstacle> allObstacles = new();
                if (m_ObstacleMode == ObstacleCalculationMode.All ||
                    m_ObstacleMode == ObstacleCalculationMode.ByObstacleTag)
                {
                    for (var i = 0; i < World.PluginCount; ++i)
                    {
                        var plugin = World.GetPlugin(i);
                        if (plugin is IObstacleSource obstacleSource)
                        {
                            var obstacles = obstacleSource.GetObstacles();
                            if (obstacles != null)
                            {
                                allObstacles.AddRange(obstacles);
                            }
                        }
                    }
                }

                if (m_ObstacleMode == ObstacleCalculationMode.All ||
                    m_ObstacleMode == ObstacleCalculationMode.ByShapeColliders)
                {
                    var shapeColliders = GetShapeColliders();
                    allObstacles.AddRange(shapeColliders);
                }

                foreach (var obstacle in allObstacles)
                {
                    var bounds = obstacle.WorldBounds;
                    var minCoord = layer.PositionToCoordinate(bounds.min.x, bounds.min.y);
                    var maxCoord = layer.PositionToCoordinate(bounds.max.x, bounds.max.y);
                    var polygon = obstacle.WorldPolygon;
                    for (var y = minCoord.y; y <= maxCoord.y; ++y)
                    {
                        for (var x = minCoord.x; x <= maxCoord.x; ++x)
                        {
                            if (x >= 0 && x < layer.HorizontalGridCount && y >= 0 && y < layer.VerticalGridCount)
                            {
                                var min = layer.CoordinateToPosition(x, y);
                                var max = layer.CoordinateToPosition(x + 1, y + 1);
                                hasObstacle[y, x] |= Helper.RectInPolygon(min, max, polygon);
                            }
                        }
                    }
                }

                var data = new uint[layer.VerticalGridCount * layer.HorizontalGridCount];
                for (var y = 0; y < layer.VerticalGridCount; ++y)
                {
                    for (var x = 0; x < layer.HorizontalGridCount; ++x)
                    {
                        data[y * layer.HorizontalGridCount + x] = hasObstacle[y, x] ? 1u : 0;
                    }
                }

                UndoSystem.SetAspect(this,
                    $"{GridAttributeName}[{layer.ID},{0},{0},{layer.HorizontalGridCount},{layer.VerticalGridCount}]",
                    IAspect.FromArray(data, makeCopy: true),
                    "Set Grid Attribute",
                    0, UndoActionJoinMode.Both);
            }
        }

        private List<IObstacle> GetShapeColliders()
        {
            List<IObstacle> shapeColliders = new();
            foreach (var obj in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var colliders = obj.GetComponentsInChildren<Shape2DCollider>(true);
                shapeColliders.AddRange(colliders);
            }
            return shapeColliders;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(writer, label, converter);

            writer.WriteInt32(m_EditorVersion, "EditorAttributeSystem.Version");

            writer.WriteString(m_Name, "Name");
            writer.WriteObjectID(m_ActiveLayerID, "Active Layer ID", converter);
            writer.WriteByte((byte)m_ObstacleMode, "ObstacleCalculationMoide");

            List<LayerBase> layers = new(m_Layers);
            writer.WriteList(layers, "Layers", (LayerBase layer, int index) =>
            {
                writer.WriteSerializable(layer, $"Layer {index}", converter, gameData: false);
            });
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            base.EditorDeserialize(reader, label);

            var version = reader.ReadInt32("EditorAttributeSystem.Version");

            m_Name = reader.ReadString("Name");
            m_ActiveLayerID = reader.ReadInt32("Active Layer ID");
            if (version >= 2)
            {
                m_ObstacleMode = (ObstacleCalculationMode)reader.ReadByte("ObstacleCalculationMoide");
            }

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
        [SerializeField]
        private ObstacleCalculationMode m_ObstacleMode = ObstacleCalculationMode.Disable;
        private AttributeSystemRenderer m_Renderer;
        private const int m_EditorVersion = 2;
    }
}

