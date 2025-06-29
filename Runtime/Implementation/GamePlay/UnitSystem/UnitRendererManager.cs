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

namespace XDay.GamePlayAPI
{
    public abstract class UnitRendererManager
    {
        public UnitRendererManager(UnitManager unitManager)
        {
            m_UnitManager = unitManager;
            m_UnitManager.EventCreateUnit += OnUnitCreated;
            m_UnitManager.EventDestroyUnit += OnUnitDestroyed;
            m_UnitManager.EventChangeUnitLOD += OnUnitLODChanged;
            m_UnitManager.EventUpdateUnit += OnUnitUpdated;

            m_Queue = new(this);
            m_HighPriorityQueue = new(this);

            ProcessExistedEntities();
        }

        public virtual void OnDestroy()
        {
            foreach (var renderer in m_Renderers.Values)
            {
                renderer.OnDestroy();
            }
            m_Renderers = null;

            m_UnitManager.EventCreateUnit -= OnUnitCreated;
            m_UnitManager.EventDestroyUnit -= OnUnitDestroyed;
            m_UnitManager.EventChangeUnitLOD -= OnUnitLODChanged;
            m_UnitManager.EventUpdateUnit -= OnUnitUpdated;

            m_Queue.OnDestroy();
            m_Queue = null;

            m_HighPriorityQueue.OnDestroy();
            m_HighPriorityQueue = null;
        }

        public void Update(float dt)
        {
            m_HighPriorityQueue.Update();
            m_Queue.Update();

            OnUpdate(dt);
        }

        public UnitRenderer CreateRenderer(Unit unit)
        {
            m_Renderers.TryGetValue(unit.ID, out var renderer);
            if (renderer == null)
            {
                renderer = CreateRendererInternal(unit);
                if (renderer != null)
                {
                    renderer.SetUnit(unit);
                    m_Renderers.Add(unit.ID, renderer);
                }
                return renderer;
            }
            Log.Instance?.Error($"Renderer {unit.Name} already exists!");
            return null;
        }

        public UnitRenderer GetUnit(long id)
        {
            m_Renderers.TryGetValue(id, out var renderer);
            return renderer;
        }

        private void OnUnitCreated(Unit unit)
        {
            if (unit.Renderable)
            {
                if (unit.IsHighPriority)
                {
                    m_HighPriorityQueue.CreateUnit(unit);
                }
                else
                {
                    m_Queue.CreateUnit(unit);
                }
            }
        }

        private void OnUnitUpdated(Unit unit)
        {
            if (unit.IsHighPriority)
            {
                m_HighPriorityQueue.UpdateUnit(unit);
            }
            else
            {
                m_Queue.UpdateUnit(unit);
            }
        }

        private void OnUnitLODChanged(Unit unit)
        {
            if (unit.IsHighPriority)
            {
                m_HighPriorityQueue.ChangeUnitLOD(unit);
            }
            else
            {
                m_Queue.ChangeUnitLOD(unit);
            }
        }

        private void OnUnitDestroyed(Unit unit)
        {
            if (unit.IsHighPriority)
            {
                m_HighPriorityQueue.DestroyUnit(unit.ID, unit.CurrentLOD);
            }
            else
            {
                m_Queue.DestroyUnit(unit.ID, unit.CurrentLOD);
            }
        }

        internal void DestroyRenderer(long unitID)
        {
            if (m_Renderers.TryGetValue(unitID, out var renderer))
            {
                if (renderer != null)
                {
                    OnWillDestroyRenderer(renderer);
                    renderer.OnDestroy();
                }
                m_Renderers.Remove(unitID);
            }
        }

        internal UnitRenderer UpdateRenderer(Unit unit)
        {
            if (m_Renderers.TryGetValue(unit.ID, out var renderer))
            {
                if (renderer != null)
                {
                    UpdateRendererInternal(renderer);
                }
            }
            return renderer;
        }

        internal void ChangeRendererLOD(long unitID, int lod)
        {
            if (m_UnitManager.GetUnit(unitID) == null)
            {
                //unit已经不存在,不需要更新Renderer了
                return;
            }

            if (m_Renderers.TryGetValue(unitID, out var renderer))
            {
                ChangeRendererLODInternal(unitID, lod);
            }
        }

        private void ProcessExistedEntities()
        {
            var entities = m_UnitManager.GetUnits();
            foreach (var unit in entities)
            {
                OnUnitCreated(unit);
            }
        }

        protected abstract void OnUpdate(float dt);
        protected abstract UnitRenderer CreateRendererInternal(Unit unit);
        protected abstract void OnWillDestroyRenderer(UnitRenderer renderer);
        protected abstract void ChangeRendererLODInternal(long unitID, int lod);
        protected abstract UnitRenderer UpdateRendererInternal(UnitRenderer renderer);

        private readonly UnitManager m_UnitManager;
        private TimeSlicedUnitRendererQueue m_Queue;
        private FullUpdateUnitRendererQueue m_HighPriorityQueue;
        protected Dictionary<long, UnitRenderer> m_Renderers = new();
    }
}