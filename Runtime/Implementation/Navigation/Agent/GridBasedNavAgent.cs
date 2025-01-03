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
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    internal partial class GridBasedNavAgent : IGridNavigationAgent
    {
        public IGridBasedPathFinder PathFinder { set => m_PathFinder = value; }
        public bool IsMoving => m_CurrentState.GetType() == typeof(Move);
        public Quaternion Rotation { get => m_GameObject.transform.rotation; set => m_GameObject.transform.rotation = value; }
        public Vector3 Position { get => m_GameObject.transform.position; set => m_GameObject.transform.position = value; }
        public List<Vector3> Path => m_Path;

        public GridBasedNavAgent(GameObject overrideGameObject, Transform parent, Vector3 position, Quaternion rotation)
        {
            m_States.Add(new Idle(this));
            m_States.Add(new Move(this));
            SetState<Idle>();

            SetGameObject(overrideGameObject, parent, position, rotation);
        }

        public void OnDestroy()
        {
            if (m_OverrideGameObject)
            {
                Helper.DestroyUnityObject(m_GameObject);
            }
        }

        public void Update()
        {
            m_CurrentState?.Update();
        }

        public void MoveTo(Vector3 target)
        {
            if (m_PathFinder == null)
            {
                Debug.LogError("Can't move, no path finder!");
                return;
            }
            m_Path.Clear();
            m_PathFinder.CalculatePath(Position, target, m_Path, null, PathFlags.SourceSearchNearestCoordinate);
            SetState<Move>();
        }

        public void DebugDraw()
        {
            for (var i = 0; i < m_Path.Count - 1; ++i)
            {
                Gizmos.DrawLine(m_Path[i], m_Path[i + 1]);
            }
        }

        private void SetState<T>() where T : State
        {
            m_CurrentState?.OnExit();
            m_CurrentState = QueryState<T>();
            m_CurrentState.OnEnter();
        }

        private T QueryState<T>() where T : State
        {
            foreach (var state in m_States)
            {
                if (state.GetType() == typeof(T))
                {
                    return state as T;
                }
            }
            return null;
        }

        private void SetGameObject(GameObject overrideGameObject, Transform parent, Vector3 position, Quaternion rotation)
        {
            m_OverrideGameObject = false;
            m_GameObject = overrideGameObject;
            if (m_GameObject == null)
            {
                m_OverrideGameObject = true;
                m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m_GameObject.name = "Nav Agent";
                m_GameObject.transform.SetPositionAndRotation(position, rotation);
                m_GameObject.transform.SetParent(parent, true);
            }
        }

        private IGridBasedPathFinder m_PathFinder;
        private List<Vector3> m_Path = new();
        private List<State> m_States = new();
        private bool m_OverrideGameObject;
        private GameObject m_GameObject;
        private State m_CurrentState;
    }
}


//XDay