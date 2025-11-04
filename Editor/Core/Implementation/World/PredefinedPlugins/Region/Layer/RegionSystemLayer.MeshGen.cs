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
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using static XDay.WorldAPI.Region.Editor.RegionSystem;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemLayer
    {
        public List<CurveRegionCreator> RegionCreatorsForLODs => m_CreatorsForLODs;
        public CurveRegionMeshGenerationParam CurveRegionMeshGenerationParam => m_CurveRegionMeshGenerationParam;
        public List<EditorRegionMeshGenerationParam> MeshGenerationParams => m_MeshGenerationParamForLODs;

        public void CreateAndGenerateOutlineAssets(string folder, int layerIdx, int lod,
            bool generateAssets, float layerWidth, float layerHeight,
            int horizontalTileCount, int verticalTileCount)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Generating Region Data", $"Generating...", 0);
                CreateOutline(lod, true);
                List<TerritoryCentroidInfo> centroidInfo = new List<TerritoryCentroidInfo>();
                GenerateOutlineAssets(folder, layerIdx, lod, true, generateAssets, layerWidth, layerHeight, horizontalTileCount, verticalTileCount, centroidInfo);
                EditorUtility.ClearProgressBar();

                for (int i = 0; i < centroidInfo.Count; ++i)
                {
                    var territory = GetRegion(centroidInfo[i].TerritoryID);
                    if (territory != null)
                    {
                        territory.Outline = centroidInfo[i].Outline;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
                EditorUtility.ClearProgressBar();
            }
        }

        public void CreateOutline(int lod, bool clearProgressBar)
        {
            m_CreatorsForLODs[lod]?.OnDestroy();
            m_CreatorsForLODs[lod] = new CurveRegionCreator(m_Renderer.Root.transform, Width, Height);
            List<CurveRegionCreator.RegionInput> regions = new List<CurveRegionCreator.RegionInput>();
            List<Task<CurveRegionCreator.RegionInput>> tasks = new List<Task<CurveRegionCreator.RegionInput>>();
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() => {
                    var coordinates = GetRegionCoordinates(m_Regions[idx].ID);
                    if (coordinates.Count > 0)
                    {
                        CurveRegionCreator.RegionInput regionInput = new CurveRegionCreator.RegionInput(m_Regions[idx].ID, coordinates);
                        return regionInput;
                    }
                    return null;
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            for (int i = 0; i < tasks.Count; ++i)
            {
                var regionInput = tasks[i].Result;
                if (regionInput != null)
                {
                    regions.Add(regionInput);
                }
            }

            var param = m_CurveRegionMeshGenerationParam.LODParams[lod];
            CurveRegionCreator.SettingInput settings = new CurveRegionCreator.SettingInput(param.PointDeltaDistance, param.SegmentLengthRatio, param.MinTangentLength, param.MaxTangentLength, param.MaxPointCountInOneSegment, param.MoreRectangular, param.LineWidth, m_CurveRegionMeshGenerationParam.VertexDisplayRadius, param.TextureAspectRatio, m_CurveRegionMeshGenerationParam.SegmentLengthRatioRandomRange, m_CurveRegionMeshGenerationParam.TangentRotationRandomRange, param.GridErrorThreshold, param.EdgeMaterial, param.RegionMaterial, param.UseVertexColorForRegionMesh, param.CombineMesh, param.MergeEdge, param.EdgeHeight, param.ShareEdge, param.CombineAllEdgesOfOneRegion, System.GenerateUnityAssets);

            CurveRegionCreator.Input input = new CurveRegionCreator.Input(regions, m_GridWidth, m_HorizontalGridCount, m_VerticalGridCount, GetGridData, CoordinateToPosition, PositionToCoordinate, GetRegionColor, GetRegionCenter, settings);
            m_CreatorsForLODs[lod].Create(input, true);

            if (clearProgressBar)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public Color GetRegionColor(int id)
        {
            var t = GetRegion(id);
            if (t != null)
            {
                return t.Color;
            }
            return Color.white;
        }

        public string GetTerritoryAssetPath(string exportFolder, int layer, int territoryID, int lod)
        {
            string assetFolder = $"{exportFolder}/{RegionDefine.REGION_SYSTEM_RUNTIME_ASSETS_FOLDER_NAME}/layer{layer}";
            return $"{assetFolder}/region_{territoryID}_lod{lod}.prefab";
        }

        public void GenerateOutlineAssets(string folder, int layer, int lod, bool displayProgressBar, bool generateAssets, float layerWidth, float layerHeight, int horizontalTileCount, int verticalTileCount, List<TerritoryCentroidInfo> outCentroidInfo)
        {
            if (m_CreatorsForLODs[lod] != null)
            {
                string assetFolder = $"{folder}/{RegionDefine.REGION_SYSTEM_RUNTIME_ASSETS_FOLDER_NAME}/layer{layer}";
                if (generateAssets)
                {
                    if (lod == 0)
                    {
                        FileUtil.DeleteFileOrDirectory(assetFolder);
                    }

                    Helper.CreateDirectory(assetFolder);
                    AssetDatabase.Refresh();
                }

                m_CreatorsForLODs[lod].Generate(assetFolder, lod, displayProgressBar, generateAssets, layerWidth, layerHeight, horizontalTileCount, verticalTileCount, layer, outCentroidInfo);
            }
        }

        public void HideLineAndMesh(int lod)
        {
            m_CreatorsForLODs[lod]?.HideLineAndMesh();
        }

        public void HideLine(int lod)
        {
            m_CreatorsForLODs[lod]?.HideLine();
        }

        public void HideMesh(int lod)
        {
            m_CreatorsForLODs[lod]?.HideMesh();
        }

        public void HideRegionMesh(int lod)
        {
            m_CreatorsForLODs[lod]?.HideRegionMesh();
        }

        public void ShowMesh(int lod)
        {
            m_CreatorsForLODs[lod]?.ShowMesh();
        }

        private CurveRegionMeshGenerationLODParam GetDefaultLODParam(bool isLOD0)
        {
            CurveRegionMeshGenerationLODParam param = null;
            if (isLOD0)
            {
                param = new CurveRegionMeshGenerationLODParam(0, segmentLengthRatio: 0.3f, minTangentLength: m_GridWidth * 0.1f, maxTangentLength: m_GridWidth * 0.6f, pointDeltaDistance: m_GridWidth * 0.1f, maxPointCountInOneSegment: 4, moreRectangular: false, lineWidth: m_GridWidth * 0.5f, textureAspectRatio: 2, gridErrorThreshold: 10, edgeMaterial: null, regionMaterial: null, useVertexColorForRegionMesh: false, combineMesh: false, mergeEdge: true, edgeHeight: 1, shareEdge: true, combineAllEdgesOfOneRegion: false);
            }
            else
            {
                param = new CurveRegionMeshGenerationLODParam(1, segmentLengthRatio: 0.3f, minTangentLength: m_GridWidth * 0.1f, maxTangentLength: m_GridWidth * 0.6f, pointDeltaDistance: m_GridWidth * 0.1f, maxPointCountInOneSegment: 3, moreRectangular: false, lineWidth: m_GridWidth * 0.5f, textureAspectRatio: 2, gridErrorThreshold: 10, edgeMaterial: null, regionMaterial: null, useVertexColorForRegionMesh: false, combineMesh: true, mergeEdge: true, edgeHeight: 1, shareEdge: true, combineAllEdgesOfOneRegion: false);
            }
            return param;
        }

        public EditorRegionMeshGenerationParam GetMeshGenerationParam(int lod)
        {
            if (lod >= 0 && lod < m_MeshGenerationParamForLODs.Count)
            {
                return m_MeshGenerationParamForLODs[lod];
            }
            return null;
        }

        internal short GetRegionIndex(int id)
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                if (id == m_Regions[i].ID)
                {
                    return (short)i;
                }
            }
            return -1;
        }

        private void CreateDefaultParams()
        {
            EditorRegionMeshGenerationParam meshGenerationParam = new EditorRegionMeshGenerationParam(4, 0.3f, 10, true, "");
            m_MeshGenerationParamForLODs.Add(meshGenerationParam);
            
            float vertexDisplayRadius = 0.5f;
            float segmentLengthRatioRandomRange = 0.1f;
            float tangentRotationRandomRange = 20f;
            var paramLOD0 = GetDefaultLODParam(true);
            var paramLOD1 = GetDefaultLODParam(false);
            m_CurveRegionMeshGenerationParam = new CurveRegionMeshGenerationParam(vertexDisplayRadius, segmentLengthRatioRandomRange, tangentRotationRandomRange, new List<CurveRegionMeshGenerationLODParam>() { paramLOD0, paramLOD1 });
        }

        //public void RemoveInvalidGrids()
        //{
        //    StopWatchWrapper w = new StopWatchWrapper();
        //    BeginPainting();
        //    EditorUtility.DisplayProgressBar("Removing Invalid Grids", $"Removing...", 0);

        //    w.Start();
        //    Dictionary<int, EditorTerritory> territories = new Dictionary<int, EditorTerritory>();
        //    for (int i = 0; i < mTerritories.Count; ++i)
        //    {
        //        territories[mTerritories[i].id] = mTerritories[i];
        //    }

        //    //填充格子
        //    int gridTileIdx;
        //    for (int i = 0; i < mVerticalGridCount; ++i)
        //    {
        //        for (int j = 0; j < mHorizontalGridCount; ++j)
        //        {
        //            gridTileIdx = i * mHorizontalGridCount + j;
        //            var type = mGrids[gridTileIdx];
        //            if (type != 0)
        //            {
        //                for (int k = 0; k < 4; ++k)
        //                {
        //                    int nx = mSlopeNeighbours[k].x + j;
        //                    int ny = mSlopeNeighbours[k].y + i;
        //                    if (nx >= 0 && nx < mHorizontalGridCount &&
        //                        ny >= 0 && ny < mVerticalGridCount)
        //                    {
        //                        if (mGrids[ny * mHorizontalGridCount + nx] == type &&
        //                            (mGrids[ny * mHorizontalGridCount + j] != type && mGrids[i * mHorizontalGridCount + nx] != type))
        //                        {
        //                            SetGridData(j, ny, type);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    var t0 = w.Stop();
        //    Debug.Log($"RemoveInvalidGrids step 0 cost: {t0} seconds");

        //    w.Start();
        //    bool[] visitedTiles = new bool[mVerticalGridCount * mHorizontalGridCount];
        //    List<Vector2Int> connectedTiles = new List<Vector2Int>();
        //    for (int i = 0; i < mVerticalGridCount; ++i)
        //    {
        //        for (int j = 0; j < mHorizontalGridCount; ++j)
        //        {
        //            gridTileIdx = i * mHorizontalGridCount + j;
        //            var type = mGrids[gridTileIdx];
        //            if (type != 0)
        //            {
        //                if (visitedTiles[i * mHorizontalGridCount + j] == false)
        //                {
        //                    GetCollectedTiles(j, i, connectedTiles, visitedTiles);
        //                    if (connectedTiles.Count == 1)
        //                    {
        //                        int validNeighbourID = GetValidNeighbourID(j, i);
        //                        Debug.Log($"remove invalid tiles to (x:{j},y:{i}) {validNeighbourID}");
        //                        SetGridData(j, i, validNeighbourID);
        //                        var obj = new GameObject($"invalid grid (x:{j},y:{i})");
        //                        obj.transform.position = FromCoordinateToPositionCenter(j, i);
        //                    }
        //                    else if (connectedTiles.Count < 10)
        //                    {
        //                        Debug.LogError($"区域层第{mLayer.GetSubLayerIndex(this)}个子层有无效的格子 (x:{j},y:{i})");
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    var t1 = w.Stop();
        //    Debug.Log($"RemoveInvalidGrids step 1 cost: {t1} seconds");

        //    RefreshTexture();

        //    EditorUtility.ClearProgressBar();

        //    var dirtyRange = EndPainting();
        //    if (!dirtyRange.IsEmpty())
        //    {
        //        var action = new ActionSaveRegionData();
        //        action.Init(mLayer.id, mLayer.GetSubLayerIndex(this), dirtyRange.minX, dirtyRange.minY, dirtyRange.maxX, dirtyRange.maxY, grids, gridsCopy);
        //        ActionManager.instance.PushAction(action, true, false);
        //    }
        //}

        private void InitEdges()
        {
            for (int i = 0; i < m_CurveRegionMeshGenerationParam.LODParams.Count; ++i)
            {
                m_CreatorsForLODs.Add(new CurveRegionCreator(m_Renderer.Root.transform, Width, Height));
            }
            List<CurveRegionCreator.EdgeAssetInfo> edges = new();
            for (int i = 0; i < m_SharedEdges.Length; ++i)
            {
                var data = m_SharedEdges[i];
                var edge = new CurveRegionCreator.EdgeAssetInfo(data.TerritoryID, data.NeighbourTerritoryID, data.PrefabPath, data.Material);
                edges.Add(edge);
            }
            //only need lod0 edge assets info
            m_CreatorsForLODs[0].EdgeAssetsInfo = edges;
            if (m_CreatorsForLODs.Count > 1)
            {
                //List<CurveRegionCreator.Block> blocks = new List<CurveRegionCreator.Block>();
                //for (int i = 0; i < subLayerData.blocks.Length; ++i)
                //{
                //    var data = subLayerData.blocks[i];
                //    var block = new CurveRegionCreator.Block();
                //    block.bounds = data.bounds;
                //    block.prefabPath = data.prefabPath;
                //    block.edges = new List<CurveRegionCreator.BlockEdge>();
                //    foreach (var edge in data.edges)
                //    {
                //        var blockEdge = new CurveRegionCreator.BlockEdge();
                //        blockEdge.territoryID = edge.territoryID;
                //        blockEdge.neighbourTerritoryID = edge.neighbourTerritoryID;
                //        block.edges.Add(blockEdge);
                //    }
                //    block.regions = new List<CurveRegionCreator.BlockRegion>();
                //    foreach (var region in data.regions)
                //    {
                //        var blockRegion = new CurveRegionCreator.BlockRegion();
                //        blockRegion.territoryID = region.territoryID;
                //        block.regions.Add(blockRegion);
                //    }
                //    blocks.Add(block);
                //}
                ////只是为了导出时能有数据
                //m_CreatorsForLODs[1].blocks = blocks;
                //m_CreatorsForLODs[1].lod1MaskTextureWidth = subLayerData.maskTextureWidth;
                //m_CreatorsForLODs[1].lod1MaskTextureHeight = subLayerData.maskTextureHeight;
                //m_CreatorsForLODs[1].lod1MaskTextureData = subLayerData.maskTextureData;
            }
        }

        [SerializeField]
        private CurveRegionMeshGenerationParam m_CurveRegionMeshGenerationParam = new();
        [SerializeField]
        private List<EditorRegionMeshGenerationParam> m_MeshGenerationParamForLODs = new();
        private Dictionary<Vector3, BorderEdge> m_StartPosToEdge = new();
        private TerritorySharedEdgeInfo[] m_SharedEdges = new TerritorySharedEdgeInfo[0];
        private List<RegionPreview> m_PreviewGameObjects = new();
        private List<CurveRegionCreator> m_CreatorsForLODs = new();
    }
}

