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

using XDay.UtilityAPI;

namespace XDay.GamePlayAPI
{
    public class PhysicsManager
    {
        public PhysicsWorld World => m_PhysicsWorld;

        public PhysicsManager(PhysicsSetting setting, UnityEngine.Vector3 offset, GridCreateInfo gridCreateInfo, bool showDebugger) 
        {
            m_PhysicsWorld = new PhysicsWorld(gridCreateInfo);
            CreateFromColliders(setting, offset, showDebugger);
        }

        public void OnDestroy()
        {
            m_Debugger.OnDestroy();
        }

        public void Tick(FixedPoint deltaTime)
        {
            m_PhysicsWorld.Step(deltaTime, 2);
        }

        public Rigidbody CreateCircleBody(string name, FixedPoint radius, bool isStatic, bool kinematic)
        {
            Log.Instance?.Assert(radius > 0);
            var body = Rigidbody.CreateCircleBody(name, radius, isStatic, m_Material, m_PhysicsWorld);
            body.IsKinematic = kinematic;
            m_PhysicsWorld.AddBody(body);
            return body;
        }

        private void CreateFromColliders(PhysicsSetting setting, UnityEngine.Vector3 offset, bool showDebugger)
        {
            int boxColliderCount = 0;
            int sphereColliderCount = 0;

            foreach (var boxCollider in setting.BoxColliders)
            {
                var body = Rigidbody.CreateBoxBody(boxCollider.Name, (FixedPoint)boxCollider.Width, (FixedPoint)boxCollider.Height, true, m_Material, m_PhysicsWorld);
                body.MoveTo(new FixedVector2(boxCollider.Center.x + offset.x, boxCollider.Center.z + offset.z));
                body.RotateTo((FixedPoint)(boxCollider.Rotation * UnityEngine.Mathf.Deg2Rad));
                m_PhysicsWorld.AddBody(body);

                ++boxColliderCount;
            }

            foreach (var collider in setting.SphereColliders)
            {
                var body = Rigidbody.CreateCircleBody(collider.Name, (FixedPoint)collider.Radius, true, m_Material, m_PhysicsWorld);
                body.RotateTo((FixedPoint)(collider.Rotation * UnityEngine.Mathf.Deg2Rad));
                body.MoveTo(new FixedVector2(collider.Center.x, collider.Center.z));
                m_PhysicsWorld.AddBody(body);

                ++sphereColliderCount;
            }

            Log.Instance?.Info($"Collider count: {boxColliderCount + sphereColliderCount}, box: {boxColliderCount}, sphere: {sphereColliderCount}");

            m_Debugger = new PhysicsWorldDebugger(m_PhysicsWorld);
            m_Debugger.Create();
            m_Debugger.SetVisible(showDebugger);
        }

        public void RemoveBody(Rigidbody body)
        {
            m_PhysicsWorld.RemoveBody(body);
        }

        private readonly PhysicsWorld m_PhysicsWorld;
        private readonly PhysicalMaterial m_Material = new();
        private PhysicsWorldDebugger m_Debugger;
    }
}