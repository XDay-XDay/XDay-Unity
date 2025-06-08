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

using System;
using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    internal partial class GridBasedNavAgent : IGridNavigationAgent
    {
        public IGridBasedPathFinder PathFinder { set => m_PathFinder = value; get => m_PathFinder; }
        public bool IsMoving => m_CurrentState.GetType() == typeof(Move);
        public Quaternion Rotation { get => m_GameObject.transform.rotation; set => m_GameObject.transform.rotation = value; }
        public Vector3 Position { get => m_GameObject.transform.position; set => m_GameObject.transform.position = value; }
        public List<Vector3> Path => m_Path;
        public float MoveSpeed { get => m_MoveSpeed; set => m_MoveSpeed = value; }
        public float RotateSpeed { get => m_RotateSpeed; set => m_RotateSpeed = value; }

        public event Action EventStartMove;
        public event Action EventStopMove;

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

        public void Update(float dt)
        {
            m_CurrentState?.Update(dt);
        }

        public bool MoveTo(Vector3 target)
        {
            if (m_PathFinder == null)
            {
                return false;
            }

            m_PathFinder.CalculatePath(Position, target, m_TempPath, null, PathFlags.SourceSearchNearestCoordinate);
            if (m_TempPath.Count > 1)
            {
                m_Path.Clear();
                m_Path.AddRange(m_TempPath);
                m_TempPath.Clear();
                SetState<Move>();
                EventStartMove?.Invoke();
            }

            return true;
        }

        public void MoveTo(List<Vector3> path)
        {
            if (path.Count > 1)
            {
                m_Path.Clear();
                m_Path.AddRange(path);
                SetState<Move>();
                EventStartMove?.Invoke();
            }
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
        private List<Vector3> m_TempPath = new();
        private List<State> m_States = new();
        private bool m_OverrideGameObject;
        private GameObject m_GameObject;
        private State m_CurrentState;
        private float m_MoveSpeed = 3;
        private float m_RotateSpeed = 500;
    }
}


//XDay