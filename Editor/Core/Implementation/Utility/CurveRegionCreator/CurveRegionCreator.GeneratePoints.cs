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
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public class TerritoryCentroidInfo
    {
        public int TerritoryID;
        public Vector3 Centroid;
        public bool IsInPolygon;
        public List<Vector3> Outline;
    }

    public partial class CurveRegionCreator
    {
        //生成中心点
        public void GenerateTerritoryCentroid(List<TerritoryCentroidInfo> outTerritoryPoints)
        {
            outTerritoryPoints.Clear();
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                var info = new TerritoryCentroidInfo();
                info.Centroid = Helper.CalculatePolygonCenter(m_Territories[i].Outline);
                info.IsInPolygon = Helper.PointInPolygon2D(info.Centroid, m_Territories[i].Outline);
                info.TerritoryID = m_Territories[i].RegionID;
                info.Outline = new List<Vector3>();
                info.Outline.AddRange(m_Territories[i].Outline);
                outTerritoryPoints.Add(info);
            }
        }

        //生成自定义点
        //public void GenerateCustomPoints(int bindingID, int subLayerIndex, RegionPointGenerater pointGenerator, List<TerritoryCentroidInfo> outTerritoryPoints)
        //{
        //    outTerritoryPoints.Clear();
        //    for (int i = 0; i < m_Territories.Count; ++i)
        //    {
        //        var centroid = EditorUtils.CalculatePolygonCenter(m_Territories[i].outline);
        //        var bounds = EditorUtils.CalculateBounds(m_Territories[i].coordinates);
                
        //        var points = pointGenerator.GeneratePoint(bindingID, centroid, m_Territories[i].regionID, m_Territories[i].coordinates, m_Territories[i].outline, new Vector2Int(bounds.minX, bounds.minY), new Vector2Int(bounds.maxX, bounds.maxY), subLayerIndex);
        //        for (int k = 0; k < points.Count; ++k)
        //        {
        //            var info = new TerritoryCentroidInfo();
        //            info.territoryID = m_Territories[i].regionID;
        //            info.outline = new List<Vector3>();
        //            info.outline.AddRange(m_Territories[i].outline);
        //            info.centroid = points[k];
        //            info.isInPolygon = GeometricAlgorithm.PointInPolygon2D(points[k], info.outline);
        //            outTerritoryPoints.Add(info);
        //        }
        //    }
        //}
    }
}

