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
            m_GameObject.transform.localScale = sphereCollider.Radius.RawFloat * 2 * Vector3.one;
        }
        else if (body.Collider is XDay.BoxCollider boxCollider)
        {
            m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_GameObject.transform.localScale = new Vector3(boxCollider.Width.RawFloat, 1, boxCollider.Height.RawFloat);
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
            new Vector3(pos.X.RawFloat, 0, pos.Y.RawFloat), 
            Quaternion.Euler(0, -m_Body.Angle.RawFloat * Mathf.Rad2Deg, 0));
    }

    private XDay.Rigidbody m_Body;
    private PhysicsWorld m_World;
    private GameObject m_GameObject;
}

