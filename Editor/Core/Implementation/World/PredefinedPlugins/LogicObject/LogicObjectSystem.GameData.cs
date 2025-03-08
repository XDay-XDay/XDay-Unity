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

using XDay.UtilityAPI;

namespace XDay.WorldAPI.LogicObject.Editor
{
    partial class LogicObjectSystem
    {
        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            SyncObjectTransforms();

            GenerateGridData(converter);
        }

        private void GenerateGridData(IObjectIDConverter converter)
        {
            ISerializer serializer = ISerializer.CreateBinary();

            serializer.WriteInt32(m_RuntimeVersion, "GridData.Version");
            
            serializer.WriteBounds(Bounds, "Bounds");
            serializer.WriteString(Name, "Name");
            serializer.WriteObjectID(ID, "ID", converter);

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, true);

            serializer.Uninit();
            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("logic_object"));
        }

        private const int m_RuntimeVersion = 1;
    }
}



//XDay