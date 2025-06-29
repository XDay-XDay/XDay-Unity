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

namespace XDay.GUIAPI
{
    public static class UIHelper
    {
        /// <summary>
        /// 将anchor position转换成local position,local position是以父节点的pivot为中心点的相对坐标, anchor position是以相对父节点的anchor为中心点的相对坐标
        /// </summary>
        /// <param name="anchorPosition">自己的anchor position</param>
        /// <param name="parent">父节点</param>
        /// <returns></returns>
        public static Vector2 AnchorToLocalPosition(Vector2 anchorPosition, RectTransform parent)
        {
            var pivotDelta = new Vector2(0.5f, 0.5f) - parent.pivot;
            anchorPosition.x += parent.rect.width * pivotDelta.x;
            anchorPosition.y += parent.rect.height * pivotDelta.y;
            return anchorPosition;
        }

        /// <summary>
        /// 让ui遮盖一个世界空间下的平面
        /// </summary>
        /// <param name="sceneCamera"></param>
        /// <param name="uiCamera"></param>
        /// <param name="coverUI"></param>
        public static void CoverWorldPlane(Camera sceneCamera, 
            Camera uiCamera, 
            RectTransform coverUI, 
            Vector3 worldMin, 
            Vector3 worldMax, 
            float worldZ)
        {
            if (coverUI != null)
            {
                worldMin.z = worldZ;
                worldMax.z = worldZ;

                Vector2 screenPointMin;
                Vector2 screenPointMax;
                if (uiCamera != null)
                {
                    var sl = sceneCamera.WorldToViewportPoint(worldMin);
                    var st = sceneCamera.WorldToViewportPoint(worldMax);
                    screenPointMin = uiCamera.ViewportToScreenPoint(sl);
                    screenPointMax = uiCamera.ViewportToScreenPoint(st);
                }
                else
                {
                    screenPointMin = sceneCamera.WorldToViewportPoint(worldMin);
                    screenPointMax = sceneCamera.WorldToViewportPoint(worldMax);
                }
                var parentTransform = coverUI.parent.transform as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentTransform, screenPointMin, uiCamera, out var localMin);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentTransform, screenPointMax, uiCamera, out var localMax);
                var size = localMax - localMin;
                coverUI.anchoredPosition = localMin + size * coverUI.pivot;
                coverUI.sizeDelta = size;
            }
        }
    }
}
