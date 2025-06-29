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

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using XDay.WorldAPI.Editor;
using System.Linq;
using System;

namespace XDay.NavigationAPI.Editor
{
    public class UnityNavMeshBuildSetting : INavMeshBuildSetting
    {
        //unity的导航网格烘培设置
        public NavMeshBuildSettings NavmeshBuildSetting;
        //收集障碍物信息
        public List<IObstacleSource> ObstacleSources = new();
        //收集可行走物体信息
        public List<IWalkableObjectSource> WalkableObjectSources = new();
    }

    public class UnityNavMeshBuilder : INavMeshBuilder
    {
        public NavMeshBuildResult BuildNavMesh(INavMeshBuildSetting buildSetting)
        {
            var setting = buildSetting as UnityNavMeshBuildSetting;
            bool ok = Check(setting);
            if (!ok)
            {
                return null;
            }

            NavMesh.RemoveAllNavMeshData();
            m_DataInstance.Remove();

            var sources = CollectSources(setting);
            var bounds = CalculateWorldBounds(sources);

            m_NavMeshData = NavMeshBuilder.BuildNavMeshData(setting.NavmeshBuildSetting, sources, bounds, Vector3.zero, Quaternion.identity);
            //m_NavMeshData = NavMeshBuilder.BuildNavMeshData(setting.NavmeshBuildSetting, new(), new Bounds(), Vector3.zero, Quaternion.identity);
            //ok = NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMeshData, setting.NavmeshBuildSetting, sources, bounds);
            //Debug.Assert(ok);
            m_DataInstance = NavMesh.AddNavMeshData(m_NavMeshData);

            //注意只包含用于寻路的navmesh,不带地形高度.The returned mesh contains only the triangles used for pathfinding. It does not contain the detail that is used to place the agents on the walkable surface. This can be noticeable on locations with curved surfaces.
            //这里生成的顶点包含重复,需要去重
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            RebuildMesh(triangulation, out var meshVertices, out var meshIndices);
            NavMeshBuildResult result = new()
            {
                MeshVertices = meshVertices,
                MeshIndices = meshIndices,
                TriangleAreaIDs = Helper.ConvertToUInt16Array(triangulation.areas),
            };

            List<ushort> uniqueTypes = new(result.TriangleAreaIDs);
            Debug.Log($"生成导航网格. 顶点数:{result.MeshVertices.Length} 三角形数:{result.MeshIndices.Length / 3} 区域类型:{Helper.ToString(uniqueTypes.Distinct().ToArray())}");

            Clear();

            return result;
        }

        private void RebuildMesh(NavMeshTriangulation triangulation, out Vector3[] meshVertices, out int[] meshIndices)
        {
            meshVertices = new Vector3[triangulation.vertices.Length];
            meshIndices = triangulation.indices.Clone() as int[];
            int idx = 0;
            Dictionary<Vector3, int> newVertices = new();
            foreach (var vert in triangulation.vertices)
            {
                if (!newVertices.ContainsKey(vert))
                {
                    meshVertices[idx] = ChangePrecision(vert);
                    newVertices.Add(vert, idx++);
                }
            }

            for (var i = 0; i < triangulation.indices.Length; i++)
            {
                var pos = triangulation.vertices[triangulation.indices[i]];
                meshIndices[i] = newVertices[pos];
            }
        }

        private Vector3 ChangePrecision(Vector3 vert)
        {
            return new Vector3(Helper.ChangePrecision(vert.x), Helper.ChangePrecision(vert.y), Helper.ChangePrecision(vert.z));
        }

        private bool Check(UnityNavMeshBuildSetting setting)
        {
            var param = setting.NavmeshBuildSetting;
            if (param.tileSize < 0)
            {
                Debug.LogError("无效的Tile Size");
                return false;
            }
            if (param.voxelSize < 0)
            {
                Debug.LogError("无效的Voxel Size");
                return false;
            }
            if (param.overrideTileSize && param.tileSize <= 0)
            {
                Debug.LogError("无效的Tile Size");
                return false;
            }
            if (param.overrideVoxelSize && param.voxelSize <= 0)
            {
                Debug.LogError("无效的Voxel Size");
                return false;
            }
            return true;
        }

        private Bounds CalculateWorldBounds(List<NavMeshBuildSource> sources)
        {
            // Use the unscaled matrix for the NavMeshSurface
            Matrix4x4 worldToLocal = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            worldToLocal = worldToLocal.inverse;

            var result = new Bounds();
            foreach (var src in sources)
            {
                switch (src.shape)
                {
                    case NavMeshBuildSourceShape.Mesh:
                        {
                            var m = src.sourceObject as Mesh;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                            break;
                        }
                    case NavMeshBuildSourceShape.Terrain:
                        {
#if NMC_CAN_ACCESS_TERRAIN
                        // Terrain pivot is lower/left corner - shift bounds accordingly
                        var t = src.sourceObject as TerrainData;
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
#else
                            Debug.LogWarning("The NavMesh cannot be properly baked for the terrain because the necessary functionality is missing. Add the com.unity.modules.terrain package through the Package Manager.");
#endif
                            break;
                        }
                    case NavMeshBuildSourceShape.Box:
                    case NavMeshBuildSourceShape.Sphere:
                    case NavMeshBuildSourceShape.Capsule:
                    case NavMeshBuildSourceShape.ModifierBox:
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                        break;
                }
            }
            // Inflate the bounds a bit to avoid clipping co-planar sources
            result.Expand(0.1f);
            return result;
        }

