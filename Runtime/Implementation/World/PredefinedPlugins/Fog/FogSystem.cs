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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace XDay.WorldAPI.FOW
{
    [Preserve]
    internal partial class FogSystem : WorldPlugin
    {
        public event Action<bool> EventFogStateChange
        {
            add => m_Impl.EventFogStateChange += value;
            remove => m_Impl.EventFogStateChange -= value;
        }
        public int HorizontalResolution => m_Impl.HorizontalResolution;
        public int VerticalResolution => m_Impl.VerticalResolution;
        public float GridWidth => m_GridWidth;
        public float GridHeight => m_GridHeight;
        public Vector3 Origin => m_Origin;
        public override string Name { set => throw new System.NotImplementedException(); get => m_Name; }
        public override List<string> GameFileNames => new() { "fog" };
        public override string TypeName => "FogSystem";
        public string FogPrefabPath => m_FogPrefabPath;
        public string FogConfigPath => m_FogConfigPath;
        public string BlurShaderPath => m_BlurShaderPath;

        public FogSystem()
        {
        }

        protected override void InitInternal()
        {
            m_Impl = new FogSystemImpl(this, m_HorizontalResolution, m_VerticalResolution, m_Data);
            m_Data = null;
            m_Renderer = new FogSystemRenderer(this);
        }

        protected override void UninitInternal()
        {
            m_Impl.OnDestroy();
        }

        public void ResetFog()
        {
            m_Impl.ResetFog();
        }

        public void BeginBatchOpen()
        {
            m_Impl.BeginBatchOpen();
        }

        public void EndBatchOpen()
        {
            m_Impl.EndBatchOpen();
        }

        public void OpenCircle(FogDataType type, int minX, int minY, int maxX, int maxY, bool inner)
        {
            m_Impl.OpenCircle(type, minX, minY, maxX, maxY, inner);
        }

        public void OpenRectangle(FogDataType type, int minX, int minY, int maxX, int maxY)
        {
            m_Impl.OpenRectangle(type, minX, minY, maxX, maxY);
        }

        public bool IsOpen(int x, int y)
        {
            return m_Impl.IsOpen(x, y);
        }

        public bool IsUnlocked(int x, int y)
        {
            return m_Impl.IsUnlocked(x, y);
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var deserializer = world.QueryGameDataDeserializer(world.ID, $"fog@{pluginName}");

            deserializer.ReadInt32("Fog.Version");

            m_Name = deserializer.ReadString("Name");

            m_HorizontalResolution = deserializer.ReadInt32("Horizontal Grid Count");
            m_VerticalResolution = deserializer.ReadInt32("Vertical Grid Count");
            m_GridWidth = deserializer.ReadSingle("Grid Width");
            m_GridHeight = deserializer.ReadSingle("Grid Height");
            m_Origin = deserializer.ReadVector2("Origin");
            m_Data = deserializer.ReadByteArray("Data");
            m_FogPrefabPath = deserializer.ReadString("Fog Prefab");
            m_FogConfigPath = deserializer.ReadString("Fog Config");
            m_BlurShaderPath = deserializer.ReadString("Fog Blur Shader");

            deserializer.Uninit();
        }

        private IFogSystemImpl m_Impl;
        private FogSystemRenderer m_Renderer;
        private int m_HorizontalResolution;
        private int m_VerticalResolution;
        private float m_GridWidth;
        private float m_GridHeight;
        private Vector3 m_Origin;
        private byte[] m_Data;
        private string m_Name;
        private string m_FogPrefabPath;
        private string m_FogConfigPath;
        private string m_BlurShaderPath;
    }
}
