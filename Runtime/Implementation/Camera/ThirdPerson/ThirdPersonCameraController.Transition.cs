using UnityEngine;

namespace XDay.CameraAPI
{
    public partial class ThirdPersonCameraController
    {
        private void CreateTransitionData()
        {
            m_TransitionData = new TransitionData();
            m_TransitionData.TargetPosition = GetDesiredPosition(out m_TransitionData.TargetRotation);
        }

        private void DoTransition(float dt)
        {
            var transform = m_Camera.transform;
            transform.SetPositionAndRotation(
                position:Vector3.MoveTowards(transform.position, m_TransitionData.TargetPosition, m_TransitionData.MoveSpeed * Time.deltaTime), rotation:Quaternion.RotateTowards(transform.rotation, m_TransitionData.TargetRotation, m_TransitionData.RotateSpeed * Time.deltaTime));
            if (transform.position == m_TransitionData.TargetPosition &&
                transform.rotation == m_TransitionData.TargetRotation)
            {
                m_TransitionData = null;
            }
        }

        private TransitionData m_TransitionData;

        private class TransitionData
        {
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public float MoveSpeed = 500f;
            public float RotateSpeed = 360;
        }
    }
}
