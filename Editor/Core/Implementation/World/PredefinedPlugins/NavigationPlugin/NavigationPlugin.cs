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
using UnityEngine.AI;
using XDay.NavigationAPI;
using XDay.NavigationAPI.Editor;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Navigation.Editor
{
    [WorldPluginMetadata("导航层", "navigation_data", typeof(NavigationPluginCreateWindow), singleton: true)]
    internal partial class NavigationPlugin : EditorWorldPlugin
    {
        public bool ShowDebugInfo => m_ShowDebugInfo;
        public override GameObject Root => m_Renderer == null ? null : m_Renderer.Root;
        public override List<string> GameFileNames => new() { "navigation" };
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
        public override string TypeName => "EditorNavigationPlugin";

        public NavigationPlugin()
        {
        }

        public NavigationPlugin(int id, int objectIndex, string name)
            : base(id, objectIndex)
        {
            m_Name = name;
        }

        protected override void InitInternal()
        {
            Selection.selectionChanged += OnSelectionChanged;

            m_Renderer = new NavigationPluginRenderer(World.Root.transform, this);

            var obj = new GameObject("Draw Path");
            obj.transform.SetParent(m_Renderer.Root.transform, false);
            m_DrawPath = obj.AddComponent<DrawPolyLineInEditor>();

            m_Renderer.CreateNavMeshRenderer(m_LastBuiltResult);
        }

        private void OnSelectionChanged()
        {
        }

        protected override void UninitInternal()
        {
            m_Renderer.OnDestroy();

            Selection.selectionChanged -= OnSelectionChanged;
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            throw new System.NotImplementedException();
        }

        public override void DestroyObjectUndo(int objectID)
        {
            throw new System.NotImplementedException();
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            throw new System.NotImplementedException();
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (!base.SetAspect(objectID, name, aspect))
            {
                //var obj = World.QueryObject<ShapeObject>(objectID);
                //if (obj != null)
                //{
                //    var ok = obj.SetAspect(objectID, name, aspect);
                //    if (ok)
                //    {
                //        m_Renderer.SetAspect(objectID, name);
                //    }
                //}
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

            return null;
        }

        public void BuildNavMesh()
        {
            var builder = new UnityNavMeshBuilder();
            var setting = new UnityNavMeshBuildSetting()
            {
                NavmeshBuildSetting = m_BuildParam,
                ObstacleSources = GetObstacleSources(),
                WalkableObjectSources = GetWalkableObjectSources(),
            };
            m_LastBuiltResult = builder.BuildNavMesh(setting);
            m_Renderer.CreateNavMeshRenderer(m_LastBuiltResult);
        }

        public void ClearNavMesh()
        {
            m_LastBuiltResult = null;
            m_Renderer.ClearNavMesh();
            NavMesh.RemoveAllNavMeshData();
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "NavigationPlugin.Version");
            serializer.WriteString(m_Name, "Name");

            Vector3[] vertices = null;
            int[] indices = null;
            ushort[] triangleTypes = null;
            if (m_LastBuiltResult != null)
            {
                vertices = m_LastBuiltResult.MeshVertices;
                indices = m_LastBuiltResult.MeshIndices;
                triangleTypes = m_LastBuiltResult.TriangleTypes;
            }
            serializer.WriteVector3Array(vertices, "Vertices");
            serializer.WriteInt32Array(indices, "Indices");
            serializer.WriteUInt16Array(triangleTypes, "Triangle Types");

            SaveBuildParam(serializer);

            serializer.WriteBoolean(m_ShowDebugInfo, "Show Debug Info");
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("NavigationPlugin.Version");

            m_Name = deserializer.ReadString("Name");
            var vertices = deserializer.ReadVector3Array("Vertices");
            var indices = deserializer.ReadInt32Array("Indices");
            var triangleTypes = deserializer.ReadUInt16Array("Triangle Types");
            if (vertices.Length > 0)
            {
                m_LastBuiltResult = new NavMeshBuildResult()
                {
                    TriangleTypes = triangleTypes,
                    MeshVertices = vertices,
                    MeshIndices = indices,
                };
            }

            LoadBuildParam(deserializer);

            m_ShowDebugInfo = deserializer.ReadBoolean("Show Debug Info");
        }

        private void SaveBuildParam(ISerializer serializer)
        {
            serializer.WriteSingle(m_BuildParam.agentRadius, "Agent Radius");
            serializer.WriteSingle(m_BuildParam.agentHeight, "Agent Height");
            serializer.WriteSingle(m_BuildParam.agentSlope, "Agent Slope");
            serializer.WriteSingle(m_BuildParam.agentClimb, "Agent Climb");
            serializer.WriteSingle(m_BuildParam.ledgeDropHeight, "Ledge Drop Height");
            serializer.WriteSingle(m_BuildParam.maxJumpAcrossDistance, "Max Jump Across Distance");
            serializer.WriteSingle(m_BuildParam.minRegionArea, "Min Region Area");
            serializer.WriteSingle(m_BuildParam.voxelSize, "Voxel Size");
            serializer.WriteInt32(m_BuildParam.tileSize, "Tile Size");
            serializer.WriteBoolean(m_BuildParam.overrideVoxelSize, "Override Voxel Size");
            serializer.WriteBoolean(m_BuildParam.overrideTileSize, "Override Tile Size");
            serializer.WriteBoolean(m_BuildParam.buildHeightMesh, "Build Height Mesh");
            serializer.WriteBoolean(m_BuildParam.preserveTilesOutsideBounds, "Preserve Tiles Outside Bounds");
        }

        private void LoadBuildParam(IDeserializer deserializer)
        {
            m_BuildParam.agentRadius = deserializer.ReadSingle("Agent Radius");
            m_BuildParam.agentHeight = deserializer.ReadSingle("Agent Height");
            m_BuildParam.agentSlope = deserializer.ReadSingle("Agent Slope");
            m_BuildParam.agentClimb = deserializer.ReadSingle("Agent Climb");
            m_BuildParam.ledgeDropHeight = deserializer.ReadSingle("Ledge Drop Height");
            m_BuildParam.maxJumpAcrossDistance = deserializer.ReadSingle("Max Jump Across Distance");
            m_BuildParam.minRegionArea = deserializer.ReadSingle("Min Region Area");
            m_BuildParam.voxelSize = deserializer.ReadSingle("Voxel Size");
            m_BuildParam.tileSize = deserializer.ReadInt32("Tile Size");
            m_BuildParam.overrideVoxelSize = deserializer.ReadBoolean("Override Voxel Size");
            m_BuildParam.overrideTileSize = deserializer.ReadBoolean("Override Tile Size");
            m_BuildParam.buildHeightMesh = deserializer.ReadBoolean("Build Height Mesh");
            m_BuildParam.preserveTilesOutsideBounds = deserializer.ReadBoolean("Preserve Tiles Outside Bounds");
        }

        private List<IObstacleSource> GetObstacleSources()
        {
            List<IObstacleSource> sources = new();
            for (var i = 0; i < World.PluginCount; ++i)
            {
                var plugin = World.GetPlugin(i);
                if (plugin is IObstacleSource obstacleSource)
                {
                    sources.Add(obstacleSource);
                }
            }
            return sources;
        }

        private List<IWalkableObjectSource> GetWalkableObjectSources()
        {
            List<IWalkableObjectSource> sources = new();
            for (var i = 0; i < World.PluginCount; ++i)
            {
                var plugin = World.GetPlugin(i);
                if (plugin is IWalkableObjectSource source)
                {
                    sources.Add(source);
                }
            }
            return sources;
        }

        protected override void UpdateInternal(float dt)
        {
            SceneView.RepaintAll();
        }

        private string m_Name;
        private bool m_ShowDebugInfo = false;
        private NavigationPluginRenderer m_Renderer;
        private PathFinderGizmo m_PathFinder = new();
        private MyNavigationSystem m_NavigationSystem;
        private NavMeshBuildResult m_LastBuiltResult;
        private NavMeshBuildSettings m_BuildParam = new()
        {
            overrideTileSize = false,
            tileSize = 256,
            agentClimb = 1.0f,
            agentHeight = 2.0f,
            agentRadius = 0.5f,
            agentSlope = 45.0f,
            minRegionArea = 2,
        };
        private const int m_Version = 1;
    }
}
