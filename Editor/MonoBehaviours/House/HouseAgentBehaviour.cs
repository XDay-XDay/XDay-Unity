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

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;

namespace XDay.WorldAPI.House.Editor
{
    /// <summary>
    /// monobehaviour无法在Editor Assembly中使用，所以只能使用#if UNITY_EDITOR宏隔开了
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class HouseAgentBehaviour : MonoBehaviour
    {
        public void Initialize(int agentInstanceID, int gridID,
            Action<DestroyAgentEvent> destroyAgentEventSender,
            Action<AgentPositionChangeEvent> agentPositionChangeEventSender,
            Action<UpdateAgentMovementEvent> updateAgentMovementEventSender,
            Action<SetAgentDestinationEvent> setAgentDestinationEventSender)
        {
            m_AgentInstanceID = agentInstanceID;
            m_HouseID = gridID;
            m_DestroyAgentEventSender = destroyAgentEventSender;
            m_AgentPositionChangeEventSender = agentPositionChangeEventSender;
            m_UpdateAgentMovementEventSender = updateAgentMovementEventSender;
            m_SetAgentDestinationEventSender = setAgentDestinationEventSender;
        }

        void Start()
        {
            m_LastUpdatePosition = gameObject.transform.position;
        }

        void OnDestroy()
        {
            m_DestroyAgentEventSender?.Invoke(new DestroyAgentEvent()
            {
                HouseID = m_HouseID,
                AgentID = m_AgentInstanceID
            });
        }

        void Update()
        {
            UpdatePosition();
        }

        void UpdatePosition()
        {
            if (m_LastUpdatePosition != gameObject.transform.position)
            {
                m_LastUpdatePosition = gameObject.transform.position;

                m_AgentPositionChangeEventSender?.Invoke(new AgentPositionChangeEvent()
                {
                    HouseID = m_HouseID,
                    AgentID = m_AgentInstanceID
                });
            }
        }

        public void Refresh()
        {
            m_UpdateAgentMovementEventSender?.Invoke(new UpdateAgentMovementEvent()
            {
                HouseID = m_HouseID,
                AgentID = m_AgentInstanceID
            });
        }

        public void MoveTo(Vector2 guiScreenPoint)
        {
            m_SetAgentDestinationEventSender?.Invoke(new SetAgentDestinationEvent()
            {
                HouseID = m_HouseID,
                AgentID = m_AgentInstanceID,
                GuiScreenPoint = guiScreenPoint
            });
        }

        private Vector3 m_LastUpdatePosition;
        private int m_AgentInstanceID;
        private int m_HouseID;
        private Action<DestroyAgentEvent> m_DestroyAgentEventSender;
        private Action<AgentPositionChangeEvent> m_AgentPositionChangeEventSender;
        private Action<UpdateAgentMovementEvent> m_UpdateAgentMovementEventSender;
        private Action<SetAgentDestinationEvent> m_SetAgentDestinationEventSender;
    }

    [CustomEditor(typeof(HouseAgentBehaviour))]
    [CanEditMultipleObjects]
    class HouseAgentBehaviourEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var e = Event.current;
            if (e.type == EventType.MouseUp)
            {
                var behaviour = target as HouseAgentBehaviour;
                if (e.button == 0)
                {
                    if (behaviour != null)
                    {
                        behaviour.Refresh();
                    }
                }
                else if (e.button == 1)
                {
                    behaviour.MoveTo(e.mousePosition);
                }
            }
        }
    }
}

#endif