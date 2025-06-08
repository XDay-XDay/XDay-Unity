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

namespace XDay.WorldAPI.City.Editor
{
    internal partial class Grid
    {
        /// <summary>
        /// 计算每个格子可行走的高度,如果建筑在地下，以建筑地板高度计算
        /// </summary>
        /// <returns></returns>
        public float[] CalculateGridHeights()
        {
            var gridHeights = new float[m_VerticalGridCount * m_HorizontalGridCount];

            for (var i = 0; i < m_VerticalGridCount; ++i)
            {
                for (var j = 0; j < m_HorizontalGridCount; ++j)
                {
                    var idx = i * m_HorizontalGridCount + j;
                    gridHeights[idx] = GetGridHeight(j, i);
                }
            }

            return gridHeights;
        }

        public float GetGridHeight(int x, int y)
        {
            var buildingWalkableHeight = GetBuildingHeight(x, y, out var valid);
            if (valid)
            {
                return buildingWalkableHeight;
            }

            return GetGroundHeight(x, y);
        }

        private float GetBuildingHeight(int x, int y, out bool valid)
        {
            valid = false;
            var layer = GetLayer<ObjectLayer>();
            var objID = layer.GetObjectID(x, y);
            if (objID == 0)
            {
                return 0;
            }
            valid = true;
            var building = GetBuildingInstance(objID);
            var groundHeight = building.SnapToGround(building.Position).y;
            var room = building.Template.RoomPrefab;
            return groundHeight + room.FacilityLocalY;
        }

        public float GetGroundHeight(int x, int y)
        {
            var src = CoordinateToGridCenterPosition(x, y);
            var pos = src;
            pos.y = 500;

            var layer = LayerMask.NameToLayer("Mask Object");

            var hit = Physics.Raycast(pos, Vector3.down, out var hitInfo, float.MaxValue, 1 << layer);
            if (hit)
            {
                src = new Vector3(pos.x, hitInfo.point.y, pos.z);
            }

            return src.y;
        }
    }
}
