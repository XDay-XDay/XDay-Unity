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
using UnityEditor;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;
using XDay.SerializationAPI;
using System.Text;

namespace XDay.WorldAPI.Tile.Editor
{
    public struct TileSystemCreateInfo
    {
        public float TileWidth;
        public float TileHeight;
        public int HorizontalTileCount;
        public int VerticalTileCount;
        public Vector2 Origin;
        public float Rotation;
        public string Name;
        public int ObjectIndex;
        public int ID;
    }

    [WorldPluginMetadata("TileSystem", "tile_editor_data", typeof(TileSystemCreateWindow), true)]
    internal sealed partial class TileSystem : EditorWorldPlugin
    {
        public override string Name { set => throw new System.NotImplementedException(); get => m_Name; }
        public Vector2 Origin => m_Origin;
        public int XTileCount => m_XTileCount;
        public int YTileCount => m_YTileCount;
        public override GameObject Root => m_Renderer?.Root;
        public override IPluginLODSystem LODSystem => m_PluginLODSystem;
        public float TileWidth => m_TileWidth;
        public float TileHeight => m_TileHeight;
        public Vector3 Center => new(m_Origin.x + m_Width * 0.5f, 0, m_Origin.y + m_Height * 0.5f);
        public int LODCount => m_PluginLODSystem.LODCount;
        public float Width => m_Width;
        public float Height => m_Height;
        public override Quaternion Rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                m_Renderer?.SyncRotation();
            }
        }
        public MaterialConfig MaterialConfig => World.QueryObject<MaterialConfig>(m_ActiveMaterialConfigID);
        public override List<string> GameFileNames
        {
            get
            {
                if (m_GameTileType == GameTileType.TerrainLOD)
                {
                    return new() { "terrain_lod_tile" };
                }
                else if (m_GameTileType == GameTileType.FlatTile)
                {
                    return new() { "flat_tile" };
                }
                Debug.Assert(false, "todo");
                return null;
            }
        }
        public TileSystemRenderer Renderer => m_Renderer;
        public override string TypeName => "EditorTileSystem";

        public TileSystem()
        {
        }

        public TileSystem(TileSystemCreateInfo createInfo) 
            : base(createInfo.ID, createInfo.ObjectIndex)
        {
            m_ResourceDescriptorSystem = new EditorResourceDescriptorSystem();
            m_PluginLODSystem = IPluginLODSystem.Create(1);
            m_Origin = createInfo.Origin;
            m_Name = createInfo.Name;
            m_Rotation = Quaternion.Euler(0, createInfo.Rotation, 0);
            m_TileWidth = createInfo.TileWidth;
            m_TileHeight = createInfo.TileHeight;
            m_XTileCount = createInfo.HorizontalTileCount;
            m_YTileCount = createInfo.VerticalTileCount;
            m_Tiles = new TileObject[m_XTileCount * m_YTileCount];
        }

        protected override void InitInternal()
        {
            m_Indicator = IMeshIndicator.Create(World);
            m_ResourceDescriptorSystem.Init(World);
            m_PluginLODSystem.Init(World.WorldLODSystem);
            m_Width = m_XTileCount * m_TileWidth;
            m_Height = m_YTileCount * m_TileHeight;
            m_Renderer = new TileSystemRenderer(this);

            m_ResourceGroupSystem?.Init(resourceDataCreator: null);

            foreach (var config in m_MaterialConfigs)
            {
                config.Init(World);
            }

            foreach (var tile in m_Tiles)
            {
                tile?.Init(World);
            }

            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    var tile = m_Tiles[i * m_XTileCount + j];
                    if (tile != null && 
                        tile.SetVisibility(WorldObjectVisibility.Visible))
                    {
                        m_Renderer.ToggleActiveState(tile, j, i);
                    }
                }
            }

            m_TexturePainter.Init(() => { SceneView.RepaintAll(); }, this);
        }

        protected override void UninitInternal()
        {
            m_TexturePainter.OnDestroy();

            m_Indicator.OnDestroy();
            m_ResourceDescriptorSystem.Uninit();

            foreach (var tile in m_Tiles)
            {
                tile?.Uninit();
            }

            m_Renderer?.OnDestroy();

            foreach (var config in m_MaterialConfigs)
            {
                config.Uninit();
            }
            m_MaterialConfigs.Clear();
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (QueryObjectUndo(objectID) is WorldObject tile)
            {
                aspect = tile.GetAspect(objectID, name);
                if (aspect != null)
                {
                    return aspect;
                }
            }

            return m_Renderer.GetAspect(objectID, name);
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (QueryObjectUndo(objectID) is WorldObject tile)
            {
                if (tile.SetAspect(objectID, name, aspect))
                {
                    m_Renderer.SetAspect(objectID, name, aspect);
                }
            }
            return true;
        }

        public override IWorldObject QueryObjectUndo(int tileID)
        {
            return World.QueryObject<WorldObject>(tileID);
        }

        public override void DestroyObjectUndo(int tileID)
        {
            if (QueryObjectUndo(tileID) is TileObject tile)
            {
                var tileCoord = UnrotatedPositionToCoordinate(tile.Position.x, tile.Position.z);

                if (tile.SetVisibility(WorldObjectVisibility.Invisible))
                {
                    m_Renderer.ToggleActiveState(tile, tileCoord.x, tileCoord.y);
                }

                for (var i = 0; i < m_Tiles.Length; ++i)
                {
                    if (m_Tiles[i] != null && 
                        m_Tiles[i].ID == tileID)
                    {
                        m_Tiles[i].Uninit();
                        m_Tiles[i] = null;
                        break;
                    }
                }
            }
        }

        public override void AddObjectUndo(IWorldObject worldObject, int lod, int objectIndex)
        {
            var tile = worldObject as TileObject;
            var tileCoord = UnrotatedPositionToCoordinate(tile.Position.x, tile.Position.z);
            tile.SetVisibility(WorldObjectVisibility.Visible);
            if (tile.IsActive)
            {
                m_Renderer.ShowTile(tile, tileCoord.x, tileCoord.y);
            }
            var tileIndex = tileCoord.y * m_XTileCount + tileCoord.x;
            Debug.Assert(m_Tiles[tileIndex] == null);
            m_Tiles[tileIndex] = tile;
        }

        public TileObject GetTile(int x, int y)
        {
            if (x >= 0 && x < m_XTileCount &&
                y >= 0 && y < m_YTileCount)
            {
                return m_Tiles[y * m_XTileCount + x];
            }
            return null;
        }

        public Vector2Int UnrotatedPositionToCoordinate(float x, float z)
        {
            return new Vector2Int(Mathf.FloorToInt((x + m_Width * 0.5f) / m_TileWidth), Mathf.FloorToInt((z + m_Height * 0.5f) / m_TileHeight));
        }

        public Vector3 CoordinateToUnrotatedPosition(int x, int y)
        {
            return new Vector3(x * m_TileWidth - m_Width * 0.5f, 0, y * m_TileHeight - m_Height * 0.5f);
        }

        public Vector2Int RotatedPositionToCoordinate(float x, float z)
        {
            var center = Center;
            var unrotatedPos = Quaternion.Inverse(m_Rotation) * new Vector3(x - center.x, 0, z - center.z) + new Vector3(center.x, 0, center.z);
            return new Vector2Int(Mathf.FloorToInt((unrotatedPos.x - m_Origin.x) / m_TileWidth), Mathf.FloorToInt((unrotatedPos.z - m_Origin.y) / m_TileHeight));
        }

        public void TilingUseSelectedGroup()
        {
            if (!CheckTile())
            {
                return;
            }

            var group = m_ResourceGroupSystem.SelectedGroup;

            UndoSystem.NextGroupAndJoin();
            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    var tile = GetTile(j, i);
                    var path = group.GetResourcePath(i * m_XTileCount + j);
                    UndoSystem.DestroyObject(tile, "Destroy Tile", ID);
                    tile = CreateTile(CoordinateToUnrotatedPosition(j, i), path);
                    UndoSystem.CreateObject(tile, World.ID, "Create Tile", ID);
                }
            }
            UndoSystem.NextGroupAndJoin();
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("TileSystem.Version");

            m_TexturePainter = deserializer.ReadSerializable<TexturePainter>("Texture Painter", false);
            m_TexturePainter ??= new TexturePainter();

            m_ResourceGroupSystem = deserializer.ReadSerializable<IResourceGroupSystem>("Resource Group System", false);
            m_PluginLODSystem = deserializer.ReadSerializable<IPluginLODSystem>("Plugin LOD System", false);

            m_ShowTileSetting = deserializer.ReadBoolean("Show Tile Setting");
            m_Rotation = deserializer.ReadQuaternion("Rotation");
            m_TerrainLODMorphRatio = deserializer.ReadSingle("LOD Morph Ratio");
            m_GameTileType = (GameTileType)deserializer.ReadByte("Game Tile Type");
            m_XTileCount = deserializer.ReadInt32("X Tile Count");
            m_YTileCount = deserializer.ReadInt32("Y Tile Count");
            m_Name = deserializer.ReadString("Name");
            m_Origin = deserializer.ReadVector2("Origin");
            m_TileWidth = deserializer.ReadSingle("Tile Width");
            m_TileHeight = deserializer.ReadSingle("Tile Height");

            m_ResourceDescriptorSystem = deserializer.ReadSerializable<EditorResourceDescriptorSystem>("Resource Descriptor System", false);
            m_MaterialConfigs = deserializer.ReadList("Material Settings", (int index) =>
            {
                return deserializer.ReadSerializable<MaterialConfig>($"Material Setting{index}", false);
            });

            m_ActiveMaterialConfigID = deserializer.ReadInt32("Selected Material Setting ID");
            if (m_MaterialConfigs.Count > 0)
            {
                m_ActiveMaterialConfigID = m_MaterialConfigs[0].ID;
            }

            m_Tiles = deserializer.ReadArray("Tiles", (int index) =>
            {
                return deserializer.ReadSerializable<TileObject>($"Tile {index}", false);
            });

            m_ShowMaterialConfig = deserializer.ReadBoolean("Show Material Setting");
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "TileSystem.Version");

            serializer.WriteSerializable(m_TexturePainter, "Texture Painter", converter, false);

            serializer.WriteSerializable(m_ResourceGroupSystem, "Resource Group System", converter, false);

            serializer.WriteSerializable(m_PluginLODSystem, "Plugin LOD System", converter, false);

            serializer.WriteBoolean(m_ShowTileSetting, "Show Tile Setting");

            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteSingle(m_TerrainLODMorphRatio, "LOD Morph Ratio");
            serializer.WriteByte((byte)m_GameTileType, "Game Tile Type");

            serializer.WriteInt32(m_XTileCount, "X Tile Count");
            serializer.WriteInt32(m_YTileCount, "Y Tile Count");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteVector2(m_Origin, "Origin");
            serializer.WriteSingle(m_TileWidth, "Tile Width");
            serializer.WriteSingle(m_TileHeight, "Tile Height");

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, false);

            serializer.WriteList(m_MaterialConfigs, "Material Configs", (MaterialConfig setting, int index) =>
            {
                serializer.WriteSerializable(setting, $"Material Config{index}", converter, false);
            });

            serializer.WriteObjectID(m_ActiveMaterialConfigID, "Selected Material Config ID", converter);

            serializer.WriteArray(m_Tiles, "Tiles", (TileObject tile, int index) =>
            {
                serializer.WriteSerializable(tile, $"Tile {index}", converter, false);
            });

            serializer.WriteBoolean(m_ShowMaterialConfig, "Show Material Config");

            m_TexturePainter.SaveToFile();
        }

        private Vector3 CoordinateToRotatedPosition(int x, int y)
        {
            return m_Rotation * (new Vector3(x * m_TileWidth, 0, y * m_TileHeight) - Center) + Center;
        }

        private void GetTileCoordinatesInRange(Vector3 pos, out int minX, out int minY, out int maxX, out int maxY)
        {
            var coord = RotatedPositionToCoordinate(pos.x, pos.z);
            minX = coord.x - m_Range / 2;
            minY = coord.y - m_Range / 2;
            maxX = coord.x + m_Range / 2;
            maxY = coord.y + m_Range / 2;
        }

        private bool CheckTile()
        {
            var builder = new StringBuilder();
            var group = m_ResourceGroupSystem.SelectedGroup;
            if (group.Count != m_XTileCount * m_YTileCount)
            {
                builder.AppendLine("invalid tile count");
            }

            for (var i = 0; i < m_YTileCount; ++i)
            {
                for (var j = 0; j < m_XTileCount; ++j)
                {
                    var assetPath = group.GetResourcePath(i * m_XTileCount + j);
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
                    {
                        builder.AppendLine($"resouce {assetPath} is null");
                    }
                }
            }

            if (builder.Length != 0)
            {
                EditorUtility.DisplayDialog("Error", builder.ToString(), "OK");
                return false;
            }
            return true;
        }

        private enum GameTileType
        {
            FlatTile,
            TerrainLOD,
        }

        private string m_Name;
        private Vector2 m_Origin;
        private TileObject[] m_Tiles;
        private TileSystemRenderer m_Renderer;
        private float m_Width;
        private float m_Height;
        private Quaternion m_Rotation;
        private GameTileType m_GameTileType = GameTileType.FlatTile;
        private IMeshIndicator m_Indicator;
        private bool m_ShowTileSetting = false;
        private float m_TerrainLODMorphRatio = 0.667f;
        private IPluginLODSystem m_PluginLODSystem;
        private int m_XTileCount;
        private int m_YTileCount;
        private EditorResourceDescriptorSystem m_ResourceDescriptorSystem;
        private int m_ActiveMaterialConfigID = 0;
        private List<MaterialConfig> m_MaterialConfigs = new();
        private float m_TileWidth;
        private float m_TileHeight;
        private bool m_ShowMaterialConfig = true;
    }
}

//XDay
