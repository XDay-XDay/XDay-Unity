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

namespace XDay.GamePlayAPI
{
    public partial class UnitManager
    {
        public event Action<Unit> EventCreateUnit;
        public event Action<Unit> EventDestroyUnit;
        public event Action<Unit> EventUpdateUnit;
        public event Action<Unit> EventChangeUnitLOD;
        public int LastLOD => m_LastLOD;
        public int CurrentLOD => m_CurrentLOD;
        public long NextID => ++m_NextID;

        public virtual void OnDestroy()
        {
            foreach (var kv in m_Units)
            {
                DestroyUnit(kv.Value);
            }
        }

        public bool DestroyUnit(long id)
        {
            var ok = m_Units.Remove(id, out var unit);
            if (ok)
            {
                DestroyUnit(unit);
            }
            else
            {
                Log.Instance?.Error($"删除Unit{id}失败");
            }
            return ok;
        }

        public void DestroyUnit(Unit unit)
        {
            OnWillDestroyUnit(unit);
            EventDestroyUnit?.Invoke(unit);
            unit.Uninit();
        }

        public void AddUnit(Unit unit)
        {
            unit.UpdateLOD(m_CurrentLOD);
            m_Units.Add(unit.ID, unit);
            EventCreateUnit?.Invoke(unit);

            OnUnitAdded(unit);
        }

        public Unit GetUnit(long id)
        {
            m_Units.TryGetValue(id, out var unit);
            return unit;
        }

        public void ChangeLOD(int oldLOD, int newLOD)
        {
            m_LastLOD = oldLOD;
            m_CurrentLOD = newLOD;

            foreach (var kv in m_Units)
            {
                var unit = kv.Value;
                unit.UpdateLOD(newLOD);
                EventChangeUnitLOD?.Invoke(unit);
            }
        }

        public List<Unit> GetUnits()
        {
            return new(m_Units.Values);
        }

        protected virtual void OnUnitAdded(Unit unit) {}
        protected virtual void OnWillDestroyUnit(Unit unit) {}

        private readonly SortedDictionary<long, Unit> m_Units = new();
        private int m_LastLOD = -1;
        private int m_CurrentLOD = 0;
        private long m_NextID;
    }
}