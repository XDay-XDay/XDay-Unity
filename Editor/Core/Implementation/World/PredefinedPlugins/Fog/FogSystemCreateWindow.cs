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

using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.FOW.Editor
{
    public class FogSystemCreateWindow : GridBasedWorldPluginCreateWindow
    {
        protected override string DisplayName => "Fog Layer";
        protected override bool SetGridCount => true;

        protected override void CreateInternal()
        {
            var createInfo = new FogSystem.CreateInfo
            {
                ID = World.AllocateObjectID(),
                ObjectIndex = World.PluginCount,
                HorizontalGridCount = m_GridCountX,
                VerticalGridCount = m_GridCountY,
                Origin = CalculateOrigin(m_Width, m_Height),
                Name = DisplayName,
                GridWidth = m_Width / m_GridCountX,
                GridHeight = m_Height / m_GridCountY,
                MaxGridCountPerBlock = 1024,
                Layer0ID = World.AllocateObjectID(),
            };

            var system = new FogSystem(createInfo);
            UndoSystem.CreateObject(system, World.ID, "Create Fog System");
        }
    }
}