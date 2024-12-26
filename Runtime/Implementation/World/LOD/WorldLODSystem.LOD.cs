/*
 * Copyright (c) 2024 XDay
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



using XDay.SerializationAPI;
using UnityEngine;

namespace XDay.WorldAPI
{
    [XDaySerializableClass("World LOD Setup")]
    internal partial class WorldLODSetup : IWorldLODSetup, ISerializable
    {
        public string Name { get => m_Name; set => m_Name = value; }
        public float Altitude { get => m_Height; set => m_Height = value; }
        public Vector2 WorldDataRange { get => m_WorldDataRange; set => m_WorldDataRange = value; }
        public string TypeName => "WorldLODSetup";

        public WorldLODSetup()
        {
        }

        public WorldLODSetup(string name, float height)
        {
            m_Name = name;
            m_Height = height;
        }

        public void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldLODSetup.Version");

            m_Name = deserializer.ReadString("Name");
            m_WorldDataRange = deserializer.ReadVector2("Data Range");
            m_Height = deserializer.ReadSingle("Height");
        }

        public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "WorldLODSetup.Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteVector2(m_WorldDataRange, "Data Range");
            serializer.WriteSingle(m_Height, "Height");
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldLODSetup.Version");

            m_Name = deserializer.ReadString("Name");
            m_WorldDataRange = deserializer.ReadVector2("Data Range");
            m_Height = deserializer.ReadSingle("Height");
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "WorldLODSetup.Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteVector2(m_WorldDataRange, "Data Range");
            serializer.WriteSingle(m_Height, "Height");
        }

        [XDaySerializableField(1, "Height")]
        private float m_Height;
        [XDaySerializableField(1, "Name")]
        private string m_Name;
        [XDaySerializableField(1, "Data Range")]
        private Vector2 m_WorldDataRange;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}