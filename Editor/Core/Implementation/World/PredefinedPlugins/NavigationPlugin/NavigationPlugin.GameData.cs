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
using System.Linq;
using System.Text;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Navigation.Editor
{
    internal partial class NavigationPlugin
    {
        protected override void ValidateExportInternal(StringBuilder errorMessage)
        {
        }

        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            ISerializer serializer = ISerializer.CreateBinary();
            serializer.WriteInt32(m_RuntimeVersion, "NavigationPlugin.Version");

            serializer.WriteVector3Array(m_LastBuiltResult == null ? null : m_LastBuiltResult.MeshVertices, "Mesh Vertices");
            serializer.WriteInt32Array(m_LastBuiltResult == null ? null : m_LastBuiltResult.MeshIndices, "Mesh Indices");
            serializer.WriteUInt16Array(m_LastBuiltResult == null ? null : m_LastBuiltResult.TriangleAreaIDs, "Mesh Triangle Area IDs");

            Color[] colors = null;
            if (m_LastBuiltResult != null)
            {
                colors = m_Renderer.CreateColors(m_LastBuiltResult.MeshIndices, m_LastBuiltResult.TriangleAreaIDs, m_LastBuiltResult.MeshVertices.Length);
            }
            serializer.WriteColorArray(colors, "Area Colors");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteObjectID(m_ID, "ID", converter);
            serializer.WriteBoolean(m_EnableDepthTest, "Enable Depth Test");

            serializer.Uninit();

            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("navigation"));
        }

        private const int m_RuntimeVersion = 1;
    }
}
