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

using UnityEngine;
using XDay.NavigationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    [SelectionBase]
    class AgentInstance : GridObject
    {
        public AgentInstance()
        {
        }

        public AgentInstance(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation, int templateID)
            : base(id, size, min, max, rotation)
        {
            m_TemplateID = templateID;
        }

        public void Initialize(CityEditor cityEditor, Transform parent, Grid ownerGrid)
        {
            m_Template = cityEditor.GetAgentTemplate(m_TemplateID);

            var prefab = m_Template?.Prefab;
            Initialize(parent, ownerGrid, prefab, "Agent");

            m_Agent = IGridNavigationAgent.Create(m_GameObject, parent, Position, Rotation);

            var behaviour = m_GameObject.AddComponent<AgentBehaviour>();
            behaviour.Initialize(ID, OwnerGrid.ID, 
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            m_Agent.OnDestroy();
        }

        public override void Update(float dt)
        {
            m_Agent.Update(dt);

            if (m_Agent.IsMoving)
            {
                UpdateMovement(changePosition: false, forceUpdate: true, true);
            }
        }

        public bool MoveTo(Vector2 guiScreenPoint)
        {
            var world = WorldEditor.WorldManager.FirstWorld;
            var worldPosition =  Helper.GUIRayCastWithXZPlane(guiScreenPoint, world.CameraManipulator.Camera);
            var targetCoord = OwnerGrid.PositionToCoordinate(worldPosition);
            var endPos = OwnerGrid.CoordinateToGridCenterPosition(targetCoord.x, targetCoord.y);
            return m_Agent.MoveTo(endPos);
        }

        public void SetNavigationData(IGridBasedPathFinder pathFinder)
        {
            m_Agent.PathFinder = pathFinder;
        }

        public override void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_VERSION, "AgentInstance.Version");

            base.Save(writer, converter);

            writer.WriteObjectID(m_TemplateID, "Template ID", converter);
        }

        public override void Load(IDeserializer reader)
        {
            reader.ReadInt32("AgentInstance.Version");

            base.Load(reader);

            m_TemplateID = reader.ReadInt32("Template ID");
        }

        public AgentTemplate Template { get => m_Template; set { m_Template = value; m_TemplateID = value.ID; } }

        public override bool CanUpdateMovement => !m_Agent.IsMoving;

        [SerializeField]
        int m_TemplateID;

        AgentTemplate m_Template;

        IGridNavigationAgent m_Agent;

        const int m_VERSION = 1;
    }
}