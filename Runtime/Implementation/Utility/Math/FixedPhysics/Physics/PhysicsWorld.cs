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

namespace XDay
{
    public enum RestitutionMethod
    {
        Min,
        Max,
        Average,
    }

    public enum FrictionMethod
    {
        Min,
        Max,
        Average,
    }

    public sealed class PhysicsWorld
    {
        public static readonly FixedPoint MinDensity = new FixedPoint(0.5f);
        public static readonly FixedPoint MaxDensity = new FixedPoint(22f);
        public static readonly int MinIterations = 1;
        public static readonly int MaxIterations = 40;

        public int BodyCount => m_BodyList.Count;
        public FixedVector2 Gravity { get => m_Gravity; set => m_Gravity = value; }
        public RestitutionMethod RestitutionMethod { get => m_RestitutionMethod; set => m_RestitutionMethod = value; }
        public FrictionMethod FrictionMethod { get => m_FrictionMethod; set => m_FrictionMethod = value; }

        public PhysicsWorld()
        {
            m_Gravity = new FixedVector2(0f, -9.81f);
            m_BodyList = new List<Rigidbody>();
            m_ContactPairs = new List<Pair>();

            m_ContactList = new FixedVector2[2];
            m_ImpulseList = new FixedVector2[2];
            m_RAList = new FixedVector2[2];
            m_RBList = new FixedVector2[2];
            m_FrictionImpulseList = new FixedVector2[2];
            m_ImpulseStrengthList = new FixedPoint[2];
        }

        public void AddBody(Rigidbody body)
        {
            m_BodyList.Add(body);
        }

        public bool RemoveBody(Rigidbody body)
        {
            return m_BodyList.Remove(body);
        }

        public bool GetBody(int index, out Rigidbody body)
        {
            body = null;

            if (index < 0 || index >= m_BodyList.Count)
            {
                return false;
            }

            body = m_BodyList[index];
            return true;
        }

        public void Step(FixedPoint deltaTime, int totalIterations)
        {
            totalIterations = Math.Clamp(totalIterations, MinIterations, MaxIterations);

            for (int currentIteration = 0; currentIteration < totalIterations; currentIteration++)
            {
                m_ContactPairs.Clear();
                StepBodies(deltaTime, totalIterations);
                BroadPhase();
                NarrowPhase();
            }

            foreach (var body in m_BodyList)
            {
                body.StepOver();
            }
        }

        private void BroadPhase()
        {
            for (int i = 0; i < m_BodyList.Count - 1; i++)
            {
                Rigidbody bodyA = m_BodyList[i];
                FixedAABB aabbA = bodyA.GetAABB();

                for (int j = i + 1; j < m_BodyList.Count; j++)
                {
                    Rigidbody bodyB = m_BodyList[j];

                    if (bodyA.IsStatic && bodyB.IsStatic) 
                    {
                        //static物体之间不碰撞
                        continue;
                    }

                    if (!bodyA.ResolveCollision || !bodyB.ResolveCollision)
                    {
                        continue;
                    }

                    if (!FixedCollision.IntersectAABBs(aabbA, bodyB.GetAABB()))
                    {
                        continue;
                    }

                    m_ContactPairs.Add(new Pair(i, j));
                }
            }
        }

        private void NarrowPhase()
        {
            foreach (var pair in m_ContactPairs)
            {
                Rigidbody bodyA = m_BodyList[pair.Item1];
                Rigidbody bodyB = m_BodyList[pair.Item2];

                if (FixedCollision.Collide(bodyA, bodyB, out FixedVector2 normal, out FixedPoint depth))
                {
                    SeparateBodies(bodyA, bodyB, normal * depth);

                    if (bodyA.IsKinematic && bodyB.IsKinematic)
                    {
                        continue;
                    }

                    FixedCollision.FindContactPoints(bodyA, bodyB, out FixedVector2 contact1, out FixedVector2 contact2, out int contactCount);
                    FixedManifold contact = new(bodyA, bodyB, normal, depth, contact1, contact2, contactCount);
                    ResolveCollisionWithRotationAndFriction(in contact);
                }
            }
        }

        public void StepBodies(FixedPoint deltaTime, int totalIterations)
        {
            foreach (var body in m_BodyList)
            {
                if (!body.IsKinematic && body.EnableGravity)
                {
                    body.AddForce(m_Gravity);
                }
                body.Step(deltaTime, totalIterations);
            }
        }

