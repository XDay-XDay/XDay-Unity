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

namespace XDay.UtilityAPI.Editor
{
    public class VirtualViewer
    {
        public VirtualViewer(float worldWidth, float worldHeight)
        {
            m_WorldWidth = worldWidth;
            m_WorldHeight = worldHeight;
            m_Zoom = 1.0f;
        }

        public void SetFrustum(float height, float ratio)
        {
            m_AspectRatio = ratio;
            m_FrustumHeight = height;
            m_FrustumWidth = m_FrustumHeight * m_AspectRatio;
        }

        public void ResetPosition()
        {
            m_AnchorWorldPositionX = 0;
            m_AnchorWorldPositionY = 0;
        }

        public void Reset()
        {
            ResetPosition();
            m_Zoom = 1.0f;
        }

        public void SetWorldPosition(float worldX, float worldY)
        {
            m_AnchorWorldPositionX = worldX;
            m_AnchorWorldPositionY = worldY;
        }

        public void Move(float x, float y)
        {
            float frustumWidth = m_FrustumWidth * m_Zoom;
            float frustumHeight = m_FrustumHeight * m_Zoom;

            m_AnchorWorldPositionX += x * m_Zoom;
            m_AnchorWorldPositionY += y * m_Zoom;

            m_AnchorWorldPositionX = Mathf.Clamp(m_AnchorWorldPositionX, -m_WorldWidth * 0.5f, m_WorldWidth * 0.5f - frustumWidth);
            m_AnchorWorldPositionY = Mathf.Clamp(m_AnchorWorldPositionY, -m_WorldHeight * 0.5f, m_WorldHeight * 0.5f - frustumHeight);
        }

        public void Zoom(float delta, float windowWidth, float windowHeight, Vector2 fixedWindowPos)
        {
            WindowToWorld(windowWidth, windowHeight, fixedWindowPos, out float worldPosOldX, out float worldPosOldY);
            float frustumWidth = m_FrustumHeight * m_AspectRatio * m_Zoom;
            float frustumHeight = m_FrustumHeight * m_Zoom;
            float rx = (worldPosOldX - m_AnchorWorldPositionX) / frustumWidth;
            float ry = (worldPosOldY - m_AnchorWorldPositionY) / frustumHeight;

            m_Zoom += delta;
            m_Zoom = Mathf.Clamp(m_Zoom, m_MinZoom, m_MaxZoom);

            float newFrustumWidth = m_FrustumHeight * m_AspectRatio * m_Zoom;
            float newFrustumHeight = m_FrustumHeight * m_Zoom;
            m_AnchorWorldPositionX = worldPosOldX - rx * newFrustumWidth;
            m_AnchorWorldPositionY = worldPosOldY - ry * newFrustumHeight;
        }

        public void WorldToWindow(float windowWidth, float windowHeight, Vector2 worldPos, out float x, out float y)
        {
            float frustumWidth = m_FrustumHeight * m_AspectRatio * m_Zoom;
            float frustumHeight = m_FrustumHeight * m_Zoom;
            float deltaX = worldPos.x - m_AnchorWorldPositionX;
            float deltaY = worldPos.y - m_AnchorWorldPositionY;
            //convert to [0,1]
            float rx = deltaX / frustumWidth;
            float ry = deltaY / frustumHeight;
            x = rx * windowWidth;
            y = (1 - ry) * windowHeight;
        }

        public void WindowToWorld(float windowWidth, float windowHeight, Vector2 windowPos, out float x, out float y)
        {
            float rx = windowPos.x / windowWidth;
            float ry = windowPos.y / windowHeight;
            float frustumWidth = m_FrustumHeight * m_AspectRatio * m_Zoom;
            float frustumHeight = m_FrustumHeight * m_Zoom;
            x = m_AnchorWorldPositionX + rx * frustumWidth;
            y = m_AnchorWorldPositionY + (1 - ry) * frustumHeight;
        }

        public float GetWorldWidth() { return m_WorldWidth; }
        public float GetWorldHeight() { return m_WorldHeight; }
        public float GetZoom() { return m_Zoom; }
        public float GetMaxZoom() { return m_MaxZoom; }
        public float GetMinZoom() { return m_MinZoom; }

        public void SetZoomRange(float maxZoom, float minZoom)
        {
            m_MaxZoom = maxZoom;
            m_MinZoom = minZoom;
        }

        float m_FrustumHeight = 0;
        float m_FrustumWidth = 0;
        float m_AspectRatio = 0;
        float m_WorldWidth;
        float m_WorldHeight;
        float m_Zoom = 1.0f;
        float m_MaxZoom = 5.0f;
        float m_MinZoom = 0.5f;
        float m_AnchorWorldPositionX;
        float m_AnchorWorldPositionY;
    };
}

