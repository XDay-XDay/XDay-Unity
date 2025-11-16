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
    public partial class DecorationSystem
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

            CalculateGameData(xGridCount, yGridCount, 
                out var maxLOD0ObjectCount, 
                out var decorationMetadata, 
                out var gridData, 
                out var initialVisible,
                out var resourceMetadata);

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
            serializer.WriteVector3Array(decorationMetadata.Position, "Position");

            var n = resourceMetadata.Count;
            serializer.WriteInt32(n, "Resource Metadata Count");
            for (var i = 0; i < n; ++i)
            {
                serializer.WriteInt32(resourceMetadata[i].GPUBatchID, "GPU Batch ID");
                serializer.WriteQuaternion(resourceMetadata[i].Rotation, "Rotation");
                serializer.WriteVector3(resourceMetadata[i].Scale, "Scale");
                serializer.WriteRect(resourceMetadata[i].Bounds, "Bounds");
                serializer.WriteString(resourceMetadata[i].Path, "Resource Path");
                serializer.WriteInt32((int)resourceMetadata[i].Type, "Type");
            }

            serializer.WriteBooleanArray(initialVisible, "InitialVisible");

            serializer.Uninit();
            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("decoration"));

            AfterGenerate();
        }

        private void CalculateGameData(int xGridCount, int yGridCount,
            out int maxLOD0ObjectCount,
            out GameDecorationMetaData decorationMetadata,
            out GameGridData[] gridData,
            out bool[] initialVisible,
            out List<GameResourceMetadata> resourceMetadatas)
        {
            resourceMetadatas = new List<GameResourceMetadata>();

            var decorations = new List<DecorationObject>();
            foreach (var kv in m_Decorations)
            {
                var descriptor = kv.Value.ResourceDescriptor;
                if (descriptor != null) 
                {
                    GenerateDecorations(kv.Value, descriptor.Prefab, m_Renderer.QueryGameObject(kv.Key), decorations);
                }
            }

            initialVisible = new bool[decorations.Count];

            decorationMetadata = new GameDecorationMetaData
            {
                Position = new Vector3[decorations.Count],
                ResourceMetadataIndex = new int[decorations.Count],
                LODResourceChangeMasks = new byte[decorations.Count],
            };

            for (var i = 0; i < decorations.Count; ++i)
            {
                decorationMetadata.LODResourceChangeMasks[i] = CalculateLODChangeMasks(decorations[i]);
                decorationMetadata.Position[i] = decorations[i].Position;
                decorationMetadata.ResourceMetadataIndex[i] = QueryResourceMetadataIndex(decorations[i], resourceMetadatas);
                initialVisible[i] = IsInitialVisible(decorations[i]);
            }

            var grid = new Grid(LODCount, m_GameGridSize.x, m_GameGridSize.y, xGridCount, yGridCount, Bounds.ToRect());
            foreach (var decoration in decorations)
            {
                grid.Add(decoration);
            }
            gridData = grid.Data;

            CalculateMaxObjectCount(gridData, out maxLOD0ObjectCount);
        }

        private bool IsInitialVisible(DecorationObject decorationObject)
        {
            var type = TagToType(decorationObject.Tag, out _);
            return type != DecorationTagType.HideableAfter && 
                type != DecorationTagType.ObstacleAfter;
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
            var batchID = CalculateBatchID(decoration);

            var type = GetDecorationObjectType(path);
            var resourceMetadataIndex = -1;

            var realScale = GetRealScale(batchID, decoration);
            var realRotation = GetRealRotation(batchID, decoration);

            for (var i = 0; i < resourceMetadata.Count; ++i)
            {
                //如果使用instance rendering,要比较mesh的transform
                if (path == resourceMetadata[i].Path &&
                    realScale == resourceMetadata[i].Scale &&
                    realRotation == resourceMetadata[i].Rotation &&
                    type == resourceMetadata[i].Type)
                {
                    resourceMetadataIndex = i;
                    break;
                }
            }

            if (resourceMetadataIndex == -1)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var bounds = prefab.QueryRectWithLocalScaleAndRotation(realRotation, realScale);
                resourceMetadataIndex = resourceMetadata.Count;
                resourceMetadata.Add(new GameResourceMetadata(batchID, realRotation, realScale, bounds, path, type));
            }

            resourceMetadata[resourceMetadataIndex].GPUBatchID = batchID;

            return resourceMetadataIndex;
        }

        private Vector3 GetRealScale(int batchID, DecorationObject decoration)
        {
            if (batchID < 0)
            {
                return decoration.Scale;
            }

            var gameObject = m_Renderer.GetGameObject(decoration.ID);
            if (gameObject != null)
            {
                var filter = gameObject.GetComponentInChildren<MeshFilter>();
                if (filter != null)
                {
                    return filter.gameObject.transform.lossyScale;
                }
            }

            Debug.LogError("No mesh found!, will return default scale");
            return Vector3.one;
        }

        private Quaternion GetRealRotation(int batchID, DecorationObject decoration)
        {
            if (batchID < 0)
            {
                return decoration.Rotation;
            }

            var gameObject = m_Renderer.GetGameObject(decoration.ID);
            if (gameObject != null)
            {
                var filter = gameObject.GetComponentInChildren<MeshFilter>();
                if (filter != null)
                {
                    return filter.gameObject.transform.rotation;
                }
            }

            Debug.LogError("No mesh found!, will return default rotation");
            return Quaternion.identity;
        }

        private DecorationTagType GetDecorationObjectType(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return 0;
            }

            var type = TagToType(prefab.tag, out var ok);
            if (!ok)
            {
                Debug.LogError($"{assetPath}未知的Tag {prefab.tag}.导出为Obstacle物体");
            }
            return type;
        }

        private DecorationTagType TagToType(string tag, out bool ok)
        {
            ok = true;
            foreach (var kv in m_TagToType)
            {
                if (tag == kv.Key)
                {
                    return kv.Value;
                }
            }

            ok = false;
            return DecorationTagType.Obstacle;
        }

        private int CalculateBatchID(DecorationObject decoration)
        {
            if (!m_EnableInstanceRendering || !decoration.EnableInstanceRendering)
            {
                return -1;
            }

            var descriptor = decoration.ResourceDescriptor;
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
            m_AnimatorBatchDataPath = $"{World.GameFolder}/DecorationInstanceBatchInfo.asset";
            m_BatchDataPath = $"{World.GameFolder}/DecorationBatchInfo.asset";
            AssetDatabase.DeleteAsset(m_AnimatorBatchDataPath);
            AssetDatabase.DeleteAsset(m_BatchDataPath);

            if (m_EnableInstanceRendering)
            {
                m_ResourceGPUBatchID = new Dictionary<string, int>();
                m_InstanceAnimatorBatchInfoManager = ScriptableObject.CreateInstance<InstanceAnimatorBatchInfoRegistry>();
                m_BatchInfoRegistry = ScriptableObject.CreateInstance<GPUBatchInfoRegistry>();
            }
        }

        private void AfterGenerate()
        {
            if (m_EnableInstanceRendering)
            {
                AssetDatabase.CreateAsset(m_InstanceAnimatorBatchInfoManager, $"{World.GameFolder}/DecorationInstanceBatchInfo.asset");
                AssetDatabase.CreateAsset(m_BatchInfoRegistry, $"{World.GameFolder}/DecorationBatchInfo.asset");
            }
            m_ResourceGPUBatchID = null;
            m_BatchInfoRegistry = null;
            m_InstanceAnimatorBatchInfoManager = null;
        }

        private void GenerateDecorations(DecorationObject decoration, 
            GameObject decorationPrefab, 
            GameObject decorationGameObject, 
            List<DecorationObject> decorations)
        {
            if(decorationGameObject == null)
            {
                Debug.LogError($"Invalid decoration game object");
                return;
            }

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
                        var childDecoration = new DecorationObject(0, -1, true, decoration.LODLayerMask, childGameObject.transform.position, childGameObject.transform.rotation, childGameObject.transform.lossyScale, childDescriptor, decoration.Flags);
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
        private bool m_EnableInstanceRendering = true;
        private string m_AnimatorBatchDataPath;
        private string m_BatchDataPath;
        private Dictionary<string, DecorationTagType> m_TagToType = new()
        {
            {"Hideable", DecorationTagType.Hideable },
            {"Obstacle", DecorationTagType.Obstacle },
            {"HideableBefore", DecorationTagType.HideableBefore },
            {"HideableAfter", DecorationTagType.HideableAfter },
            {"ObstacleBefore", DecorationTagType.ObstacleBefore },
            {"ObstacleAfter", DecorationTagType.ObstacleAfter },
        };
        private const int m_RuntimeVersion = 3;
    }
}

