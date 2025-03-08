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

namespace XDay.UtilityAPI
{
    public class PhysicsWorldDebugger
    {
        public PhysicsWorldDebugger(PhysicsWorld world)
        {
            m_World = world;
        }

        public void OnDestroy()
        {
            foreach (var entity in m_Entities)
            {
                entity.OnDestroy();
            }
            m_Entities.Clear();

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;
        }

        public void Create()
        {
            OnDestroy();

            m_Root = new GameObject("PhysicsWorldDebugger");
            for (var i = 0; i < m_World.BodyCount; i++)
            {
                m_World.GetBody(i, out var body);
                if (body != null)
                {
                    var entity = new PhysicsEntity(body, m_Root.transform);
                    entity.Sync();
                    m_Entities.Add(entity);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            m_Root.SetActive(visible);
        }

        private PhysicsWorld m_World;
        private GameObject m_Root;
        private List<PhysicsEntity> m_Entities = new();
    }
}