        private Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        private Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
        {
            var absAxisX = Abs(mat.MultiplyVector(Vector3.right));
            var absAxisY = Abs(mat.MultiplyVector(Vector3.up));
            var absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
            var worldPosition = mat.MultiplyPoint(bounds.center);
            var worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
            return new Bounds(worldPosition, worldSize);
        }

        private void CollectObstacles(List<Vector3> outline, float meshHeight, int area, bool walkable, List<NavMeshBuildSource> outputSources)
        {
            CreateMeshGameObject(outline, meshHeight, out var gameObject, out Mesh mesh);

            m_TempObstacleMeshes.Add(mesh);
            m_TempObstacleGameObjects.Add(gameObject);

            NavMeshBuildSource source = new()
            {
                transform = Matrix4x4.identity,
                shape = NavMeshBuildSourceShape.Mesh,
            };
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            source.sourceObject = meshFilter.sharedMesh;
            source.component = meshFilter;
            if (!walkable)
            {
                source.area = 1;
            }
            else
            {
                Debug.Assert(area >= 0 && area <= 31, "Unity NavMesh Area must be [0, 31]");
                source.area = area;
            }

            outputSources.Add(source);
        }

        private List<NavMeshBuildSource> CollectSources(UnityNavMeshBuildSetting setting)
        {
            List<NavMeshBuildSource> sources = new();

            //收集障碍物
            foreach (var obstacleSource in setting.ObstacleSources)
            {
                foreach (var obstacle in obstacleSource.GetObstacles())
                {
                    CollectObstacles(obstacle.WorldPolygon, obstacle.Height, obstacle.AreaID, obstacle.Walkable, sources);
                }
            }

            //收集Collider可行走区域
            foreach (var walkableObjectSource in setting.WalkableObjectSources)
            {
                List<ColliderSource> colliderSources = walkableObjectSource.GetAllWalkableColliders();
                foreach (var colliderSource in colliderSources)
                {
                    var collider = colliderSource.Collider;
                    NavMeshBuildSource source = new()
                    {
                        transform = collider.transform.localToWorldMatrix,
                        area = colliderSource.AreaID
                    };
                    if (collider is MeshCollider)
                    {
                        source.shape = NavMeshBuildSourceShape.Mesh;
                        source.sourceObject = (collider as UnityEngine.MeshCollider).sharedMesh;
                    }
                    else if (collider is UnityEngine.SphereCollider)
                    {
                        source.shape = NavMeshBuildSourceShape.Sphere;
                        source.size = Vector3.one;
                    }
                    else if (collider is UnityEngine.BoxCollider)
                    {
                        source.shape = NavMeshBuildSourceShape.Box;
                        source.size = Vector3.one;
                    }
                    else if (collider is CapsuleCollider)
                    {
                        source.shape = NavMeshBuildSourceShape.Capsule;
                        source.size = Vector3.one;
                    }
                    else
                    {
                        Debug.Assert(false, "not supported");
                    }
                    sources.Add(source);
                }

                //收集Mesh可行走区域
                List<MeshSource> meshes = walkableObjectSource.GetAllWalkableMeshes();
                foreach (var meshSource in meshes)
                {
                    NavMeshBuildSource source = new()
                    {
                        transform = meshSource.GameObject.transform.localToWorldMatrix,
                        shape = NavMeshBuildSourceShape.Mesh,
                        sourceObject = meshSource.Mesh,
                        area = meshSource.AreaID,
                    };
                    sources.Add(source);
                }
            }

            return sources;
        }

        private void CreateMeshGameObject(List<Vector3> polygon, float meshHeight, out GameObject gameObject, out Mesh mesh)
        {
            PolygonExtruder.Extrude(polygon, 0, meshHeight, out var vertices, out var indices);

            mesh = new Mesh
            {
                vertices = vertices,
                triangles = indices
            };

            gameObject = new GameObject("");
            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>();
        }

        private void Clear()
        {
            foreach (var mesh in m_TempObstacleMeshes)
            {
                Helper.DestroyUnityObject(mesh);
            }
            m_TempObstacleMeshes.Clear();

            foreach (var obj in m_TempObstacleGameObjects)
            {
                Helper.DestroyUnityObject(obj);
            }
            m_TempObstacleGameObjects.Clear();
        }

        private NavMeshDataInstance m_DataInstance;
        private NavMeshData m_NavMeshData;
        private readonly List<Mesh> m_TempObstacleMeshes = new();
        private readonly List<GameObject> m_TempObstacleGameObjects = new();
    }
}