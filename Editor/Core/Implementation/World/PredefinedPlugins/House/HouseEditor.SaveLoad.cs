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

namespace XDay.WorldAPI.House.Editor
{
    internal partial class HouseEditor
    {
        const int m_Version = 1;

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "HouseEditor.Version");

            base.EditorSerialize(writer, label, converter);

            writer.WriteString(m_Name, "Name");
            writer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, false);
            writer.WriteBoolean(m_ShowAgentSettings, "Show Agent Settings");
            writer.WriteBoolean(m_ShowHouseSettings, "Show House Settings");
            writer.WriteBoolean(m_ShowHouseInstanceSettings, "Show House Instance Settings");
            writer.WriteBoolean(m_ShowTeleporterSettings, "Show Teleporter Settings");
            writer.WriteBoolean(m_ShowTeleporterInstanceSettings, "Show Teleporter Instance Settings");
            writer.WriteInt32(m_SelectedAgentTemplateIndex, "Selected Agent Template Index");
            writer.WriteBoolean(m_ShowInteractivePointSettings, "Show Interactive Point Settings");
            writer.WriteBoolean(m_ShowInteractivePointInstanceSettings, "Show Interactive Point Instance Settings");

            writer.WriteList(m_Houses, "Houses", (house, index) =>
            {
                writer.WriteSerializable(house, $"House {index}", converter, false);
            });

            writer.WriteList(m_AgentTemplates, "Agents", (agent, index) =>
            {
                writer.WriteStructure($"Agent {index}", () =>
                {
                    agent.Save(writer, converter);
                });
            });

            writer.WriteList(m_HouseInstances, "House Instances", (houseInstance, index) =>
            {
                writer.WriteSerializable(houseInstance, $"House Instance {index}", converter, false);
            });

            writer.WriteStructure("Scene Prefab", () =>
            {
                m_ScenePrefab.Save(writer);
            });
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("HouseEditor.Version");

            base.EditorDeserialize(reader, label);

            m_Name = reader.ReadString("Name");
            m_ResourceDescriptorSystem = reader.ReadSerializable<EditorResourceDescriptorSystem>("Resource Descriptor System", false);
            m_ShowAgentSettings = reader.ReadBoolean("Show Agent Settings");
            m_ShowHouseSettings = reader.ReadBoolean("Show House Settings");
            m_ShowHouseInstanceSettings = reader.ReadBoolean("Show House Instance Settings");
            m_ShowTeleporterInstanceSettings = reader.ReadBoolean("Show Teleporter Instance Settings");
            m_ShowTeleporterSettings = reader.ReadBoolean("Show Teleporter Settings");
            m_SelectedAgentTemplateIndex = reader.ReadInt32("Selected Agent Template Index");
            m_ShowInteractivePointSettings = reader.ReadBoolean("Show Interactive Point Settings");
            m_ShowInteractivePointInstanceSettings = reader.ReadBoolean("Show Interactive Point Instance Settings");

            m_Houses = reader.ReadList("Houses", (index) =>
            {
                return reader.ReadSerializable<House>($"House {index}", false);
            });

            m_AgentTemplates = reader.ReadList("Agents", (int index) =>
            {
                var template = new HouseAgentTemplate();
                reader.ReadStructure($"Agent {index}", () =>
                {
                    template.Load(reader);
                });
                return template;
            });

            m_HouseInstances = reader.ReadList("House Instances", (index) =>
            {
                return reader.ReadSerializable<HouseInstance>($"House Instance {index}", false);
            });

            reader.ReadStructure("Scene Prefab", () =>
            {
                m_ScenePrefab.Load(reader);
            });
        }
    }
}