        private void SeparateBodies(Rigidbody bodyA, Rigidbody bodyB, FixedVector2 sv)
        {
            if (bodyA.IsStatic || bodyA.LinearVelocity == FixedVector2.Zero)
            {
                bodyB.Move(sv);
            }
            else if (bodyB.IsStatic || bodyB.LinearVelocity == FixedVector2.Zero)
            {
                bodyA.Move(-sv);
            }
            else
            {
                bodyA.Move(-sv / 2);
                bodyB.Move(sv / 2);
            }
        }

        public void ResolveCollisionWithRotationAndFriction(in FixedManifold contact)
        {
            Rigidbody bodyA = contact.BodyA;
            Rigidbody bodyB = contact.BodyB;
            FixedVector2 normal = contact.Normal;
            FixedVector2 contact1 = contact.Contact1;
            FixedVector2 contact2 = contact.Contact2;
            int contactCount = contact.ContactCount;

            var resitution = GetRestitution(bodyA, bodyB);
            GetFriction(bodyA, bodyB, out var staticFriction, out var dynamicFriction);

            m_ContactList[0] = contact1;
            m_ContactList[1] = contact2;

            for (int i = 0; i < contactCount; i++)
            {
                FixedVector2 ra = m_ContactList[i] - bodyA.Position;
                FixedVector2 rb = m_ContactList[i] - bodyB.Position;

                m_RAList[i] = ra;
                m_RBList[i] = rb;

                FixedVector2 raPerp = new FixedVector2(-ra.Y, ra.X);
                FixedVector2 rbPerp = new FixedVector2(-rb.Y, rb.X);

                FixedVector2 angularLinearVelocityA = bodyA.EnableRotation ? raPerp * bodyA.AngularVelocity : FixedVector2.Zero;
                FixedVector2 angularLinearVelocityB = bodyB.EnableRotation ? rbPerp * bodyB.AngularVelocity : FixedVector2.Zero;

                FixedVector2 relativeVelocity =
                    (bodyB.LinearVelocity + angularLinearVelocityB) -
                    (bodyA.LinearVelocity + angularLinearVelocityA);

                var contactVelocityMag = FixedVector2.Dot(relativeVelocity, normal);

                if (contactVelocityMag > 0)
                {
                    continue;
                }

                var raPerpDotN = FixedVector2.Dot(raPerp, normal);
                var rbPerpDotN = FixedVector2.Dot(rbPerp, normal);

                var denom = bodyA.InvMass + bodyB.InvMass +
                    (raPerpDotN * raPerpDotN) * bodyA.InvInertia +
                    (rbPerpDotN * rbPerpDotN) * bodyB.InvInertia;

                var impulseMag = -(FixedPoint.One + resitution) * contactVelocityMag;
                impulseMag /= denom;
                impulseMag /= contactCount;

                m_ImpulseStrengthList[i] = impulseMag;
                m_ImpulseList[i] = impulseMag * normal;
            }

            //rotation
            for (int i = 0; i < contactCount; i++)
            {
                FixedVector2 impulse = m_ImpulseList[i];

                if (!bodyA.IsKinematic)
                {
                    FixedVector2 ra = m_RAList[i];
                    bodyA.LinearVelocity += -impulse * bodyA.InvMass;
                    if (bodyA.EnableRotation)
                    {
                        bodyA.AngularVelocity += -FixedVector2.Cross(ra, impulse) * bodyA.InvInertia;
                    }
                }

                if (!bodyB.IsKinematic)
                {
                    FixedVector2 rb = m_RBList[i];
                    bodyB.LinearVelocity += impulse * bodyB.InvMass;
                    if (bodyB.EnableRotation)
                    {
                        bodyB.AngularVelocity += FixedVector2.Cross(rb, impulse) * bodyB.InvInertia;
                    }
                }
            }

            //friction
            for (int i = 0; i < contactCount; i++)
            {
                FixedVector2 ra = m_RAList[i];
                FixedVector2 rb = m_RBList[i];

                FixedVector2 raPerp = new FixedVector2(-ra.Y, ra.X);
                FixedVector2 rbPerp = new FixedVector2(-rb.Y, rb.X);

                FixedVector2 angularLinearVelocityA = bodyA.EnableRotation ? raPerp * bodyA.AngularVelocity : FixedVector2.Zero;
                FixedVector2 angularLinearVelocityB = bodyB.EnableRotation ? rbPerp * bodyB.AngularVelocity : FixedVector2.Zero;
                FixedVector2 relativeVelocity = (bodyB.LinearVelocity + angularLinearVelocityB) - (bodyA.LinearVelocity + angularLinearVelocityA);
                FixedVector2 tangent = relativeVelocity - FixedVector2.Dot(relativeVelocity, normal) * normal;

                if (tangent == FixedVector2.Zero)
                {
                    continue;
                }
                else
                {
                    tangent.Normalize();
                }

                var raPerpDotT = FixedVector2.Dot(raPerp, tangent);
                var rbPerpDotT = FixedVector2.Dot(rbPerp, tangent);

                var denom = bodyA.InvMass + bodyB.InvMass +
                    (raPerpDotT * raPerpDotT) * bodyA.InvInertia +
                    (rbPerpDotT * rbPerpDotT) * bodyB.InvInertia;

                var tangentImpulse = -FixedVector2.Dot(relativeVelocity, tangent);
                tangentImpulse /= denom;
                tangentImpulse /= contactCount;

                FixedVector2 frictionImpulse;
                var impulseMag = m_ImpulseStrengthList[i];

                if (tangentImpulse.Abs() <= impulseMag * staticFriction)
                {
                    frictionImpulse = tangentImpulse * tangent;
                }
                else
                {
                    frictionImpulse = -impulseMag * tangent * dynamicFriction;
                }

                m_FrictionImpulseList[i] = frictionImpulse;
            }

            //apply friction
            for (int i = 0; i < contactCount; i++)
            {
                FixedVector2 frictionImpulse = m_FrictionImpulseList[i];

                if (!bodyA.IsKinematic)
                {
                    FixedVector2 ra = m_RAList[i];
                    bodyA.LinearVelocity += -frictionImpulse * bodyA.InvMass;
                    if (bodyA.EnableRotation)
                    {
                        bodyA.AngularVelocity += -FixedVector2.Cross(ra, frictionImpulse) * bodyA.InvInertia;
                    }
                }

                if (!bodyB.IsKinematic)
                {
                    FixedVector2 rb = m_RBList[i];
                    bodyB.LinearVelocity += frictionImpulse * bodyB.InvMass;
                    if (bodyB.EnableRotation)
                    {
                        bodyB.AngularVelocity += FixedVector2.Cross(rb, frictionImpulse) * bodyB.InvInertia;
                    }
                }
            }
        }

