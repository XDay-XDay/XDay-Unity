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
    public class LocatorBehaviour : MonoBehaviour
    {
        public void Initialize(int gridID, Action<UpdateLocatorMovementEvent> updateLocatorMovementEventSender)
        {
            m_GridID = gridID;
            m_UpdateLocatorMovementEventSender = updateLocatorMovementEventSender;
        }

        public void Refresh()
        {
            m_UpdateLocatorMovementEventSender?.Invoke(new UpdateLocatorMovementEvent()
            {
                GridID = m_GridID,
                LocatorGameObject = gameObject,
                IsSelectionValid = IsSelectionValid(),
            });
        }

        bool IsSelectionValid()
        {
            if (Selection.activeGameObject == null)
            {
                return false;
            }

            if (Selection.activeGameObject.GetComponent<LocatorBehaviour>() != null)
            {
                return true;
            }

            return false;
        }

        private Action<UpdateLocatorMovementEvent> m_UpdateLocatorMovementEventSender;
        private int m_GridID;
    }

    [CustomEditor(typeof(LocatorBehaviour))]
    [CanEditMultipleObjects]
    class LocatorBehaviourEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var e = Event.current;
            if (e.type == EventType.MouseUp)
            {
                var behaviour = target as LocatorBehaviour;
                if (behaviour != null)
                {
                    behaviour.Refresh();
                }
            }
        }
    }
}

#endif