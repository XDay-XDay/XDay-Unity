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

using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUutilities;

internal class PhyEntity
{
    public int ID => m_ID;
    public string Name => m_Name;
    public BEPUphysics.Entities.Entity Handle => m_Handle;
    public Vector3 Position => m_Handle.Position;
    public Quaternion Orientation => m_Handle.Orientation;

    public PhyEntity(int id, string name, bool dynamic, BEPUphysics.Space space, BEPUphysics.Entities.Entity entity)
    {
        m_ID = id;
        m_Name = name;
        m_Space = space;
        m_Handle = entity;
        if (!dynamic)
        {
            m_Handle.BecomeKinematic();
        }
        m_Handle.CollisionInformation.Events.InitialCollisionDetected += OnCollisionStart;
        m_Handle.CollisionInformation.Events.CollisionEnded += OnCollisionEnd;
        m_Space.Add(m_Handle);
    }

    public virtual void OnDestroy()
    {
        m_Space.Remove(m_Handle);
    }

    public void LockRotation(bool lockX, bool lockY, bool lockZ)
    {
        var mat = m_Handle.LocalInertiaTensorInverse;
        if (lockX)
        {
            mat.M11 = 0;
        }
        if (lockY)
        {
            mat.M22 = 0;
        }
        if (lockZ)
        {
            mat.M33 = 0;
        }
        m_Handle.LocalInertiaTensorInverse = mat;
    }

    public void SetVelocity(BEPUutilities.Vector3 velocity)
    {
        m_Handle.LinearVelocity = velocity;
    }

    protected virtual void OnCollisionEnd(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
    {
    }

    protected virtual void OnCollisionStart(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
    {
    }

    private readonly int m_ID;
    private readonly string m_Name;
    private readonly BEPUphysics.Entities.Entity m_Handle;
    private readonly BEPUphysics.Space m_Space;
}

