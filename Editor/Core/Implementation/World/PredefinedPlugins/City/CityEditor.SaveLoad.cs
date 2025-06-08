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


namespace XDay.WorldAPI.City.Editor
{
    partial class CityEditor
    {
        const int m_Version = 1;

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "CityEditor.Version");

            base.EditorSerialize(writer, label, converter);

            writer.WriteString(m_Name, "Name");

            writer.WriteEnum(m_Operation, "Operation");

            writer.WriteBoolean(m_ShowGridSettings, "Show Grid Settings");
            writer.WriteBoolean(m_ShowTileSettings, "Show Tile Settings");
            writer.WriteBoolean(m_ShowBuildingSettings, "Show Building Settings");
            writer.WriteBoolean(m_ShowInteractivePointSettings, "Show Interactive Point Settings");
            writer.WriteBoolean(m_ShowWaypointSettings, "Show Way Point Settings");
            writer.WriteBoolean(m_ShowBuildingNames, "Show Building Names");
            writer.WriteBoolean(m_ShowAgentSettings, "Show Agent Settings");
            writer.WriteBoolean(m_ShowRegions, "Show Regions");
            writer.WriteBoolean(m_ShowGridLabels, "Show Grid Labels");
            writer.WriteInt32(m_SelectedTileIndex, "Selected Tile Index");
            writer.WriteInt32(m_SelectedBuildingTemplateIndex, "Selected Building Template Index");
            writer.WriteInt32(m_SelectedAgentTemplateIndex, "Selected Agent Template Index");
            writer.WriteInt32(m_SelectedGridIndex, "Selected Grid Index");
            writer.WriteInt32(m_BrushSize, "Brush Size");
            writer.WriteSingle(m_LocatorSize, "Locator Size");
            writer.WriteBoolean(m_ShowAllWaypoints, "Show All Waypoints");
            writer.WriteBoolean(m_ShowAllInteractivePoints, "Show All Interactive Points");

            writer.WriteList(m_Tiles, "Tiles", (GridTileTemplate tile, int index) =>
            {
                writer.WriteStructure($"Tile {index}", () =>
                {
                    tile.Save(writer, converter);
                });
            });

            writer.WriteList(m_BuildingTemplates, "Buildings", (BuildingTemplate building, int index) =>
            {
                writer.WriteStructure($"Building {index}", () =>
                {
                    building.Save(writer, converter);
                });
            });

            writer.WriteList(m_AgentTemplates, "Agents", (AgentTemplate agent, int index) =>
            {
                writer.WriteStructure($"Agent {index}", () =>
                {
                    agent.Save(writer, converter);
                });
            });

            writer.WriteList(m_Grids, "Grids", (Grid grid, int index) =>
            {
                writer.WriteStructure($"Grid {index}", () =>
                {
                    grid.Save(writer, converter);
                });
            });
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("CityEditor.Version");

            base.EditorDeserialize(reader, label);

            m_Name = reader.ReadString("Name");

            m_Operation = reader.ReadEnum<OperationType>("Operation");

            m_ShowGridSettings = reader.ReadBoolean("Show Grid Settings");
            m_ShowTileSettings = reader.ReadBoolean("Show Tile Settings");
            m_ShowBuildingSettings = reader.ReadBoolean("Show Building Settings");
            m_ShowInteractivePointSettings = reader.ReadBoolean("Show Interactive Point Settings");
            m_ShowWaypointSettings = reader.ReadBoolean("Show Way Point Settings");
            m_ShowBuildingNames = reader.ReadBoolean("Show Building Names");
            m_ShowAgentSettings = reader.ReadBoolean("Show Agent Settings");

            m_ShowRegions = reader.ReadBoolean("Show Regions");
            m_ShowGridLabels = reader.ReadBoolean("Show Grid Labels");

            m_SelectedTileIndex = reader.ReadInt32("Selected Tile Index");
            m_SelectedBuildingTemplateIndex = reader.ReadInt32("Selected Building Template Index");
            m_SelectedAgentTemplateIndex = reader.ReadInt32("Selected Agent Template Index");
            m_SelectedGridIndex = reader.ReadInt32("Selected Grid Index");
            m_BrushSize = reader.ReadInt32("Brush Size");
            m_LocatorSize = reader.ReadSingle("Locator Size");
            m_ShowAllWaypoints = reader.ReadBoolean("Show All Waypoints");
            m_ShowAllInteractivePoints = reader.ReadBoolean("Show All Interactive Points");

            m_Tiles = reader.ReadList("Tiles", (int index) =>
            {
                var template = new GridTileTemplate();
                reader.ReadStructure($"Tile {index}", () =>
                {
                    template.Load(reader);
                });
                return template;
            });

            m_BuildingTemplates = reader.ReadList("Buildings", (int index) =>
            {
                var template = new BuildingTemplate();
                reader.ReadStructure($"Building {index}", () =>
                {
                    template.Load(reader);
                });
                return template;
            });

            m_AgentTemplates = reader.ReadList("Agents", (int index) =>
            {
                var template = new AgentTemplate();
                reader.ReadStructure($"Agent {index}", () =>
                {
                    template.Load(reader);
                });
                return template;
            });

            m_Grids = reader.ReadList("Grids", (int index) =>
            {
                var grid = new Grid();
                reader.ReadStructure($"Grid {index}", () =>
                {
                    grid.Load(reader);
                });
                return grid;
            });

            if (m_SelectedGridIndex >= m_Grids.Count)
            {
                m_SelectedGridIndex = -1;
            }
        }
    }
}