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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XDay.NavigationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.House.Editor
{
    internal class House : WorldObject, IGridData
    {
        public override bool IsActive => m_Enabled;
        public override string TypeName => "EditorHouse";
        public IEditorResourceDescriptor ResourceDescriptor => m_ResourceDescriptor.ToObject<IEditorResourceDescriptor>();
        public GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(ResourceDescriptor.GetPath(0));
        public GameObject Root => m_Root;
        public GameObject InteractivePointRoot => m_InteractivePointRoot;
        public GameObject TeleporterRoot => m_TeleporterRoot;
        public List<HouseInteractivePoint> InteractivePoints => m_InteractivePoints;
        public List<HouseTeleporter> Teleporters => m_Teleporters;
        public int SelectedInteractivePointIndex { get => m_SelectedInteractivePointIndex; set => m_SelectedInteractivePointIndex = value; }
        public int SelectedTeleporterIndex { get => m_SelectedTeleporterIndex; set => m_SelectedTeleporterIndex = value; }
        public Bounds LocalBounds
        {
            get
            {
                var min = m_Collider.center - m_Collider.size * 0.5f;
                var max = m_Collider.center + m_Collider.size * 0.5f;
                var bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return bounds;
            }
        }
        public Bounds WorldBounds => m_Collider.bounds;
        public override bool AllowUndo => false;
        public string Name 
        {
            get => m_Name;
            set
            {
                m_Name = value;
                if (m_Root != null)
                {
                    m_Root.name = value;
                }
            }
        }
        public float WorldHeight => m_Collider.bounds.min.y + m_GridHeight;
        public float GridHeight => m_GridHeight;
        public float GridSize => m_GridSize;
        public Quaternion RotationInverse => Quaternion.Inverse(Rotation);
        public bool IsGridActive => m_Grid.IsLineActive;
        public bool IsWalkableLayerActive => m_ShowWalkableLayer;
        public HouseEditor HouseEditor => m_HouseEditor;
        public int HorizontalGridCount => m_Grid.HorizontalGridCount;
        public int VerticalGridCount => m_Grid.VerticalGridCount;
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public override Vector3 Position
        {
            get => m_Position;
            set
            {
                m_Position = value;
                if (m_Root != null)
                {
                    m_Root.transform.position = value;
                }
            }
        }
        public override Quaternion Rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                if (m_Root != null)
                {
                    m_Root.transform.rotation = value;
                }
            }
        }

        public House()
        {
        }

        public House(int id, int index, string name, float gridSize, IResourceDescriptor descriptor)
            : base(id, index)
        {
            m_Name = name;
            m_GridSize = gridSize;
            m_ResourceDescriptor = new WorldObjectWeakRef(descriptor);
        }

        protected override void OnInit()
        {
            SubscribeEvents();

            m_ResourceDescriptor.Init(World);

            m_HouseEditor = World.QueryPlugin<HouseEditor>();
            var obj = World.AssetLoader.LoadGameObject(ResourceDescriptor.GetPath(0));
            Helper.HideGameObject(obj);
            m_Root = new GameObject(m_Name);
            m_DrawBounds = m_Root.AddComponent<DrawBounds>();
            AddBehaviour();
            obj.transform.SetParent(m_Root.transform, false);
            m_Root.transform.SetParent(GetItemRoot().transform);
            m_Root.transform.SetPositionAndRotation(m_Position, m_Rotation);
            m_Root.SetActive(IsActive);
            m_AgentRoot = CreateItemRoot("机器人");
            m_InteractivePointRoot = CreateItemRoot("交互点");
            m_TeleporterRoot = CreateItemRoot("传送点");
            m_Collider = m_Root.transform.GetComponentInChildren<UnityEngine.BoxCollider>();
            Debug.Assert(m_Collider != null, $"房间{obj.name}没有BoxCollider");
            if (m_Collider == null)
            {
                m_Collider = m_Root.AddComponent<UnityEngine.BoxCollider>();
            }
            Physics.SyncTransforms();
            var horizontalGridCount = Mathf.CeilToInt(m_Collider.size.x / m_GridSize);
            var verticalGridCount = Mathf.CeilToInt(m_Collider.size.z / m_GridSize);
            m_Grid = new HouseGrid("House Grid", horizontalGridCount, verticalGridCount, m_GridSize, m_Root.transform, m_GridHeight + m_LayerOffset, GetOffset())
            {
                IsLineActive = m_ShowGrid
            };
            Selection.activeGameObject = m_Root;

            if (m_Layers.Count == 0)
            {
                m_Layers = CreateLayers(horizontalGridCount, verticalGridCount, m_GridSize);
            }

            var offset = GetOffset();
            foreach (var layer in m_Layers)
            {
                layer.Initialize(m_Root.transform, new Vector3(offset.x, offset.y + m_GridHeight, offset.z));
            }
            GetLayer<HouseWalkableLayer>().IsActive = m_ShowWalkableLayer;

            foreach (var agent in m_Agents)
            {
                agent.Initialize(m_AgentRoot.transform, this);
            }

            foreach(var point in m_InteractivePoints)
            {
                point.Initialize(this);
            }

            foreach (var teleporter in m_Teleporters)
            {
                teleporter.Initialize(this);
            }

            if (m_SelectedInteractivePointIndex < 0 && m_InteractivePoints.Count > 0)
            {
                m_SelectedInteractivePointIndex = 0;
            }

            if (m_SelectedTeleporterIndex < 0 && m_Teleporters.Count > 0)
            {
                m_SelectedTeleporterIndex = 0;
            }
        }

        protected override void OnUninit()
        {
            foreach (var layer in m_Layers)
            {
                layer.OnDestroy();
            }
            m_Layers.Clear();

            foreach (var point in m_Teleporters)
            {
                point.OnDestroy();
            }
            m_Teleporters.Clear();

            foreach (var point in m_InteractivePoints)
            {
                point.OnDestroy();
            }
            m_InteractivePoints.Clear();

            m_Grid.OnDestroy();
            Helper.DestroyUnityObject(m_Root);
            m_ResourceDescriptor = null;

            UnsubscribeEvents();
        }

        //将房间内物体旋转180°
        public void RotateObjects()
        {
            var rotateCenter = WorldBounds.center;
            foreach (var teleporter in m_Teleporters)
            {
                teleporter.RotateAround(rotateCenter);
            }

            foreach (var point in m_InteractivePoints)
            {
                point.RotateAround(rotateCenter);
            }

            foreach (var layer in m_Layers)
            {
                layer.Rotate();
            }
        }

        public void AddToDestroyQueue(HouseObject obj)
        {
            if (obj != null)
            {
                if (!m_DestroyQueue.Contains(obj))
                {
                    m_DestroyQueue.Add(obj);
                }
            }
        }

        public void UpdateDestroyQueue()
        {
            foreach (var obj in m_DestroyQueue)
            {
                if (obj is HouseAgentInstance)
                {
                    RemoveAgentInstance(obj as HouseAgentInstance);
                }
                //else if (obj is BuildingInstance)
                //{
                //    RemoveBuildingInstance(obj as BuildingInstance);
                //}
                else
                {
                    Debug.Assert(false, $"todo {obj.GetType()}");
                }
            }
            m_DestroyQueue.Clear();
        }

        public void SetActive(bool active)
        {
            m_Enabled = active;
            if (m_Root != null)
            {
                m_Root.SetActive(active);
            }
        }

        public Vector3 CoordinateToGridCenterPosition(int x, int y)
        {
            var localX = m_GridSize * (x + 0.5f);
            var localZ = m_GridSize * (y + 0.5f);
            return m_Collider.bounds.min + Rotation * new Vector3(localX, 0, localZ);
        }

        public Vector3 CoordinateToGridPosition(int x, int y)
        {
            var localX = m_GridSize * x;
            var localZ = m_GridSize * y;
            return m_Collider.bounds.min + Rotation * new Vector3(localX, 0, localZ);
        }

        public Vector2Int PositionToCoordinate(Vector3 worldPos)
        {
            var localPosition = RotationInverse * (worldPos - m_Collider.bounds.min);

            var xCoord = Mathf.FloorToInt(localPosition.x / m_GridSize);
            var yCoord = Mathf.FloorToInt(localPosition.z / m_GridSize);
            return new Vector2Int(xCoord, yCoord);
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < m_Grid.HorizontalGridCount &&
                y >= 0 && y < m_Grid.VerticalGridCount;
        }

        public Vector3 WorldToLocalPosition(Vector3 worldPos)
        {
            return RotationInverse * (worldPos - m_Collider.bounds.min);
        }

        public Vector2Int LocalPositionToCoordinate(float x, float z)
        {
            var xCoord = Mathf.FloorToInt((x + m_GridSize * 0.05f) / m_GridSize);
            var yCoord = Mathf.FloorToInt((z + m_GridSize * 0.05f) / m_GridSize);
            return new Vector2Int(xCoord, yCoord);
        }

        public Vector3 CalculateObjectCenterPosition(float posX, float posZ, Vector2Int size, out bool isValidCoord)
        {
            var coord = PositionToCoordinate(new Vector3(posX, 0, posZ));
            isValidCoord = IsValidCoordinate(coord.x, coord.y);
            CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), size, out var min, out var max);
            return (CoordinateToGridPosition(min.x, min.y) + CoordinateToGridPosition(max.x + 1, max.y + 1)) * 0.5f;
        }

        public void CalculateCoordinateBoundsByLowerLeftPosition(Vector3 cursorPosition, Vector2Int size, out Vector2Int min, out Vector2Int max)
        {
            var localPosition = WorldToLocalPosition(cursorPosition);

            var minX = localPosition.x - size.x * GridSize * 0.5f;
            var minZ = localPosition.z - size.y * GridSize * 0.5f;
            min = LocalPositionToCoordinate(minX, minZ);
            min.x = Mathf.Clamp(min.x, 0, m_Grid.HorizontalGridCount - size.x);
            min.y = Mathf.Clamp(min.y, 0, m_Grid.VerticalGridCount - size.y);
            max = min + size - Vector2Int.one;
        }

        public bool CalculateCoordinateBoundsByCenterPosition(Vector3 cursorPosition, Vector2Int size, out Vector2Int min, out Vector2Int max)
        {
            var coord = PositionToCoordinate(cursorPosition);
            min = new Vector2Int(
                coord.x - (size.x - 1) / 2,
                coord.y - (size.y - 1) / 2
                );
            max = min + size - Vector2Int.one;
            if (min.x < 0 || min.y < 0 || max.x >= m_Grid.HorizontalGridCount || max.y >= m_Grid.VerticalGridCount)
            {
                return false;
            }

            min.x = Mathf.Clamp(min.x, 0, m_Grid.HorizontalGridCount - size.x);
            min.y = Mathf.Clamp(min.y, 0, m_Grid.VerticalGridCount - size.y);
            max = min + size - Vector2Int.one;

            return true;
        }

        public void SetWalkableState(int x, int y, int width, int height, bool walkable)
        {
            var layer = GetLayer<HouseWalkableLayer>();
            layer.SetWalkable(x, y, width, height, walkable);
            layer.UpdateColors();
        }

        public int GetObjectID(int x, int y)
        {
            return GetLayer<HouseObjectLayer>().GetObjectID(x, y);
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= m_Grid.HorizontalGridCount || y < 0 || y >= m_Grid.VerticalGridCount)
            {
                return false;
            }
            return GetLayer<HouseWalkableLayer>().IsWalkable(x, y);
        }

        public bool IsWalkableNoCheck(int x, int y)
        {
            return IsWalkable(x, y);
        }

        public Vector2Int GetNeighbourCoordinate(int x, int y, int index)
        {
            return new Vector2Int(m_NeighbourCoordinateOffsets[index].x + x, m_NeighbourCoordinateOffsets[index].y + y);
        }

        public Vector2Int FindNearestWalkableCoordinate(Vector2Int coord)
        {
            Debug.LogWarning("todo");
            return coord;
        }

        public void Occupy(int minX, int minY, int maxX, int maxY, int objectID)
        {
            Debug.Assert(objectID != 0);
            GetLayer<HouseOccupyStateLayer>().SetEmpty(minX, minY, maxX - minX + 1, maxY - minY + 1, empty: false);
            GetLayer<HouseObjectLayer>().SetObjectID(minX, minY, maxX - minX + 1, maxY - minY + 1, objectID);
        }

        public void Free(int minX, int minY, int maxX, int maxY)
        {
            GetLayer<HouseOccupyStateLayer>().SetEmpty(minX, minY, maxX - minX + 1, maxY - minY + 1, empty: true);
            GetLayer<HouseObjectLayer>().SetObjectID(minX, minY, maxX - minX + 1, maxY - minY + 1, 0);
        }

        public bool CanPlaceGridObjectAt(float posX, float posZ, Vector2Int objectSize)
        {
            CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), objectSize, out var min, out var max);

            return CanPlaceGridObjectAt(min, max, Vector2Int.one, Vector2Int.zero);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="excludedMin">判断时忽略范围的min</param>
        /// <param name="excludedMax">判断时忽略范围的max</param>
        /// <returns></returns>
        public bool CanPlaceGridObjectAt(Vector2Int min, Vector2Int max, Vector2Int excludedMin, Vector2Int excludedMax)
        {
            for (var y = min.y; y <= max.y; ++y)
            {
                for (var x = min.x; x <= max.x; ++x)
                {
                    if (x >= excludedMin.x && x <= excludedMax.x &&
                        y >= excludedMin.y && y <= excludedMax.y)
                    {
                        continue;
                    }
                    var ok = GetLayer<HouseOccupyStateLayer>().IsEmpty(x, y, 1, 1);
                    if (!ok)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<T> GetLayers<T>() where T : HouseBaseLayer
        {
            var layers = new List<T>();
            foreach (var layer in m_Layers)
            {
                if (layer.GetType() == typeof(T))
                {
                    layers.Add(layer as T);
                }
            }
            return layers;
        }

        public T GetLayer<T>(int layerID) where T : HouseBaseLayer
        {
            foreach (var layer in m_Layers)
            {
                if (layer.ID == layerID && layer.GetType() == typeof(T))
                {
                    return layer as T;
                }
            }
            return null;
        }

        public T GetLayer<T>() where T : HouseBaseLayer
        {
            foreach (var layer in m_Layers)
            {
                if (layer.GetType() == typeof(T))
                {
                    return layer as T;
                }
            }
            return null;
        }

        public void HideLayers()
        {
            foreach (var layer in m_Layers)
            {
                layer.IsActive = false;
            }
        }

        public void SetLayerVisibility<T>(bool visible) where T : HouseBaseLayer
        {
            GetLayer<T>().IsActive = visible;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            if (m_Root != null)
            {
                m_Position = m_Root.transform.position;
                m_Rotation = m_Root.transform.rotation;
            }

            serializer.WriteInt32(m_Version, "House.Version");

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteObjectID(m_ResourceDescriptor.ObjectID, "Resource Descriptor", converter);
            serializer.WriteSingle(m_GridSize, "Grid Size");
            serializer.WriteSingle(m_GridHeight, "Grid Height");
            serializer.WriteBoolean(m_ShowGrid, "Show Grid");
            serializer.WriteBoolean(m_ShowWalkableLayer, "Show Walkable Layer");
            serializer.WriteVector3(m_Position, "Position");
            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            serializer.WriteInt32(m_SelectedTeleporterIndex, "Selected Teleporter Index");
            serializer.WriteInt32(m_SelectedInteractivePointIndex, "Selected Interactive Point Index");

            serializer.WriteList(m_Layers, "Layers", (HouseBaseLayer layer, int index) =>
            {
                serializer.WriteSerializable(layer, $"Layer {index}", converter, false);
            });

            serializer.WriteList(m_InteractivePoints, "Interactive Points", (point, index) =>
            {
                serializer.WriteStructure($"Interactive Point {index}", () =>
                {
                    point.Save(serializer, converter);
                });
            });

            serializer.WriteList(m_Teleporters, "Teleporters", (point, index) =>
            {
                serializer.WriteStructure($"Teleporter {index}", () =>
                {
                    point.Save(serializer, converter);
                });
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            var version = deserializer.ReadInt32("House.Version");

            base.EditorDeserialize(deserializer, label);

            m_ResourceDescriptor = new WorldObjectWeakRef(deserializer.ReadInt32("Resource Descriptor"));
            m_GridSize = deserializer.ReadSingle("Grid Size");
            m_GridHeight = deserializer.ReadSingle("Grid Height");
            m_ShowGrid = deserializer.ReadBoolean("Show Grid");
            m_ShowWalkableLayer = deserializer.ReadBoolean("Show Walkable Layer");
            if (version == 1)
            {
                deserializer.ReadBounds("Bounds");
            }
            m_Position = deserializer.ReadVector3("Position");
            m_Rotation = deserializer.ReadQuaternion("Rotation", Quaternion.identity);
            m_Name = deserializer.ReadString("Name");
            m_ShowInInspector = deserializer.ReadBoolean("Show In Inspector");
            m_SelectedTeleporterIndex = deserializer.ReadInt32("Selected Teleporter Index");
            m_SelectedInteractivePointIndex = deserializer.ReadInt32("Selected Interactive Point Index");

            m_Layers = deserializer.ReadList("Layers", (int index) =>
            {
                return deserializer.ReadSerializable<HouseBaseLayer>($"Layer {index}", false);
            });

            m_InteractivePoints = deserializer.ReadList("Interactive Points", (int index) =>
            {
                var point = new HouseInteractivePoint();
                deserializer.ReadStructure($"Interactive Point {index}", () =>
                {
                    point.Load(deserializer);
                });
                return point;
            });

            m_Teleporters = deserializer.ReadList("Teleporters", (int index) =>
            {
                var teleporter = new HouseTeleporter();
                deserializer.ReadStructure($"Teleporter {index}", () =>
                {
                    teleporter.Load(deserializer);
                });
                return teleporter;
            });
        }

        public override bool SetAspect(int objectID, string name, IAspect property)
        {
            if (!base.SetAspect(objectID, name, property))
            {
                if (name == "Grid Height")
                {
                    m_GridHeight = property.GetSingle();
                    m_Grid.SetHeight(m_GridHeight + m_LayerOffset);
                    foreach (var layer in m_Layers)
                    {
                        layer.SetHeight(m_GridHeight, GetOffset());
                    }
                }

                if (name == "Show Grid")
                {
                    m_ShowGrid = property.GetBoolean();
                    m_Grid.IsLineActive = m_ShowGrid;
                }

                if (name == "Show Walkable Layer")
                {
                    m_ShowWalkableLayer = property.GetBoolean();
                    GetLayer<HouseWalkableLayer>().IsActive = m_ShowWalkableLayer;
                }
            }
            return true;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var prop = base.GetAspect(objectID, name);
            if (prop != null)
            {
                return prop;
            }

            if (name == "Grid Height")
            {
                return IAspect.FromSingle(m_GridHeight);
            }

            if (name == "Show Grid")
            {
                return IAspect.FromBoolean(m_ShowGrid);
            }

            if (name == "Show Walkable Layer")
            {
                return IAspect.FromBoolean(m_ShowWalkableLayer);
            }

            return null;
        }

        private List<HouseBaseLayer> CreateLayers(int horizontalGridCount, int verticalGridCount, float gridSize)
        {
            var layers = new List<HouseBaseLayer>();

            var objectLayer = new HouseObjectLayer(World.AllocateObjectID(), "Object Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(objectLayer);

            var occupyStateLayer = new HouseOccupyStateLayer(World.AllocateObjectID(), "Occupy State Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(occupyStateLayer);

            var walkableLayer = new HouseWalkableLayer(World.AllocateObjectID(), "Walkable Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(walkableLayer);

            return layers;
        }

        public bool PlaceAgentInstance(int objectID, Vector3 worldPos, HouseAgentTemplate template)
        {
            if (!CanPlaceGridObjectAt(worldPos.x, worldPos.z, template.Size))
            {
                return false;
            }

            var isInRange = CalculateCoordinateBoundsByCenterPosition(worldPos, template.Size, out var min, out var max);
            if (!isInRange)
            {
                return false;
            }

            var agentInstance = new HouseAgentInstance(objectID, template.Size, min, max, Rotation, template.ID);
            agentInstance.Initialize(m_AgentRoot.transform, this);
            m_Agents.Add(agentInstance);

            return true;
        }

        private void RemoveAgentInstance(HouseAgentInstance agent)
        {
            agent.OnDestroy();
            m_Agents.Remove(agent);
        }

        public HouseAgentInstance GetAgentInstance(int id)
        {
            foreach (var agent in m_Agents)
            {
                if (agent.ID == id)
                {
                    return agent;
                }
            }
            return null;
        }

        public void CreateNavigationData()
        {
            var taskSystem = WorldEditor.WorldManager.TaskSystem;
            var pathFinder = IGridBasedPathFinder.Create(taskSystem, this, m_NeighbourCoordinateOffsets.Length);
            foreach (var agent in m_Agents)
            {
                agent.SetNavigationData(pathFinder);
            }
        }

        private GameObject CreateItemRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(m_Root.transform, worldPositionStays: false);
            root.AddComponent<NoKeyDeletion>();
            return root;
        }

        public void SetTeleporterState(int id, bool enable)
        {
        }

        public bool GetTeleporterState(int id)
        {
            return true;
        }

        public HouseTeleporter GetTeleporter(int id)
        {
            foreach (var teleporter in m_Teleporters)
            {
                if (teleporter.ID == id)
                {
                    return teleporter;
                }
            }
            return null;
        }

        public void AddTeleporter(HouseTeleporter teleporter)
        {
            teleporter.Initialize(this);

            m_Teleporters.Add(teleporter);

            m_SelectedTeleporterIndex = m_Teleporters.Count - 1;
        }

        public void RemoveTeleporter(int index)
        {
            if (index >= 0 && index < m_Teleporters.Count)
            {
                m_Teleporters[index].OnDestroy();

                m_Teleporters.RemoveAt(index);

                --m_SelectedTeleporterIndex;
                if (m_Teleporters.Count > 0 && m_SelectedTeleporterIndex < 0)
                {
                    m_SelectedTeleporterIndex = 0;
                }
            }
        }

        public virtual bool IsTeleporter(int x, int y)
        {
            return false;
        }

        public virtual Vector2Int GetConnectedTeleporterCoordinate(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        public float GetGridCost(int x, int y, Func<int, float> costOverride = null)
        {
            return 1.0f;
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, Vector2Int referencePoint, int searchDistance)
        {
            for (var distance = 1; distance <= searchDistance; ++distance)
            {
                var minX = x - distance;
                var maxX = x + distance;
                var minY = y - distance;
                var maxY = y + distance;
                for (var yy = minY; yy <= maxY; ++yy)
                {
                    for (var xx = minX; xx <= maxX; ++xx)
                    {
                        var dx = Mathf.Abs(xx - x);
                        var dy = Mathf.Abs(yy - y);
                        if (dx != distance && dy != distance)
                        {
                            continue;
                        }

                        if (IsWalkable(xx, yy))
                        {
                            return new Vector2Int(xx, yy);
                        }
                    }
                }
            }

            Debug.LogWarning($"FindNearestWalkableCoordinate failed! {x}_{y}");
            return new Vector2Int(x, y);
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, int searchDistance)
        {
            for (var distance = 1; distance <= searchDistance; ++distance)
            {
                var minX = x - distance;
                var maxX = x + distance;
                var minY = y - distance;
                var maxY = y + distance;
                for (var yy = minY; yy <= maxY; ++yy)
                {
                    for (var xx = minX; xx <= maxX; ++xx)
                    {
                        var dx = Mathf.Abs(xx - x);
                        var dy = Mathf.Abs(yy - y);
                        if (dx != distance && dy != distance)
                        {
                            continue;
                        }

                        if (IsWalkable(xx, yy))
                        {
                            return new Vector2Int(xx, yy);
                        }
                    }
                }
            }

            Debug.LogWarning($"FindNearestWalkableCoordinate failed! {x}_{y}");
            return new Vector2Int(x, y);
        }

        private void SubscribeEvents()
        {
            WorldEditor.EventSystem.Register(this, (HousePositionChangeEvent e) =>
            {
                if (m_ID == e.HouseID)
                {
                    OnPositionChanged();
                }
            });
            WorldEditor.EventSystem.Register(this, (HouseInstancePositionChangeEvent e) =>
            {
                if (m_ID == e.HouseID)
                {
                    OnPositionChanged();
                }
            });
            WorldEditor.EventSystem.Register(this, (AgentPositionChangeEvent e) =>
            {
                if (m_ID == e.HouseID)
                {
                    var agent = GetAgentInstance(e.AgentID);
                    agent.OnPositionChanged();
                }
            });
            WorldEditor.EventSystem.Register(this, (TeleporterPositionChangeEvent e) =>
            {
                if (m_ID == e.HouseID)
                {
                    var teleporter = e.Teleporter as HouseTeleporter;
                    teleporter.OnPositionChanged();
                }
            });
            WorldEditor.EventSystem.Register(this, (UpdateAgentMovementEvent e) =>
            {
                if (m_ID == e.HouseID)
                {
                    var agent = GetAgentInstance(e.AgentID);
                    agent.UpdateMovement(changePosition: true, forceUpdate: false, true);
                }
            });
            WorldEditor.EventSystem.Register(this, (SetAgentDestinationEvent e) =>
            {
                if (m_ID == e.HouseID)
                {
                    var agent = GetAgentInstance(e.AgentID);
                    if (!agent.CanMove())
                    {
                        CreateNavigationData();
                    }
                    var targetPosition = m_HouseEditor.HouseRaycastHit(e.GuiScreenPoint, out var targetHouse);
                    if (targetHouse != null)
                    {
                        var startHouse = m_HouseEditor.LocateHouseInstance(agent.Position);
                        if (startHouse == targetHouse)
                        {
                            agent.MoveInsideHouse(e.GuiScreenPoint, startHouse);
                        }
                        else
                        {
                            List<Vector3> path = new();
                            var foundPath = m_HouseEditor.FindPath(agent.Position, targetPosition, targetHouse, path);
                            if (foundPath)
                            {
                                agent.MoveAlongPath(path);
                            }
                        }
                    }
                }
            });
        }

        private void OnPositionChanged()
        {
            m_Position = m_Root.transform.position;
        }

        private void UnsubscribeEvents()
        {
            WorldEditor.EventSystem.Unregister(this);
        }

        internal void Update(float dt)
        {
            UpdateDestroyQueue();

            foreach (var agent in m_Agents)
            {
                agent.Update(dt);
            }

            m_DrawBounds.Bounds = m_Collider.bounds;
        }

        protected virtual Transform GetItemRoot()
        {
            return m_HouseEditor.HouseTemplateRoot.transform;
        }

        protected virtual void AddBehaviour()
        {
            var behaviour = m_Root.AddComponent<HouseBehaviour>();
            behaviour.Initialize(ID, 
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });
        }

        public HouseInteractivePoint GetInteractivePoint(int id)
        {
            foreach (var point in m_InteractivePoints)
            {
                if (point.ID == id)
                {
                    return point;
                }
            }
            return null;
        }

        public bool ContainsPoint(Vector3 point)
        {
            var worldBounds = LocalBounds.Transform(Root.transform);
            return worldBounds.Contains(point);
        }

        public void AddInteractivePoint(HouseInteractivePoint point)
        {
            point.Initialize(this);

            m_InteractivePoints.Add(point);
            m_SelectedInteractivePointIndex = m_InteractivePoints.Count - 1;
        }

        public void RemoveInteractivePoint(int index)
        {
            if (index >= 0 && index < m_InteractivePoints.Count)
            {
                m_InteractivePoints[index].OnDestroy();
                m_InteractivePoints.RemoveAt(index);
                --m_SelectedInteractivePointIndex;
                if (m_InteractivePoints.Count > 0 && m_SelectedInteractivePointIndex < 0)
                {
                    m_SelectedInteractivePointIndex = 0;
                }
            }
        }

        public void SetGridHeight(float gridHeight)
        {
            UndoSystem.SetAspect(this, "Grid Height", IAspect.FromSingle(gridHeight), "Set House Grid Height", 0, UndoActionJoinMode.None);
        }

        public void CopyTo(HouseInstance instance)
        {
            CopyTeleporters(instance);
            CopyInteractivePoints(instance);
            instance.SetGridHeight(m_GridHeight);
            var sourceLayer = GetLayer<HouseWalkableLayer>();
            var destLayer = instance.GetLayer<HouseWalkableLayer>();
            sourceLayer.CopyTo(destLayer);
        }

        private void CopyTeleporters(HouseInstance houseInstance)
        {
            List<HouseTeleporterInstance> destroyedTeleporterInstances = new();
            foreach (var teleporterInstance in houseInstance.TeleporterInstances)
            {
                var teleporter = GetTeleporter(teleporterInstance.TeleporterID);
                if (teleporter != null)
                {
                    teleporterInstance.CopyFrom(teleporter);
                }
                else
                {
                    destroyedTeleporterInstances.Add(teleporterInstance);
                }
            }
            foreach (var instance in destroyedTeleporterInstances)
            {
                houseInstance.RemoveTeleporterInstance(instance);
            }

            foreach (var teleporter in m_Teleporters)
            {
                var teleporterInstance = houseInstance.GetTeleporterInstance(teleporter.ID);
                if (teleporterInstance == null)
                {
                    teleporterInstance = new HouseTeleporterInstance(World.AllocateObjectID(), teleporter);
                    houseInstance.AddTeleporterInstance(teleporterInstance);
                    teleporterInstance.CopyFrom(teleporter);
                }
            }
        }

        private void CopyInteractivePoints(HouseInstance houseInstance)
        {
            List<HouseInteractivePointInstance> destroyedInstances = new();
            foreach (var instance in houseInstance.InteractivePointInstance)
            {
                var point = GetInteractivePoint(instance.InteractivePointID);
                if (point != null)
                {
                    instance.CopyFrom(point);
                }
                else
                {
                    destroyedInstances.Add(instance);
                }
            }
            foreach (var instance in destroyedInstances)
            {
                houseInstance.RemoveInteractivePointInstance(instance);
            }

            foreach (var point in m_InteractivePoints)
            {
                var instance = houseInstance.GetInteractivePointInstance(point.ID);
                if (instance == null)
                {
                    instance = new HouseInteractivePointInstance(World.AllocateObjectID(), point);
                    houseInstance.AddInteractivePointInstance(instance);
                    instance.CopyFrom(point);
                }
            }
        }

        private Vector3 GetOffset()
        {
            var bounds = Helper.CalculateBoxColliderWorldBounds(m_Collider);
            var offset = bounds.min - m_Position;
            return offset;
        }

        [SerializeField]
        private WorldObjectWeakRef m_ResourceDescriptor;
        [SerializeField]
        private float m_GridSize;
        [SerializeField]
        private bool m_ShowGrid = true;
        [SerializeField]
        private bool m_ShowWalkableLayer = true;
        [SerializeField]
        private float m_GridHeight = 0;
        [SerializeField]
        private Vector3 m_Position;
        [SerializeField]
        private Quaternion m_Rotation = Quaternion.identity;
        [SerializeField]
        private List<HouseAgentInstance> m_Agents = new();
        [SerializeField]
        private List<HouseBaseLayer> m_Layers;
        [SerializeField]
        private bool m_ShowInInspector = true;
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private List<HouseInteractivePoint> m_InteractivePoints = new();
        [SerializeField]
        private List<HouseTeleporter> m_Teleporters = new();
        private bool m_Enabled = true;
        private HouseGrid m_Grid;
        private GameObject m_Root;
        private GameObject m_AgentRoot;
        private GameObject m_InteractivePointRoot;
        private GameObject m_TeleporterRoot;
        private const int m_Version = 2;
        private const float m_LayerOffset = 0.01f;
        protected HouseEditor m_HouseEditor;
        [SerializeField]
        private int m_SelectedInteractivePointIndex = -1;
        [SerializeField]
        private int m_SelectedTeleporterIndex = -1;
        private readonly List<HouseObject> m_DestroyQueue = new();
        private DrawBounds m_DrawBounds;
        private UnityEngine.BoxCollider m_Collider;
        private readonly Vector2Int[] m_NeighbourCoordinateOffsets = new Vector2Int[]
                {
                    new(-1, 0),
                    new(1, 0),
                    new(0, -1),
                    new(0, 1),

                    new(1, 1),
                    new(-1, -1),
                    new(-1, 1),
                    new(1, -1),
                };
    }
}