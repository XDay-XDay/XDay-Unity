using UnityEngine;
using XDay.CameraAPI;
using XDay.InputAPI;

namespace Packages.UniGame.Maps.Editor.MonoBehaviours.WorldPreview
{
    internal class ThirdPersonCameraTest : MonoBehaviour
    {
        public GameObject Target;
        public float Distance;
        public float XRot;
        public float YRot;
        public Camera Camera;

        private void Awake()
        {
            m_Input = IDeviceInput.Create();
            m_Controller = new(m_Input, Distance, 30, XRot, YRot, new Target(Target), Camera, Physics.AllLayers &~ (1 << LayerMask.NameToLayer("Self")), false, CollisionResolveOperation.Always);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_Controller.SetActive(!m_Controller.IsActive);
            }

            m_Input.Update();

            //var x = Input.GetAxis("Horizontal");
            //var y = Input.GetAxis("Vertical");
            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.W))
            {
                y = 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                y = -1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                x = -1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                x = 1;
            }

            var cameraTransform = Camera.transform;
            var right = cameraTransform.right;
            right.y = 0;
            right.Normalize();
            var forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();
            var dir = right * x + forward * y;
            dir.Normalize();

            float moveSpeed = 3f;
            float rotSpeed = 360f;
            Target.transform.position += moveSpeed * Time.deltaTime * dir;
            if (dir != Vector3.zero)
            {
                m_TargetDir = dir;
                //Target.transform.forward = dir;
                //Debug.LogError($"dir is : {dir}");
            }
            Target.transform.forward = Vector3.RotateTowards(Target.transform.forward, m_TargetDir, Time.deltaTime * rotSpeed, 1);

            m_Controller.Update(Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            m_Controller?.DrawGizmos();
        }

        private ThirdPersonCameraController m_Controller;
        private Vector3 m_TargetDir;
        private IDeviceInput m_Input;
    }

    class Target : IThirdPersonTarget
    {
        public bool IsValid => true;
        public Vector3 Position => m_Target.transform.position;

        public Target(GameObject target)
        {
            m_Target = target;
        }

        public void OnStopFollow()
        {
        }

        private GameObject m_Target;
    }
}
