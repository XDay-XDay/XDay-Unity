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


namespace XDay.WorldAPI
{
    internal partial class PluginLODSystem
    {
        [XDaySerializableClass("Plugin LOD Setup")]
        public class PluginLODSetup : IPluginLODSetup
        {
            public float Tolerance { get => m_Tolerance; set => m_Tolerance = value; }
            public float Altitude { get => m_Height; set => m_Height = value; }
            public string Name { get => m_Name; set => m_Name = value; }
            public string TypeName => "PluginLODSetup";

            public PluginLODSetup()
            {
            }

            public PluginLODSetup(string name, float height, float tolerance)
            {
                m_Name = name;
                m_Height = height;
                m_Tolerance = tolerance;
            }

            public void EditorDeserialize(IDeserializer deserializer, string label)
            {
                deserializer.ReadInt32("PluginLODSystem.Version");

                m_Name = deserializer.ReadString("Name");
                m_Height = deserializer.ReadSingle("Height");
                m_Tolerance = deserializer.ReadSingle("Tolerance");
            }

            public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
            {
                serializer.WriteInt32(m_Version, "PluginLODSystem.Version");

                serializer.WriteString(m_Name, "Name");
                serializer.WriteSingle(m_Height, "Height");
                serializer.WriteSingle(m_Tolerance, "Tolerance");
            }

            public void GameDeserialize(IDeserializer deserializer, string label)
            {
                deserializer.ReadInt32("PluginLODSystem.Version");

                m_Height = deserializer.ReadSingle("Height");
                m_Tolerance = deserializer.ReadSingle("Tolerance");
            }

            public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
            {
                serializer.WriteInt32(m_RuntimeVersion, "PluginLODSystem.Version");

                serializer.WriteSingle(m_Height, "Height");
                serializer.WriteSingle(m_Tolerance, "Tolerance");
            }

            [XDaySerializableField(1, "Tolerance")]
            private float m_Tolerance;
            [XDaySerializableField(1, "Height")]
            private float m_Height;
            [XDaySerializableField(1, "Name")]
            private string m_Name;

            private const int m_Version = 1;
            private const int m_RuntimeVersion = 1;
        }
    }
}
