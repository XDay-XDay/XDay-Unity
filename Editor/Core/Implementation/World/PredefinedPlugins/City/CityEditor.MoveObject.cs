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
using UnityEngine;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    partial class CityEditor
    {
        void MoveObjects()
        {
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.BoolParameter("建筑", "", true),
                new ParameterWindow.BoolParameter("交互点", "", true),
                new ParameterWindow.Vector2IntParameter("移动格子偏移", "", Vector2Int.one),
            };
            ParameterWindow.Open("移动物体", input, (List<ParameterWindow.Parameter> items) =>
            {
                var ok = ParameterWindow.GetBool(items[0], out var moveBuilding);
                ok &= ParameterWindow.GetBool(items[1], out var moveInteractivePoint);
                ok &= ParameterWindow.GetVector2Int(items[2], out var offset);
                if (ok)
                {
                    if (moveBuilding)
                    {
                        MoveBuildings(offset);
                    }

                    if (moveInteractivePoint)
                    {
                        MoveInteractivePoints(offset);
                    }
                }
                return false;
            });
        }

        void MoveBuildings(Vector2Int offset)
        {
            var grid = GetSelectedGrid();

            grid.MoveBuildings(offset);
        }

        void MoveInteractivePoints(Vector2Int offset)
        {
            var grid = GetSelectedGrid();

            grid.MoveInteractivePoints(offset);
        }
    }
}
