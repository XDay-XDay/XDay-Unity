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
using XDay.NavigationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.House.Editor
{
    [SelectionBase]
    internal class HouseAgentInstance : HouseObject
    {
        public HouseAgentTemplate Template { get => m_Template; set { m_Template = value; m_TemplateID = value.ID; } }
        public override bool CanUpdateMovement => !m_Agent.IsMoving;

        public HouseAgentInstance()
        {
        }

        public HouseAgentInstance(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation, int templateID)
            : base(id, size, min, max, rotation)
        {
            m_TemplateID = templateID;
        }

        public void Initialize(Transform parent, House house)
        {
            m_Template = house.HouseEditor.GetAgentTemplate(m_TemplateID);

            var prefab = m_Template?.Prefab;
            Initialize(parent, house, prefab, "Agent");
            m_Animator = m_GameObject.GetComponentInChildren<Animator>(false);

            m_Agent = IGridNavigationAgent.Create(m_GameObject, parent, Position, Rotation);
            m_Agent.MoveSpeed = m_Template.MoveSpeed;
            m_Agent.RotateSpeed = m_Template.RotateSpeed;
            m_Agent.EventStartMove += OnStartMove;
            m_Agent.EventStopMove += OnStopMove;

            var behaviour = m_GameObject.AddComponent<HouseAgentBehaviour>();
            behaviour.Initialize(ID, House.ID,
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });

            PlayAnimation(m_Template.IdleAnimName);
        }

        private void OnStopMove()
        {
            PlayAnimation(m_Template.IdleAnimName);
        }

        private void OnStartMove()
        {
            PlayAnimation(m_Template.RunAnimName);
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

            if (m_Animator != null && m_Animator.gameObject.activeInHierarchy)
            {
                m_Animator.Update(dt);
            }
        }

        //在同一个房间内移动
        public void MoveInsideHouse(Vector2 guiScreenPoint, House house)
        {
            var world = WorldEditor.WorldManager.FirstWorld;
            var worldPosition = Helper.GUIRayCastWithXZPlane(guiScreenPoint, world.CameraManipulator.Camera, house.Position.y);
            var targetCoord = house.PositionToCoordinate(worldPosition);
            var endPos = house.CoordinateToGridCenterPosition(targetCoord.x, targetCoord.y);
            endPos.y = worldPosition.y;
            m_Agent.MoveTo(endPos);
        }

        public bool CanMove()
        {
            return m_Agent.PathFinder != null;
        }

        public void MoveAlongPath(List<Vector3> path)
        {
            m_Agent.MoveTo(path);
        }

        public void SetNavigationData(IGridBasedPathFinder pathFinder)
        {
            m_Agent.PathFinder = pathFinder;
        }

        public override void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_VERSION, "HouseAgentInstance.Version");

            base.Save(writer, converter);

            writer.WriteObjectID(m_TemplateID, "Template ID", converter);
        }

        public override void Load(IDeserializer reader)
        {
            reader.ReadInt32("HouseAgentInstance.Version");

            base.Load(reader);

            m_TemplateID = reader.ReadInt32("Template ID");
        }

        private void PlayAnimation(string name)
        {
            if (m_Animator != null)
            {
                m_Animator.Play(name);
            }
        }

        [SerializeField]
        private int m_TemplateID;
        private HouseAgentTemplate m_Template;
        private IGridNavigationAgent m_Agent;
        private Animator m_Animator;
        private const int m_VERSION = 1;
    }
}
