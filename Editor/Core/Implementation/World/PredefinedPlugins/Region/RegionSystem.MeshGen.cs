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


namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        //public EditorRegionMeshGenerationParam GetMeshGenerationParam(int layer, int lod)
        //{
        //    return m_Layers[layer].GetMeshGenerationParam(lod);
        //}

        public void OnLODCountChanged(int oldLODCount, int newLODCount)
        {
            //for (int i = 0; i < m_Layers.Count; ++i)
            //{
            //    m_Layers[i].OnLODCountChanged(oldLODCount, newLODCount);
            //}
        }

        //public List<EditorRegionMeshGenerationParam> GetMeshGenerationParams(int layer)
        //{
        //    return m_Layers[layer].MeshGenerationParams;
        //}

        //public CurveRegionMeshGenerationParam GetCurveRegionMeshGenerationParam(int layer)
        //{
        //    return m_Layers[layer].CurveRegionMeshGenerationParam;
        //}

        //public void CreateOutline(int idx, int lod)
        //{
        //    var layer = GetLayerAt(idx);
        //    if (layer != null)
        //    {
        //        layer.CreateOutline(lod, false);
        //    }
        //}

        //public void GenerateOutlineAssets(int subLayerIndex, string folder, int lod, bool generateAssets, float layerWidth, float layerHeight, int horizontalTileCount, int verticalTileCount, List<TerritoryCentroidInfo> outCentroidInfo)
        //{
        //    var layer = GetLayerAt(subLayerIndex);
        //    if (layer != null)
        //    {
        //        layer.GenerateOutlineAssets(folder, subLayerIndex, lod, false, generateAssets, layerWidth, layerHeight, horizontalTileCount, verticalTileCount, outCentroidInfo);
        //    }
        //}

        //public void HideLineAndMesh(int idx, int lod)
        //{
        //    var layer = GetLayerAt(idx);
        //    if (layer != null)
        //    {
        //        layer.HideLineAndMesh(lod);
        //    }
        //}

        //public void HideLine(int idx, int lod)
        //{
        //    var layer = GetLayerAt(idx);
        //    if (layer != null)
        //    {
        //        layer.HideLine(lod);
        //    }
        //}

        //public void HideMesh(int idx, int lod)
        //{
        //    var layer = GetLayerAt(idx);
        //    if (layer != null)
        //    {
        //        layer.HideMesh(lod);
        //    }
        //}

        //public void HideRegionMesh(int idx, int lod)
        //{
        //    var layer = GetLayerAt(idx);
        //    if (layer != null)
        //    {
        //        layer.HideRegionMesh(lod);
        //    }
        //}

        //public void ShowMesh(int idx, int lod)
        //{
        //    var layer = GetLayerAt(idx);
        //    if (layer != null)
        //    {
        //        layer.ShowMesh(lod);
        //    }
        //}

        //public void GeneratePrefabs(int bindingID, string exportFolder)
        //{
        //    var layer = GetCurrentLayer();
        //    if (layer == null)
        //    {
        //        return;
        //    }

        //    if (!string.IsNullOrEmpty(exportFolder))
        //    {
        //        //layer.RemoveInvalidGrids();
        //        int nLODs = LODCount;
        //        for (int i = 0; i < nLODs; ++i)
        //        {
        //            layer.CreateAndGenerateOutlineAssets(exportFolder, GetLayerIndex(layer.ID), i, true, layer.Width, layer.Height, 3, 3);
        //        }
        //        EditorUtility.DisplayProgressBar("", "Saving And Exporting Map...", 0.5f);
        //        WorldEditor.GameSerialize();
        //        EditorUtility.ClearProgressBar();
        //    }
        //    else
        //    {
        //        EditorUtility.DisplayDialog("Error", "Invalid export folder", "OK");
        //    }
        //}

        //public void PreviewGeneratedData(string exportFolder, int layerIndex, int lod)
        //{
        //    var layer = GetLayerAt(layerIndex);
        //    layer.CreateAndGenerateOutlineAssets(exportFolder, layerIndex, lod, false, layer.Width, layer.Height, 3, 3);
        //}

        //public void GenerateAssets(string folder)
        //{
        //    if (string.IsNullOrEmpty(folder))
        //    {
        //        Debug.LogError("Invalid export folder!");
        //        return;
        //    }

        //    string assetFolder = $"{folder}/{RegionDefine.REGION_SYSTEM_RUNTIME_ASSETS_FOLDER_NAME}";
        //    FileUtil.DeleteFileOrDirectory(assetFolder);
        //    Helper.CreateDirectory(assetFolder);
        //    AssetDatabase.Refresh();

        //    for (int i = 0; i < m_Layers.Count; ++i)
        //    {
        //        string layerAssetFolder = $"{assetFolder}/layer{i}";
        //        Helper.CreateDirectory(layerAssetFolder);
        //        AssetDatabase.Refresh();
        //        m_Layers[i].GenerateAssets(layerAssetFolder);
        //    }
        //}
    }
}
