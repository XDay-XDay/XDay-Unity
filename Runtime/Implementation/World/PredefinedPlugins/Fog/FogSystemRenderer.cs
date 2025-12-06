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

namespace XDay.WorldAPI.Fog
{
    public interface IFogRenderer
    {
        void OnDestroy();
        void UpdateMask(bool reset);
        void Update(float dt);
    }

    public enum FogType
    {
        SLG,
        RTS,
    }

    internal partial class FogSystemRenderer
    {
        public FogSystemRenderer(FogSystem fog)
        {
            m_Fog = fog;

            m_RenderFog = m_Fog.HorizontalResolution > 0;

            if (m_RenderFog)
            {
                if (fog.FogType == FogType.SLG)
                {
                    m_Renderer = new FogRenderer("fog of war", null, m_Fog.HorizontalResolution, m_Fog.VerticalResolution, m_Fog.GridWidth, m_Fog.GridHeight, m_Fog.Origin, fog.World.AssetLoader, m_Fog.FogPrefabPath, m_Fog.FogConfigPath, m_Fog.BlurShaderPath, m_Fog.IsOpen);
                }
                else
                {
                    m_Renderer = new FogOfWarRenderer("fog of war", null, m_Fog.HorizontalResolution, m_Fog.VerticalResolution, m_Fog.GridWidth, m_Fog.GridHeight, m_Fog.Origin, fog.World.AssetLoader, m_Fog.FogPrefabPath, m_Fog.BlurShaderPath, m_Fog.IsOpen);
                }

                m_Fog.EventFogStateChange += OnFogStateChange;
            }
        }

        public void OnDestroy()
        {
            if (m_RenderFog)
            {
                m_Fog.EventFogStateChange -= OnFogStateChange;

                m_Renderer.OnDestroy();
            }
        }

        private void OnFogStateChange(bool reset)
        {
            m_Renderer.UpdateMask(reset);
        }

        public void Update(float dt)
        {
            if (!m_RenderFog)
            {
                return;
            }

            m_Renderer.Update(dt);
        }

        private readonly FogSystem m_Fog;
        private readonly IFogRenderer m_Renderer;
        private readonly bool m_RenderFog;
    }
}