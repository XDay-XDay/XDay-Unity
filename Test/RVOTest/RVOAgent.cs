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

using RVO;
using UnityEngine;
using XDay;

namespace Assets.XDay.Test.RVOTest
{
    class RVOAgent
    {
        public RVOAgent(float x, float z)
        {
            m_ID = Simulator.Instance.addAgent(new FixedVector2(x, z));
            m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }

        public void OnDestroy()
        {
            Object.Destroy(m_GameObject);
        }

        public void SetVelocity(Vector3 velocity)
        {
            Simulator.Instance.setAgentPrefVelocity(m_ID, new FixedVector2(velocity.x, velocity.z));
        }

        public Vector3 GetPosition()
        {
            var pos = Simulator.Instance.getAgentPosition(m_ID);
            return new Vector3(pos.X.FloatValue, 0, pos.Y.FloatValue);
        }

        public bool Update()
        {
            if (m_GameObject == null)
            {
                Simulator.Instance.removeAgent(m_ID);
                return true;
            }
            else
            {
                var pos = Simulator.Instance.getAgentPosition(m_ID);
                m_GameObject.transform.position = new Vector3(pos.X.FloatValue, 0, pos.Y.FloatValue);
            }
            return false;
        }

        private int m_ID;
        private GameObject m_GameObject;
    }
}
