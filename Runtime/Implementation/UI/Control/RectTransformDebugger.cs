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
using UnityEngine.UI;

namespace XDay.UtilityAPI
{
    [AddComponentMenu("XDay/UI/Rect Transform Debugger", 0)]
    public class RectTransformDebugger : MonoBehaviour
    {
        public Rect Rect;
        public Rect ScreenRect;
        public float CanvasScaleFactor;
        public Vector2 SizeDelta;
        public Vector2 AnchoredPosition;
        public Vector3 AnchoredPosition3D;
        public Vector2 OffsetMin;
        public Vector2 OffsetMax;
        public Vector3 LossyScale;
        public Vector3 LocalPosition;
        public Vector3 Position;
        public Vector2 Pivot;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector3[] WorldCorners = new Vector3[4];
        //for scroll rect
        public Vector2 NormalizedScrollPosition;

        private void Update()
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }
            Rect = rectTransform.rect;
            SizeDelta = rectTransform.sizeDelta;
            AnchoredPosition = rectTransform.anchoredPosition;
            AnchoredPosition3D = rectTransform.anchoredPosition3D;
            rectTransform.GetWorldCorners(WorldCorners);
            OffsetMin = rectTransform.offsetMin;
            OffsetMax = rectTransform.offsetMax;
            LossyScale = rectTransform.lossyScale;
            LocalPosition = rectTransform.localPosition;
            Position = rectTransform.position;
            Pivot = rectTransform.pivot;
            AnchorMin = rectTransform.anchorMin;
            AnchorMax = rectTransform.anchorMax;

            var scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                NormalizedScrollPosition = scrollRect.normalizedPosition;
            }

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                CanvasScaleFactor = canvas.scaleFactor;
            }
            else
            {
                CanvasScaleFactor = 0;
            }

            ScreenRect = GetWorldRectInCanvas(rectTransform);
        }

        private Rect GetWorldRectInCanvas(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (!canvas) return Rect.zero;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Camera renderCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(renderCamera, corner);
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
    }
}
