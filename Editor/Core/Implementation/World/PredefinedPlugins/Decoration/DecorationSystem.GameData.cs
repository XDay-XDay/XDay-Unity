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
using XDay.AnimationAPI;
using XDay.RenderingAPI.BRG;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Decoration.Editor
{
    partial class DecorationSystem
    {
        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            SyncObjectTransforms();

            GenerateGridData(converter);
        }

        private void GenerateGridData(IObjectIDConverter converter)
        {
            BeforeGenerate();

            var xGridCount = Mathf.CeilToInt(Bounds.size.x / m_GameGridSize.x);
            var yGridCount = Mathf.CeilToInt(Bounds.size.z / m_GameGridSize.y);

            CalculateGameData(xGridCount, yGridCount, out var maxLOD0ObjectCount, out var decorationMetadata, out var gridData, out var resourceMetadata);

            ISerializer serializer = ISerializer.CreateBinary();

            serializer.WriteInt32(m_RuntimeVersion, "GridData.Version");

            serializer.WriteSingle(m_GameGridSize.x, "Grid Width");
            serializer.WriteSingle(m_GameGridSize.y, "Grid Height");
            serializer.WriteInt32(xGridCount, "X Grid Count");
            serializer.WriteInt32(yGridCount, "Y Grid Count");
            serializer.WriteBounds(Bounds, "Bounds");
            serializer.WriteString(Name, "Name");
            serializer.WriteObjectID(ID, "ID", converter);

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, true);
            serializer.WriteSerializable(m_PluginLODSystem, "Plugin LOD System", converter, true);

            serializer.WriteInt32(maxLOD0ObjectCount, "Max LOD0 Object Count");

            for (var i = 0; i < gridData.Length; ++i)
            {
                for (var lod = 0; lod < LODCount; ++lod)
                {
                    serializer.WriteInt32List(gridData[i].LODs[lod].DecorationMetadataIndices, "Decoration Metadata Index");
                }
            }

            serializer.WriteByteArray(decorationMetadata.LODResourceChangeMasks, "LOD Resource Change Masks");
            serializer.WriteInt32Array(decorationMetadata.ResourceMetadataIndex, "Resource Metadata Index");
            serializer.WriteVector2Array(decorationMetadata.Position, "Position XZ");

            var n = resourceMetadata.Count;
            serializer.WriteInt32(n, "Resource Metadata Count");
            for (var i = 0; i < n; ++i)
            {
                serializer.WriteInt32(resourceMetadata[i].GPUBatchID, "GPU Batch ID");
                serializer.WriteQuaternion(resourceMetadata[i].Rotation, "Rotation");
                serializer.WriteVector3(resourceMetadata[i].Scale, "Scale");
                serializer.WriteRect(resourceMetadata[i].Bounds, "Bounds");
                serializer.WriteString(resourceMetadata[i].Path, "Resource Path");
            }

            serializer.Uninit();
            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("decoration"));

            EndGenerate();
        }

        private void CalculateGameData(int xGridCount, int yGridCount,
            out int maxLOD0ObjectCount,
            out GameDecorationMetaData decorationMetadata,
            out GameGridData[] gridData,
            out List<GameResourceMetadata> resourceMetadatas)
        {
            resourceMetadatas = new List<GameResourceMetadata>();

            var decorations = new List<DecorationObject>();
            foreach (var kv in m_Decorations)
            {
                GenerateDecorations(kv.Value, kv.Value.ResourceDescriptor.Prefab, m_Renderer.QueryGameObject(kv.Key), decorations);
            }

            decorationMetadata = new GameDecorationMetaData
            {
                Position = new Vector2[decorations.Count],
                ResourceMetadataIndex = new int[decorations.Count],
                LODResourceChangeMasks = new byte[decorations.Count],
            };

            for (var i = 0; i < decorations.Count; ++i)
            {
                decorationMetadata.LODResourceChangeMasks[i] = CalculateLODChangeMasks(decorations[i]);
                decorationMetadata.Position[i] = decorations[i].Position.ToVector2();
                decorationMetadata.ResourceMetadataIndex[i] = QueryResourceMetadataIndex(decorations[i], resourceMetadatas);
            }

            var grid = new Grid(LODCount, m_GameGridSize.x, m_GameGridSize.y, xGridCount, yGridCount, Bounds.ToRect());
            foreach (var decoration in decorations)
            {
                grid.Add(decoration);
            }
            gridData = grid.Data;

            CalculateMaxObjectCount(gridData, out maxLOD0ObjectCount);
        }

        private void CalculateMaxObjectCount(GameGridData[] gridData, out int maxLODObjectCount)
        {
            maxLODObjectCount = 0;
            for (var lod = 0; lod < LODCount; ++lod)
            {
                var n = 0;
                for (var i = 0; i < gridData.Length; ++i)
                {
                    n += gridData[i].LODs[lod].DecorationMetadataIndices.Count;
                }

                if (n > maxLODObjectCount)
                {
                    maxLODObjectCount = n;
                }
            }
        }

        private int QueryResourceMetadataIndex(DecorationObject decoration, List<GameResourceMetadata> resourceMetadata)
        {
            var path = decoration.ResourceDescriptor.GetPath(0);
            var batchID = CalculateBatchID(decoration.ResourceDescriptor);

            var resourceMetadataIndex = -1;
            for (var i = 0; i < resourceMetadata.Count; ++i)
            {
                if (path == resourceMetadata[i].Path &&
                    decoration.Scale == resourceMetadata[i].Scale &&
                    decoration.Rotation == resourceMetadata[i].Rotation)
                {
                    resourceMetadataIndex = i;
                    break;
                }
            }

            if (resourceMetadataIndex == -1)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var bounds = prefab.QueryRectWithLocalScaleAndRotation(decoration.Rotation, decoration.Scale);
                resourceMetadataIndex = resourceMetadata.Count;
                resourceMetadata.Add(new GameResourceMetadata(batchID, decoration.Rotation, decoration.Scale, bounds, path));
            }

            resourceMetadata[resourceMetadataIndex].GPUBatchID = batchID;

            return resourceMetadataIndex;
        }

        private int CalculateBatchID(IResourceDescriptor descriptor)
        {
            var lod0Path = descriptor.GetPath(0);
            if (m_ResourceGPUBatchID.TryGetValue(lod0Path, out var batchID))
            {
                if (batchID >= 0 && batchID < GameDefine.ANIMATOR_BATCH_START_ID)
                {
                    m_BatchInfoRegistry.InstanceCountInc(batchID);
                }
                else if (batchID >= GameDefine.ANIMATOR_BATCH_START_ID)
                {
                    m_InstanceAnimatorBatchInfoManager.InstanceCountInc(batchID);
                }
                return batchID;
            }

            if (CheckInstanceRenderingForAllLODs(descriptor, out var isAnimator))
            {
                batchID = RegisterBatch(descriptor, isAnimator);
            }
            else
            {
                batchID = -1;
            }

            m_ResourceGPUBatchID.Add(lod0Path, batchID);
            return batchID;
        }

        private bool CheckInstanceRenderingForAllLODs(IResourceDescriptor descriptor, out bool isAnimator)
        {
            var ok = true;
            isAnimator = false;
            for (var lod = 0; lod < descriptor.LODCount; ++lod)
            {
                var path = descriptor.GetPath(lod);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!m_BatchInfoRegistry.CheckInstanceRendering(prefab, out isAnimator))
                {
                    ok = false;
                }
            }
            return ok;
        }

        private int RegisterBatch(IResourceDescriptor descriptor, bool isAnimator) 
        {
            if (isAnimator)
            {
                var batchID = m_InstanceAnimatorBatchInfoManager.CreateBatch();
                var batch = m_InstanceAnimatorBatchInfoManager.GetBatch(batchID);
                for (var lod = 0; lod < descriptor.LODCount; ++lod)
                {
                    var path = descriptor.GetPath(lod);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var setting = prefab.GetComponent<GPUAnimationBakeSetting>();
                    batch.AddLOD(setting.Setting.AdvancedSetting.InstanceAnimatorData);
                }
                return batchID;
            }
            else
            {
                var batchID = m_BatchInfoRegistry.CreateBatchInfo();
                var batch = m_BatchInfoRegistry.GetBatch(batchID);
                for (var lod = 0; lod < descriptor.LODCount; ++lod)
                {
                    var path = descriptor.GetPath(lod);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    batch.AddLOD(prefab.GetComponentInChildren<MeshFilter>().sharedMesh, prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial);
                }
                return batchID;
            }
        }

        private void BeforeGenerate()
        {
            m_ResourceGPUBatchID = new Dictionary<string, int>();
            m_InstanceAnimatorBatchInfoManager = ScriptableObject.CreateInstance<InstanceAnimatorBatchInfoRegistry>();
            m_BatchInfoRegistry = ScriptableObject.CreateInstance<GPUBatchInfoRegistry>();
        }

        private void EndGenerate()
        {
            AssetDatabase.CreateAsset(m_InstanceAnimatorBatchInfoManager, $"{World.GameFolder}/DecorationInstanceBatchInfo.asset");
            AssetDatabase.CreateAsset(m_BatchInfoRegistry, $"{World.GameFolder}/DecorationBatchInfo.asset");
            m_ResourceGPUBatchID = null;
            m_BatchInfoRegistry = null;
            m_InstanceAnimatorBatchInfoManager = null;
        }

        private void GenerateDecorations(DecorationObject decoration, GameObject decorationPrefab, GameObject decorationGameObject, List<DecorationObject> decorations)
        {
            if (decorationPrefab.GetComponent<DecorationObjectGroup>() == null)
            {
                decorations.Add(decoration);
                decoration.RuntimeObjectIndex = decorations.Count - 1;
            }
            else
            {
                var childCount = decorationPrefab.transform.childCount;
                for (var i = 0; i < childCount; ++i)
                {
                    var childGameObject = decorationGameObject.transform.GetChild(i).gameObject;
                    var childPrefabInstance = decorationPrefab.transform.GetChild(i).gameObject;
                    var childPrefab = PrefabUtility.GetCorrespondingObjectFromSource(childPrefabInstance);
                    if (childPrefab == null)
                    {
                        Debug.LogError($"decoration {childGameObject.name} is not prefab!");
                    }
                    else
                    {
                        var childPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(childPrefab);
                        var childDescriptor = m_ResourceDescriptorSystem.CreateDescriptorIfNotExists(childPrefabPath, World);
                        //child object id is 0
                        var childDecoration = new DecorationObject(0, -1, true, decoration.LODLayerMask, childGameObject.transform.position, childGameObject.transform.rotation, childGameObject.transform.lossyScale, childDescriptor);
                        childDecoration.Init(World);

                        GenerateDecorations(childDecoration, childPrefabInstance, childGameObject, decorations);
                    }
                }
            }
        }

        private byte CalculateLODChangeMasks(DecorationObject decoration)
        {
            byte masks = 0;
            var prevLODPath = decoration.ResourceDescriptor.GetPath(0);
            for (var lod = 1; lod < LODCount; ++lod)
            {
                var curLODPath = decoration.ResourceDescriptor.GetPath(lod);
                if (!decoration.ExistsInLOD(lod) ||
                    prevLODPath != curLODPath)
                {
                    masks |= (byte)(1 << lod);
                }
                prevLODPath = curLODPath;
            }
            return masks;
        }

        private InstanceAnimatorBatchInfoRegistry m_InstanceAnimatorBatchInfoManager;
        private GPUBatchInfoRegistry m_BatchInfoRegistry;
        private Dictionary<string, int> m_ResourceGPUBatchID;
        private const int m_RuntimeVersion = 1;
    }
}



//XDay