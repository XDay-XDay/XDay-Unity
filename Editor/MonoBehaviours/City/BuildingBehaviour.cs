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

namespace XDay.WorldAPI.City.Editor
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class BuildingBehaviour : MonoBehaviour
    {
        public void Initialize(int instanceID, int gridID, 
            Action<DestroyBuildingEvent> destroyBuildingEventSender,
            Action<BuildingPositionChangeEvent> buildingPositionChangeEventSender,
            Action<DrawGridEvent> drawGridEventSender,
            Action<UpdateBuildingMovementEvent> updateBuildingMovementEventSender
            )
        {
            m_BuildingInstanceID = instanceID;
            m_GridID = gridID;
            m_DestroyBuildingEventSender = destroyBuildingEventSender;
            m_BuildingPositionChangeEventSender = buildingPositionChangeEventSender;
            m_DrawGridEventSender = drawGridEventSender;
            m_UpdateBuildingMovementEventSender = updateBuildingMovementEventSender;
        }

        void Start()
        {
            m_LastUpdatePosition = gameObject.transform.position;
        }

        void OnDestroy()
        {
            if (m_ProcessDestroyEvent)
            {
                m_DestroyBuildingEventSender?.Invoke(new DestroyBuildingEvent()
                {
                    GridID = m_GridID,
                    BuildingID = m_BuildingInstanceID
                });
            }
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

                m_BuildingPositionChangeEventSender?.Invoke(new BuildingPositionChangeEvent()
                {
                    GridID = m_GridID,
                    BuildingID = m_BuildingInstanceID
                });
            }
        }

        public void Refresh()
        {
            m_UpdateBuildingMovementEventSender?.Invoke(new UpdateBuildingMovementEvent()
            {
                GridID = m_GridID,
                BuildingID = m_BuildingInstanceID,
                IsSelectionValid = IsSelectionValid(),
            });
        }

        public void DrawHandle()
        {
            m_DrawGridEventSender?.Invoke(new DrawGridEvent()
            {
                GridID = m_GridID,
                BuildingID = m_BuildingInstanceID,
            });
        }

        bool IsSelectionValid()
        {
            if (Selection.activeGameObject == null)
            {
                return false;
            }

            if (Selection.activeGameObject.GetComponent<BuildingBehaviour>() != null)
            {
                return true;
            }

            return false;
        }

        public bool ProcessDestroyEvent { set => m_ProcessDestroyEvent = value; }

        private Vector3 m_LastUpdatePosition;
        private int m_BuildingInstanceID;
        private int m_GridID;
        private bool m_ProcessDestroyEvent = true;
        private Action<DestroyBuildingEvent> m_DestroyBuildingEventSender;
        private Action<BuildingPositionChangeEvent> m_BuildingPositionChangeEventSender;
        private Action<DrawGridEvent> m_DrawGridEventSender;
        private Action<UpdateBuildingMovementEvent> m_UpdateBuildingMovementEventSender;
    }

    [CustomEditor(typeof(BuildingBehaviour))]
    [CanEditMultipleObjects]
    class BuildingBehaviourEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var behaviour = target as BuildingBehaviour;

            var e = Event.current;
            if (e.type == EventType.MouseUp)
            {
                if (behaviour != null)
                {
                    behaviour.Refresh();   
                }
            }

            behaviour.DrawHandle();
        }
    }
}
#endif