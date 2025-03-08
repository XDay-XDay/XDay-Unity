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

using XDay;
using UnityEngine;
using XDay.UtilityAPI;

internal class Entity
{
    public Entity(XDay.Rigidbody body, PhysicsWorld world)
    {
        m_Body = body;
        m_World = world;
        m_World.AddBody(body);
        if (body.Collider is XDay.SphereCollider sphereCollider)
        {
            m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_GameObject.transform.localScale = sphereCollider.Radius.FloatValue * 2 * Vector3.one;
        }
        else if (body.Collider is XDay.BoxCollider boxCollider)
        {
            m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_GameObject.transform.localScale = new Vector3(boxCollider.Width.FloatValue, 1, boxCollider.Height.FloatValue);
        }
        m_GameObject.DestroyComponent<UnityEngine.Collider>();
    }

    public void OnDestroy()
    {
        m_World.RemoveBody(m_Body);
        Helper.DestroyUnityObject(m_GameObject);
    }

    public void SetPosition(Vector3 pos)
    {
        m_Body.MoveTo(new FixedVector2(pos.x, pos.z));
    }

    public void SetRotation(float angle)
    {
        m_Body.RotateTo(new FixedPoint(angle * Mathf.Deg2Rad));
    }

    public void SetMaterial(Material material)
    {
        m_GameObject.GetComponent<Renderer>().sharedMaterial = material;
    }

    public void AddForce(Vector3 force)
    {
        m_Body.AddForce(new FixedVector2(force.x, force.z));
    }

    public void Sync()
    {
        var pos = m_Body.Position;
        m_GameObject.transform.SetPositionAndRotation(
            new Vector3(pos.X.FloatValue, 0, pos.Y.FloatValue), 
            Quaternion.Euler(0, -m_Body.Angle.FloatValue * Mathf.Rad2Deg, 0));
    }

    private XDay.Rigidbody m_Body;
    private PhysicsWorld m_World;
    private GameObject m_GameObject;
}

