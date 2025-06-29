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

namespace XDay.GamePlayAPI
{
    internal abstract class UnitRendererQueueBase
    {
        public UnitRendererQueueBase(UnitRendererManager manager)
        {
            m_RendererManager = manager;

            m_NodePool = IObjectPool<LinkedListNode<UnitQueueAction>>.Create(createFunc:
                () => {
                    return new LinkedListNode<UnitQueueAction>(new UnitQueueAction());
                });
        }

        public void OnDestroy()
        {
            m_NodePool.OnDestroy();
        }

        public abstract void Update();

        public void CreateUnit(Unit unit)
        {
            Debug.Assert(unit.ID != 0);
            var action = new UnitQueueAction()
            {
                Unit = unit,
                UnitID = unit.ID,
                Type = ActionType.Create,
                LOD = unit.CurrentLOD,
            };

            AddAction(action);
        }

        public void UpdateUnit(Unit unit)
        {
            var action = new UnitQueueAction()
            {
                Unit = unit,
                UnitID = unit.ID,
                Type = ActionType.Update,
                LOD = unit.CurrentLOD,
            };
            AddAction(action);
        }

        public void ChangeUnitLOD(Unit unit)
        {
            var action = new UnitQueueAction()
            {
                Unit = unit,
                UnitID = unit.ID,
                Type = ActionType.ChangeLOD,
                LOD = unit.CurrentLOD,
            };
            AddAction(action);
        }

        public void DestroyUnit(long unitID, int lod)
        {
            var action = new UnitQueueAction()
            {
                Unit = null,
                UnitID = unitID,
                Type = ActionType.Destroy,
                LOD = lod
            };
            AddAction(action);
        }

        private void AddAction(UnitQueueAction action)
        {
            if (action.Type == ActionType.Create)
            {
                if (m_DestroyActions.TryGetValue(action.UnitID, out var node))
                {
                    m_DestroyActions.Remove(action.UnitID);
                    ReleaseNode(node);
                    if (action.LOD != node.Value.LOD)
                    {
                        //Debug.LogError($"创建和删除抵消,变为更新LOD action {action.EntityID}, lod:{action.Entity.CurrentLOD}");
                        var updateAction = new UnitQueueAction()
                        {
                            Unit = action.Unit,
                            UnitID = action.UnitID,
                            Type = ActionType.ChangeLOD,
                            LOD = action.Unit.CurrentLOD,
                        };
                        var updateNode = m_NodePool.Get();
                        updateNode.Value = updateAction;
                        m_Actions.AddLast(updateNode);
                    }
                    return;
                }
            }
            else if (action.Type == ActionType.Destroy)
            {
                if (m_CreateActions.TryGetValue(action.UnitID, out var node))
                {
                    ReleaseNode(node);
                    m_CreateActions.Remove(action.UnitID);
                    return;
                }
            }

            var newNode = m_NodePool.Get();
            newNode.Value = action;
            m_Actions.AddLast(newNode);

            if (action.Type == ActionType.Create)
            {
                m_CreateActions.Add(action.UnitID, newNode);
            }
            else if (action.Type == ActionType.Destroy)
            {
                m_DestroyActions.Add(action.UnitID, newNode);
            }
        }

        protected void ReleaseNode(LinkedListNode<UnitQueueAction> node)
        {
            m_NodePool.Release(node);
            m_Actions.Remove(node);
        }

        protected LinkedList<UnitQueueAction> m_Actions = new();
        protected IObjectPool<LinkedListNode<UnitQueueAction>> m_NodePool;
        protected UnitRendererManager m_RendererManager;
        protected Dictionary<long, LinkedListNode<UnitQueueAction>> m_CreateActions = new();
        protected Dictionary<long, LinkedListNode<UnitQueueAction>> m_DestroyActions = new();

        internal enum ActionType
        {
            Create = 0,
            Destroy = 1,
            ChangeLOD = 2,
            Update = 3,
        }

        internal struct UnitQueueAction
        {
            public long UnitID;
            public Unit Unit;
            public ActionType Type;
            public int LOD;
        }
    }
}
