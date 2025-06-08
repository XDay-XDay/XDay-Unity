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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.House
{
    internal class HouseRenderer
    {
        public HouseRenderer(GameObject obj, HouseData house)
        {
            m_Root = obj;
            obj.transform.SetParent(m_Root.transform, false);
            obj.transform.position = house.Position;
            var dp = obj.AddComponent<DrawBounds>();
            dp.Bounds = house.WorldBounds;
            dp.Color = Color.yellow;

            foreach (var interactivePoint in house.InteractivePoints)
            {
                var startPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                startPoint.name = $"{house.Name} start";
                startPoint.transform.SetParent(obj.transform);
                startPoint.transform.SetPositionAndRotation(interactivePoint.StartPosition, interactivePoint.StartRotation);
                var endPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                endPoint.name = $"{house.Name} end";
                endPoint.transform.SetParent(obj.transform);
                endPoint.transform.SetPositionAndRotation(interactivePoint.EndPosition, interactivePoint.EndRotation);
            }
             
            m_WalkableStateDebugger = new WalkableStateDebugger(house.Name + "walkable", house.HorizontalGridCount, house.VerticalGridCount, house.GridSize, house.GridHeight, house.Walkable);
            m_WalkableStateDebugger.Initialize(m_Root.transform, new Vector3(0, house.GridHeight, 0));
            m_WalkableStateDebugger.IsActive = true;
        }

        public void OnDestroy()
        {
            m_WalkableStateDebugger.OnDestroy();
            Helper.DestroyUnityObject(m_Root);
        }

        private GameObject m_Root;
        private WalkableStateDebugger m_WalkableStateDebugger;
    }
}
