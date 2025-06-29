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
using UnityEngine.Scripting;
using XDay.NavigationAPI;

namespace XDay.WorldAPI.Navigation
{
    [Preserve]
    internal partial class NavigationPlugin : WorldPlugin, INavigationPlugin
    {
        public override List<string> GameFileNames => new() { "navigation" };
        public override string Name { set => throw new System.NotImplementedException(); get => m_Name; }
        public override string TypeName => "NavigationPlugin";
        public ushort[] MeshTriangleAreaIDs => m_MeshTriangleAreaIDs;
        public Vector3[] MeshVertices => m_MeshVertices;
        public int[] MeshIndices => m_MeshIndices;
        public Color[] AreaColors => m_AreaColors;
        public bool EnableDepthTest => m_EnableDepthTest;

        public NavigationPlugin()
        {
        }

        protected override void InitInternal()
        {
            m_NavigationSystem.Init(m_MeshVertices, m_MeshIndices);

            InitRendererInternal();
        }

        protected override void InitRendererInternal()
        {
            if (m_Renderer != null)
            {
                return;
            }

            m_Renderer = new(this);
        }

        protected override void UninitRendererInternal()
        {
            m_Renderer?.OnDestroy();
            m_Renderer = null;
        }

        protected override void UninitInternal()
        {
            UninitRendererInternal();
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var deserializer = world.QueryGameDataDeserializer(world.ID, $"navigation@{pluginName}");

            deserializer.ReadInt32("NavigationPlugin.Version");

            m_MeshVertices = deserializer.ReadVector3Array("Mesh Vertices");
            m_MeshIndices = deserializer.ReadInt32Array("Mesh Indices");
            m_MeshTriangleAreaIDs = deserializer.ReadUInt16Array("Mesh Triangle Area IDs");
            m_AreaColors = deserializer.ReadColorArray("Area Colors");
            m_Name = deserializer.ReadString("Name");
            m_ID = deserializer.ReadInt32("ID");
            m_EnableDepthTest = deserializer.ReadBoolean("Enable Depth Test");

            deserializer.Uninit();
        }

        public void FindPath(Vector3 start, Vector3 end, List<Vector3> path)
        {
            m_NavigationSystem.FindPath(start, end, path, null);
        }

        private string m_Name;
        private Vector3[] m_MeshVertices;
        private int[] m_MeshIndices;
        private ushort[] m_MeshTriangleAreaIDs;
        private Color[] m_AreaColors;
        private bool m_EnableDepthTest;
        private readonly MyNavigationSystem m_NavigationSystem = new();
        private NavigationPluginRenderer m_Renderer;
    }
}