        private FixedPoint GetRestitution(Rigidbody bodyA, Rigidbody bodyB)
        {
            if (m_RestitutionMethod == RestitutionMethod.Min)
            {
                return FixedMath.Min(bodyA.Material.Restitution, bodyB.Material.Restitution);
            }

            if (m_RestitutionMethod == RestitutionMethod.Max)
            {
                return FixedMath.Max(bodyA.Material.Restitution, bodyB.Material.Restitution);
            }

            if (m_RestitutionMethod == RestitutionMethod.Average)
            {
                return (bodyA.Material.Restitution + bodyB.Material.Restitution) / 2;
            }

            Log.Instance?.Error($"Invalid type");
            return 0;
        }

        private void GetFriction(Rigidbody bodyA, Rigidbody bodyB, out FixedPoint staticFriction, out FixedPoint dynamicFriction)
        {
            if (m_FrictionMethod == FrictionMethod.Average)
            {
                staticFriction = (bodyA.Material.StaticFriction + bodyB.Material.StaticFriction) / 2;
                dynamicFriction = (bodyA.Material.DynamicFriction + bodyB.Material.DynamicFriction) / 2;
                return;
            }

            if (m_FrictionMethod == FrictionMethod.Max)
            {
                staticFriction = FixedMath.Max(bodyA.Material.StaticFriction, bodyB.Material.StaticFriction);
                dynamicFriction = FixedMath.Max(bodyA.Material.DynamicFriction, bodyB.Material.DynamicFriction);
                return;
            }

            if (m_FrictionMethod == FrictionMethod.Min)
            {
                staticFriction = FixedMath.Min(bodyA.Material.StaticFriction, bodyB.Material.StaticFriction);
                dynamicFriction = FixedMath.Min(bodyA.Material.DynamicFriction, bodyB.Material.DynamicFriction);
                return;
            }

            Log.Instance?.Error($"Invalid friction type");
            staticFriction = FixedPoint.Zero;
            dynamicFriction = FixedPoint.Zero;
        }

        private FixedVector2 m_Gravity;
        private List<Rigidbody> m_BodyList;
        private List<Pair> m_ContactPairs;
        private FixedVector2[] m_ContactList;
        private FixedVector2[] m_ImpulseList;
        private FixedVector2[] m_RAList;
        private FixedVector2[] m_RBList;
        private FixedVector2[] m_FrictionImpulseList;
        private FixedPoint[] m_ImpulseStrengthList;
        private RestitutionMethod m_RestitutionMethod = RestitutionMethod.Min;
        private FrictionMethod m_FrictionMethod = FrictionMethod.Average;

        private struct Pair
        {
            public Pair(int item1, int item2)
            {
                Item1 = item1;
                Item2 = item2;
            }
            public int Item1;
            public int Item2;
        }
    }
}
