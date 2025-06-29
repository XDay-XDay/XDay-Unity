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
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Decoration
{
    [Preserve]
    internal partial class DecorationSystem : WorldPlugin, IDecorationSystem
    {
        public DecorationObjectPool DecorationPool => m_DecorationPool;
        public override List<string> GameFileNames => new() { "decoration" };
        public float GridWidth => m_GridWidth;
        public float GridHeight => m_GridHeight;
        public ICameraVisibleAreaUpdater CameraVisibleAreaUpdater => m_VisibleAreaUpdater;
        public int XGridCount => m_XGridCount;
        public int YGridCount => m_YGridCount;
        public override string Name { set => throw new System.NotImplementedException(); get => m_Name; }
        public DecorationSystemRenderer Renderer => m_Renderer;
        public override string TypeName => "DecorationSystem";

        public DecorationSystem()
        {
        }

        public DecorationObject QueryVisibleObject(int gridX, int gridY, int lod, int localIndex)
        {
            var objectIndex = m_GridData[gridY * m_XGridCount + gridX].GetObjectIndex(localIndex, lod);
            m_VisibleObjects.TryGetValue(CalculateObjectID(lod, objectIndex), out var activeObject);
            return activeObject;
        }

        public void QueryDecorationIDsInCircle(Vector3 center, float radius, List<int> decorationIDs)
        {
            GetOverlappedGrids(center.x - radius, center.z - radius, center.x + radius, center.z + radius, m_TempGrids);

            var lodCount = m_LODSystem.LODCount;
            var radiusSqr = radius * radius;
            foreach (var grid in m_TempGrids)
            {
                for (var lod = 0; lod < lodCount; lod++)
                {
                    var objectCount = grid.GetObjectCount(lod);
                    for (var i = 0; i < objectCount; i++)
                    {
                        var objectIndex = grid.GetObjectIndex(i, lod);
                        var position = m_DecorationMetaData.Position[objectIndex];
                        var deltaX = center.x - position.x;
                        var deltaZ = center.z - position.z;
                        if (deltaX * deltaX + deltaZ * deltaZ <= radiusSqr)
                        {
                            var objectID = CalculateObjectID(lod, objectIndex);
                            if (!decorationIDs.Contains(objectID))
                            {
                                decorationIDs.Add(objectID);
                            }
                        }
                    }
                }
            }
        }

        public void PlayAnimation(int decorationID, string animationName, bool alwaysPlay)
        {
            if (m_VisibleObjects.ContainsKey(decorationID))
            {
                m_Renderer.PlayAnimation(decorationID, animationName, alwaysPlay);
            }
        }

        public void ShowDecoration(int decorationID, bool show)
        {
            m_CustomSetEnabled[decorationID] = show;
            var decoration = World.QueryObject<DecorationObject>(decorationID);
            if (decoration != null)
            {
                //if (decoration.SetEnabled(show) ||
                //    VisibleTest(decoration))
                if (decoration.SetEnabled(show))
                {
                    m_ToggleActiveState?.Invoke(decoration);
                }
            }
        }

        public void ShowDecoration(Vector3 circleCenter, float circleRadius, bool show)
        {
            m_TempIDs.Clear();
            QueryDecorationIDsInCircle(circleCenter, circleRadius, m_TempIDs);
            foreach (var objID in m_TempIDs)
            {
                ShowDecoration(objID, show);
            }
            m_TempIDs.Clear();
        }

        public void ShowGrid(GridData grid)
        {
            m_UpdateNeeded = true;
            grid.ActiveStateCounter++;
        }

        public void HideGrid(GridData grid)
        {
            m_UpdateNeeded = true;
            grid.ActiveStateCounter--;
            Debug.Assert(grid.ActiveStateCounter >= 0);
        }

        public GridData GetGrid(int x, int y)
        {
            if (x >= 0 && x < m_XGridCount && y >= 0 && y < m_YGridCount)
            {
                return m_GridData[y * m_XGridCount + x];
            }
            return null;
        }

        public void DestroyDecoration(DecorationObject decoration, int prevLOD, int lod)
        {
            if (!ModelChanged(prevLOD, lod, m_DecorationMetaData.LODResourceChangeMasks[decoration.ObjectIndex]))
            {
                return;
            }

            if (decoration.ReleaseRef())
            {
                decoration.Uninit();
                decoration.SetEnabled(false);
                m_ToggleActiveState?.Invoke(decoration);
                m_VisibleObjects.Remove(decoration.ID);
                m_DecorationPool.Release(decoration);
            }
        }

        public void CreateDecoration(int indexInGrid, int gridX, int gridY, int prevLOD, int curLOD)
        {
            var grid = GetGrid(gridX, gridY);
            var objectIndex = grid.GetObjectIndex(indexInGrid, curLOD);
            if (!ModelChanged(prevLOD, curLOD, m_DecorationMetaData.LODResourceChangeMasks[objectIndex]))
            {
                return;
            }

            var objectID = CalculateObjectID(curLOD, objectIndex);
            if (m_VisibleObjects.TryGetValue(objectID, out var decoration))
            {
                decoration.AddRef();
            }
            else
            {
                var visibleArea = World.CameraVisibleAreaCalculator.ExpandedArea;
                var resourceMetadata = m_ResourceMetadata[m_DecorationMetaData.ResourceMetadataIndex[objectIndex]];

                var position = m_DecorationMetaData.Position[objectIndex];
                
                var boundsMinX = resourceMetadata.Bounds.xMin + position.x;
                var boundsMinZ = resourceMetadata.Bounds.yMin + position.z;

                var invisible = false;
                if (boundsMinZ > visibleArea.yMax ||
                    boundsMinX > visibleArea.xMax ||
                    visibleArea.yMin > (boundsMinZ + resourceMetadata.Bounds.height) ||
                    visibleArea.xMin > (boundsMinX + resourceMetadata.Bounds.width))
                {
                    invisible = true;
                }

                decoration = m_DecorationPool.Get(objectID, IsSetEnabled(objectID), objectIndex, World, grid.X, grid.Y, curLOD, indexInGrid, position.x, position.y, position.z, resourceMetadata, invisible);
                m_VisibleObjects.Add(objectID, decoration);
                m_ToggleActiveState?.Invoke(decoration);
            }
        }

        public Vector3 CoordinateToCenterPosition(int x, int y)
        {
            return new Vector3((x + 0.5f) * m_GridWidth + m_Bounds.min.x, 
                0,
                (y + 0.5f) * m_GridHeight + m_Bounds.min.z);
        }

        protected override void InitInternal()
        {
            foreach (var grid in m_GridData)
            {
                grid.Init(this);
            }

            m_ResourceDescriptorSystem.Init(World);
            foreach (var metadata in m_ResourceMetadata)
            {
                metadata.Init(m_ResourceDescriptorSystem);
            }

            m_VisibleAreaUpdater = new CameraVisibleAreaUpdater(World.CameraVisibleAreaCalculator);

            InitRendererInternal();

            UpdateDecorations(m_Timer).Forget();
        }

        protected override void InitRendererInternal()
        {
            if (m_Renderer != null)
            {
                return;
            }

            m_Renderer = new DecorationSystemRenderer(this);
            m_ToggleActiveState = m_Renderer.ToggleActiveState;
            m_VisibleAreaUpdater.Reset();
        }

        protected override void UninitRendererInternal()
        {
            m_Renderer?.OnDestroy();
            m_Renderer = null;

            ResetFrameTasks();

            DestroyVisibleObjects();
        }

        protected override void UninitInternal()
        {
            m_ResourceDescriptorSystem.Uninit();

            foreach (var kv in m_VisibleObjects)
            {
                kv.Value.Uninit();
            }

            m_Renderer?.OnDestroy();
            m_Renderer = null;
        }

        private async UniTask UpdateDecorations(SimpleStopwatch timer)
        {
            while (true)
            {
                if (!timer.Running)
                {
                    await UniTask.Yield();
                }

                if (m_Renderer == null)
                {
                    break;
                }

                if (m_UpdateNeeded)
                {
                    var lod = m_LODSystem.CurrentLOD;
                    var oldVisibleArea = m_VisibleAreaUpdater.CurrentArea;
                    foreach (var grid in m_GridData)
                    {
                        if (grid.ActiveStateCounter > 0)
                        {
                            var objectCount = grid.GetObjectCount(lod);
                            for (var i = 0; i < objectCount; ++i)
                            {
                                var objectIndex = grid.GetObjectIndex(i, lod);
                                var decoration = World.QueryObject<DecorationObject>(CalculateObjectID(lod, objectIndex));
                                if (decoration != null &&
                                    decoration.IsEnabled() &&
                                    VisibleTest(decoration))
                                {
                                    m_ToggleActiveState?.Invoke(decoration);
                                }

                                if (timer.ElapsedSeconds > GameDefine.MAX_TASK_TIME_SECONDS_PER_FRAME)
                                {
                                    await UniTask.Yield();
                                }
                            }
                        }
                    }
                    if (oldVisibleArea == m_VisibleAreaUpdater.CurrentArea)
                    {
                        m_UpdateNeeded = false;
                    }
                }

                await UniTask.Yield();
            }
        }

        protected override void UpdateInternal(float dt)
        {
            var cameraPos = World.CameraManipulator.RenderPosition;

            var viewportChanged = m_VisibleAreaUpdater.BeginUpdate();
            var lodChanged = m_LODSystem.Update(cameraPos.y);
            if (lodChanged)
            {
                LODChangeInternal(m_VisibleAreaUpdater.PreviousArea, m_VisibleAreaUpdater.CurrentArea);
            }
            else if (viewportChanged)
            {
                m_UpdateNeeded = true;
                VisibleAreaChangeInternal(m_VisibleAreaUpdater.PreviousArea, m_VisibleAreaUpdater.CurrentArea);
            }

            m_VisibleAreaUpdater.EndUpdate();

            if (m_Renderer != null)
            {
                ScheduleTasks();

                m_Renderer.Update();
            }
        }

        private int QueryLODGroup(int lod, int objectIndex)
        {
            var resourceMetadataIndex = m_DecorationMetaData.ResourceMetadataIndex[objectIndex];
            return m_ResourceMetadata[resourceMetadataIndex].QueryLODGroup(lod);
        }

        private bool ModelChanged(int prevLOD, int curLOD, byte changeMasks)
        {
#if true
            //有bug,暂时关闭model changed功能
            return true;
#else
            if (prevLOD == curLOD)
            {
                return true;
            }

            if ((changeMasks & (1 << Mathf.Max(curLOD, prevLOD))) != 0)
            {
                return true;
            }
            return false;
#endif
        }

        private void DestroyVisibleObjects()
        {
            m_ToggleActiveState = null;

            var decorations = new List<DecorationObject>();
            foreach (var kv in m_VisibleObjects)
            {
                decorations.Add(kv.Value);
            }

            foreach (var decoration in decorations)
            {
                DestroyDecoration(decoration, -1, decoration.LOD);
            }
            Debug.Assert(m_VisibleObjects.Count == 0);
            m_VisibleObjects.Clear();
        }

        private void ResetFrameTasks()
        {
            var lodCount = m_LODSystem.LODCount;
            foreach (var grid in m_GridData)
            {
                for (var lod = 0; lod < lodCount; ++lod)
                {
                    var task = grid.GetFrameTask(lod);
                    task.Reset();
                }   
            }
            m_Tasks.Clear();
        }

        private void LODChangeInternal(Rect oldVisibleArea, Rect newVisibleArea)
        {
            var oldBounds = GetBoundsInVisibleArea(oldVisibleArea);
            var oldMin = oldBounds.min;
            var oldMax = oldBounds.max;
            for (var y = oldMin.y; y <= oldMax.y; ++y)
            {
                for (var x = oldMin.x; x <= oldMax.x; ++x)
                {
                    var grid = GetGrid(x, y);
                    if (grid != null)
                    {
                        ShowGridObjects(x, y, false, changeLOD: true);
                    }
                }
            }

            var newBounds = GetBoundsInVisibleArea(newVisibleArea);
            var newMin = newBounds.min;
            var newMax = newBounds.max;
            for (var y = newMin.y; y <= newMax.y; ++y)
            {
                for (var x = newMin.x; x <= newMax.x; ++x)
                {
                    var grid = GetGrid(x, y);
                    if (grid != null)
                    {
                        ShowGridObjects(x, y, true, changeLOD: true);
                    }
                }
            }
        }

        private Vector2Int PositionToCoordinate(float x, float z)
        {
            return new Vector2Int(Mathf.FloorToInt((x - m_Bounds.min.x) / m_GridWidth),
                Mathf.FloorToInt((z - m_Bounds.min.z) / m_GridHeight));
        }

        private void ScheduleTasks()
        {
            m_Tasks.Sort((x, y) => { return x.Order.CompareTo(y.Order); });

            m_Timer.Begin();

            for (var i = m_Tasks.Count - 1; i >= 0; --i)
            {
                var timeup = m_Tasks[i].Run(m_Timer);
                if (m_Tasks[i].End)
                {
                    m_Tasks.RemoveAt(i);
                }
                if (timeup)
                {
                    break;
                }
            }
        }

        private RectInt GetBoundsInVisibleArea(Rect visibleArea)
        {
            var min = PositionToCoordinate(visibleArea.xMin, visibleArea.yMin);
            var max = PositionToCoordinate(visibleArea.xMax, visibleArea.yMax);

            return new RectInt(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private void ShowGridObjects(int x, int y, bool visible, bool changeLOD)
        {
            var prevLOD = changeLOD ? m_LODSystem.PreviousLOD : m_LODSystem.CurrentLOD;
            var grid = GetGrid(x, y);

            if (visible)
            {
                var task = grid.GetFrameTask(m_LODSystem.CurrentLOD);
                if (!m_Tasks.Contains(task))
                {
                    m_Tasks.Add(task);
                }
                task.Init(prevLOD, FrameTaskDecorationToggle.Type.Activate);
            }
            else
            {
                var task = grid.GetFrameTask(prevLOD);
                if (!m_Tasks.Contains(task))
                {
                    m_Tasks.Add(task);
                }
                task.Init(m_LODSystem.CurrentLOD, FrameTaskDecorationToggle.Type.Deactivate);
            }
        }

        private int CalculateObjectID(int lod, int objectIndex)
        {
            var lodGroup = QueryLODGroup(lod, objectIndex);
            return 100000 + lodGroup * m_MaxLODObjectCount + objectIndex;
        }

        private void VisibleAreaChangeInternal(Rect oldVisibleArea, Rect newVisibleArea)
        {
            var oldBounds = GetBoundsInVisibleArea(oldVisibleArea);
            var newBounds = GetBoundsInVisibleArea(newVisibleArea);

            if (oldBounds != newBounds)
            {
                var oldMin = oldBounds.min;
                var oldMax = oldBounds.max;
                var newMin = newBounds.min;
                var newMax = newBounds.max;

                for (var y = oldMin.y; y <= oldMax.y; ++y)
                {
                    for (var x = oldMin.x; x <= oldMax.x; ++x)
                    {
                        if (x >= 0 && x < m_XGridCount && y >= 0 && y < m_YGridCount)
                        {
                            if (y < newMin.y || y > newMax.y ||
                            x < newMin.x || x > newMax.x)
                            {
                                ShowGridObjects(x, y, false, changeLOD: false);   
                            }
                        }
                    }
                }

                for (var y = newMin.y; y <= newMax.y; ++y)
                {
                    for (var x = newMin.x; x <= newMax.x; ++x)
                    {
                        if (x >= 0 && x < m_XGridCount && y >= 0 && y < m_YGridCount)
                        {
                            if (y < oldMin.y || y > oldMax.y ||
                            x < oldMin.x || x > oldMax.x)
                            {
                                ShowGridObjects(x, y, true, changeLOD: false);
                            }
                        }
                    }
                }
            }
        }

        private bool VisibleTest(DecorationObject decoration)
        {
            var area = m_VisibleAreaUpdater.CurrentArea;
            var overlap = decoration.IntersectWith(area.xMin, area.yMin, area.width, area.height);
            return decoration.SetVisibility(overlap ? WorldObjectVisibility.Visible : WorldObjectVisibility.Invisible);
        }

        private void GetOverlappedGrids(float minX, float minZ, float maxX, float maxZ, List<GridData> grids)
        {
            grids.Clear();
            var min = PositionToCoordinate(minX, minZ);
            var max = PositionToCoordinate(maxX, maxZ);
            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    if (x >= 0 && x < m_XGridCount &&
                        y >= 0 && y < m_YGridCount)
                    {
                        grids.Add(GetGrid(x, y));
                    }
                }
            }
        }

        private bool IsSetEnabled(int objectID)
        {
            if (!m_CustomSetEnabled.TryGetValue(objectID, out bool enabled))
            {
                return true;
            }
            return enabled;
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var deserializer = world.QueryGameDataDeserializer(world.ID, $"decoration@{pluginName}");

            deserializer.ReadInt32("GridData.Version");

            m_GridWidth = deserializer.ReadSingle("Grid Width");
            m_GridHeight = deserializer.ReadSingle("Grid Height");
            m_XGridCount = deserializer.ReadInt32("X Grid Count");
            m_YGridCount = deserializer.ReadInt32("Y Grid Count");
            m_Bounds = deserializer.ReadBounds("Bounds");
            m_Name = deserializer.ReadString("Name");
            m_ID = deserializer.ReadInt32("ID");
            m_ResourceDescriptorSystem = deserializer.ReadSerializable<ResourceDescriptorSystem>("Resource Descriptor System", true);
            m_LODSystem = deserializer.ReadSerializable<IPluginLODSystem>("Plugin LOD System", true);
            m_MaxLODObjectCount = deserializer.ReadInt32("Max LOD0 Object Count");

            var gridCount = m_XGridCount * m_YGridCount;
            m_GridData = new GridData[gridCount];
            for (var i = 0; i < gridCount; ++i)
            {
                var x = i % m_XGridCount;
                var y = i / m_XGridCount;
                m_GridData[i] = new GridData(m_LODSystem.LODCount, x, y);
                for (var lod = 0; lod < m_LODSystem.LODCount; ++lod)
                {
                    m_GridData[i].LODs[lod].ObjectGlobalIndices = deserializer.ReadInt32List("Object Index");
                }
            }

            m_DecorationMetaData = new DecorationMetaData
            {
                LODResourceChangeMasks = deserializer.ReadByteArray("LOD Resource Change Masks"),
                ResourceMetadataIndex = deserializer.ReadInt32Array("Resource Metadata Index"),
                Position = deserializer.ReadVector3Array("Position"),
            };

            var resourceMetadataCount = deserializer.ReadInt32("Resource Metadata Count");
            m_ResourceMetadata = new List<ResourceMetadata>(resourceMetadataCount);
            for (var i = 0; i < resourceMetadataCount; ++i)
            {
                var batchIndex = deserializer.ReadInt32("GPU Batch ID");
                var rotation = deserializer.ReadQuaternion("Rotation");
                var scale = deserializer.ReadVector3("Scale");
                var bounds = deserializer.ReadRect("Bounds");
                var assetPath = deserializer.ReadString("Resource Path");
                m_ResourceMetadata.Add(new ResourceMetadata(batchIndex, rotation, scale, bounds, assetPath));
            }

            deserializer.Uninit();
        }

        private DecorationSystemRenderer m_Renderer;
        private Bounds m_Bounds;
        private CameraVisibleAreaUpdater m_VisibleAreaUpdater;
        private float m_GridWidth;
        private float m_GridHeight;
        private bool m_UpdateNeeded;
        private GridData[] m_GridData;
        private DecorationMetaData m_DecorationMetaData;
        private IPluginLODSystem m_LODSystem;
        private List<ResourceMetadata> m_ResourceMetadata;
        private int m_XGridCount;
        private int m_YGridCount;
        private readonly DecorationObjectPool m_DecorationPool = new();
        private Action<DecorationObject> m_ToggleActiveState;
        private readonly Dictionary<int, DecorationObject> m_VisibleObjects = new(1800);
        private readonly SimpleStopwatch m_Timer = new();
        private int m_MaxLODObjectCount;
        private readonly List<FrameTask> m_Tasks = new();
        private ResourceDescriptorSystem m_ResourceDescriptorSystem;
        private string m_Name;
        private readonly Dictionary<int, bool> m_CustomSetEnabled = new();
        private readonly List<GridData> m_TempGrids = new();
        private readonly List<int> m_TempIDs = new();
    }
}


//XDay