using System.IO;
using UnityEngine;
using XDay;

public class PhysicsTest : MonoBehaviour
{

    public string PhysicsWorldConfigFilePath;
    public Transform Player;
    public float Speed = 3.0f;

#if false

    void Start()
    {
        Log.Init(true, () => {
            Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
        }, () => {
            Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
        }, Application.persistentDataPath);

        var stream = new FileStream(PhysicsWorldConfigFilePath, FileMode.Open);
        m_World = IPhysicsWorld.Create(stream);
        stream.Close();

        var config = new CylinderColliderConfig()
        {
            Name = "Player",
            Radius = Player.transform.localScale.x * 0.5f,
            Position = Player.transform.position,
        };
        m_PlayerCollider = ICylinderCollider.Create(config);
    }

    void FixedUpdate()
    {
        Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"),
            0, Input.GetAxis("Vertical"));
        inputDir.Normalize();
        m_Velocity = new FixedVector3(inputDir);

        if (inputDir != Vector3.zero)
        {
            int a = 1;
        }

        m_PlayerCollider.Position += m_Velocity * (FixedPoint)Speed * (FixedPoint)Time.deltaTime;

        FixedVector3 adjust = FixedVector3.Zero;
        bool hit = m_World.HitTest(m_PlayerCollider, ref m_Velocity, ref adjust);
        if (hit)
        {
            Log.Instance?.Info($"Hit");
        }

        m_PlayerCollider.Position += adjust;

        m_PlayerPos = m_PlayerCollider.Position;

        Player.position = m_PlayerPos.ToVector3();
    }

    private void OnUnityLogMessageReceived(string message, string stackTrace, UnityEngine.LogType type)
    {
        Log.Instance?.LogMessage(message, stackTrace, ToUnityLogType(type));
    }

    private XDay.LogType ToUnityLogType(UnityEngine.LogType type)
    {
        if (type == UnityEngine.LogType.Log)
        {
            return XDay.LogType.Log;
        }

        if (type == UnityEngine.LogType.Error)
        {
            return XDay.LogType.Error;
        }

        if (type == UnityEngine.LogType.Warning)
        {
            return XDay.LogType.Warning;
        }

        if (type == UnityEngine.LogType.Assert)
        {
            return XDay.LogType.Assert;
        }

        if (type == UnityEngine.LogType.Exception)
        {
            return XDay.LogType.Exception;
        }

        Debug.Assert(false);
        return XDay.LogType.Exception;
    }

    private FixedVector3 m_PlayerPos;
    private IPhysicsWorld m_World;
    private ICylinderCollider m_PlayerCollider;
    private FixedVector3 m_Velocity;
#endif
}
