using System;
using UnityEngine;
using XDay.InputAPI;
using XDay.UtilityAPI;

namespace XDay.CameraAPI
{
    public interface IThirdPersonTarget
    {
        /// <summary>
        /// is valid target
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// follow target's position
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// called when follow stopped
        /// </summary>
        void OnStopFollow();
    }

    public enum CollisionResolveOperation
    {
        /// <summary>
        /// 不处理相机碰撞
        /// </summary>
        Ignore,
        /// <summary>
        /// 只有相机collider和其他collider碰撞时才处理相机碰撞
        /// </summary>
        /// <summary>
        OnlyHitCollider,
        /// 总是处理相机碰撞
        /// </summary>
        Always,
    }

    public partial class ThirdPersonCameraController
    {
        public bool IsActive => m_IsActive;

        public ThirdPersonCameraController(IDeviceInput input, float distance, float maxDistance, float xRot, float yRot, 
            IThirdPersonTarget target, Camera camera, LayerMask collisionLayerMask, bool isActive,
            CollisionResolveOperation resolve = CollisionResolveOperation.Always,
            Func<Vector3, bool> isAboveGround = null)
        {
            m_Input = input;
            m_Target = target;
            m_Camera = camera;
            m_MaxDistanceToTarget = maxDistance;
            m_Distance = distance;
            m_XRot = xRot;
            m_YRot = yRot;
            m_CollisionLayerMask = collisionLayerMask;
            m_ResolveOperation = resolve;
            m_IsAboveGround = isAboveGround ?? IsAboveGround;

            DoSetActive(isActive, false);
        }

        public void SetActive(bool active)
        {
            DoSetActive(active, true);
        }

        public void DrawGizmos()
        {
            Gizmos.DrawWireSphere(m_Camera.transform.position, m_CameraCollideRadius);

            var old = Gizmos.color;
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(m_Target.Position, m_CameraCollideRadius);

            Gizmos.color = old;
        }

        public void Update(float dt)
        {
            if (!m_IsActive)
            {
                return;
            }

            if (m_TransitionData != null)
            {
                DoTransition(dt);
            }
            else
            {
                CheckRotate();
                CheckZoom();
                SetPosition(true);
            }
        }

        private void CheckZoom()
        {
            var n = m_Input.TouchCountNotStartFromUI;
            if (n != 2)
            {
                return;
            }

            var touch0 = m_Input.GetTouchNotStartFromUI(0);
            var touch1 = m_Input.GetTouchNotStartFromUI(1);

            var scale = Helper.CalculateScaleAtFixedDepth(m_Camera, m_Distance);

            var isFinished = (touch0.State == TouchState.Finish || touch1.State == TouchState.Finish);
            var isTouching = (touch0.State == TouchState.Touching || touch1.State == TouchState.Touching);

            var curDistance = Vector2.Distance(touch0.Current, touch1.Current);
            var prevDistance = Vector2.Distance(touch0.Previous, touch1.Previous);
            var movedDistance = curDistance - prevDistance;

            if (isTouching &&
                !isFinished &&
                Mathf.Abs(movedDistance) > m_ZoomMoveThreshold)
            {
                m_Distance -= movedDistance * scale.x;
                SetDistance(m_Distance);
            }
        }

        private void CheckRotate()
        {
            var n = m_Input.TouchCountNotStartFromUI;
            if (n != 1)
            {
                return;
            }
            var touch = m_Input.GetTouchNotStartFromUI(0);
            if (touch != null)
            {
                var delta = touch.Current - touch.Previous;
                if (delta != Vector2.zero)
                {
                    Rotate(touch);
                }
            }
        }

        private Vector3 GetDesiredPosition(out Quaternion desiredRotation)
        {
            desiredRotation = Quaternion.Euler(m_XRot, m_YRot, 0);
            var dir = desiredRotation * Vector3.forward;
            var desiredPosition = m_Target.Position - dir * m_Distance;
            return desiredPosition;
        }

        private void SetPosition(bool smooth)
        {
            var desiredPosition = GetDesiredPosition(out _);

            var targetPosition = desiredPosition;
            targetPosition = ResolveCollision(targetPosition);

            if (smooth)
            {
                m_Camera.transform.position = Vector3.SmoothDamp(m_Camera.transform.position, targetPosition, ref m_CurrentVelocity, 1f / m_SmoothSpeed, Mathf.Infinity, Time.deltaTime);
            }
            else
            {
                m_Camera.transform.position = targetPosition;
            }

            m_Camera.transform.LookAt(m_Target.Position);
        }

        private Vector3 ResolveCollision(Vector3 targetPosition)
        {
            if (m_ResolveOperation == CollisionResolveOperation.Ignore)
            {
                return targetPosition;
            }

            if (m_ResolveOperation == CollisionResolveOperation.OnlyHitCollider)
            {
                if (Physics.OverlapSphereNonAlloc(targetPosition, m_CameraCollideRadius, m_TempList, m_CollisionLayerMask) == 0 && m_IsAboveGround.Invoke(targetPosition))
                {
                    //没有任何碰撞,并且相机在地面之上,才跳过碰撞检测
                    return targetPosition;
                }
            }

            var direction = targetPosition - m_Target.Position;
            var maxDistance = direction.magnitude;
            direction.Normalize();
            if (Physics.SphereCast(m_Target.Position, m_CameraCollideRadius, direction, out var hit, maxDistance, m_CollisionLayerMask, QueryTriggerInteraction.Ignore))
            {
                targetPosition = hit.point + hit.normal * (m_CameraCollideRadius + m_CollisionOffset);
            }
            //判断与目标最小距离,过近就不能往下旋转了
            var newDir = m_Target.Position - targetPosition;
            var dis = newDir.magnitude;
            if (dis < m_MinDistanceToTarget)
            {
                m_MinXAngle = m_XRot;
            }
            else
            {
                m_MinXAngle = m_MaxMinXAngle;
            }

            return targetPosition;
        }

        private void Rotate(ITouch touch)
        {
            var cur = touch.Current;
            var prev = touch.Previous;

            var delta = cur - prev;

            var scale = Helper.CalculateScaleAtFixedDepth(m_Camera, m_Distance);
            var worldDelta = scale * delta;

            var angle = DistanceToAngle(worldDelta);

            m_XRot -= angle.y;
            m_XRot = Mathf.Clamp(m_XRot, m_MinXAngle, m_MaxXAngle);

            m_YRot += angle.x;
            m_YRot = Helper.Mod(m_YRot, 360);
        }

        /// <summary>
        /// 距离转换成旋转角度
        /// </summary>
        /// <param name="worldDelta"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private Vector2 DistanceToAngle(Vector2 worldDelta)
        {
            return worldDelta / m_Distance * m_RotateScale * Mathf.Rad2Deg;
        }

        private void SetDistance(float distance)
        {
            m_Distance = distance;
            m_Distance = Mathf.Clamp(m_Distance, m_MinDistanceToTarget, m_MaxDistanceToTarget);
        }

        //坐标在地面之上
        private bool IsAboveGround(Vector3 pos)
        {
            return pos.y >= m_CameraCollideRadius * 0.5f;
        }

        private void DoSetActive(bool active, bool transition)
        {
            if (m_IsActive != active)
            {
                m_IsActive = active;
                if (active && transition)
                {
                    CreateTransitionData();
                }
                else
                {
                    SetPosition(false);
                }
            }
        }

        /// <summary>
        /// 相机离目标的距离
        /// </summary>
        private float m_Distance;
        /// <summary>
        /// 相机离目标的最近距离
        /// </summary>
        private float m_MinDistanceToTarget = 1.5f;
        /// <summary>
        /// 相机离目标的最远距离
        /// </summary>
        private float m_MaxDistanceToTarget = 30f;
        private float m_XRot;
        private float m_YRot;
        private float m_MinXAngle = m_MaxMinXAngle;
        private float m_MaxXAngle = m_MaxMaxXAngle;
        private const float m_MaxMinXAngle = -89.9f;
        private const float m_MaxMaxXAngle = 89.9f;
        private IDeviceInput m_Input;
        /// <summary>
        /// 目标
        /// </summary>
        private IThirdPersonTarget m_Target;
        private readonly Camera m_Camera;
        /// <summary>
        /// 相机碰撞半径
        /// </summary>
        private float m_CameraCollideRadius = 0.2f;
        private float m_CollisionOffset = 0.1f;
        private Vector3 m_CurrentVelocity;
        /// <summary>
        /// 相机移动平滑速度
        /// </summary>
        private float m_SmoothSpeed = 20f;
        /// <summary>
        /// 旋转速度倍率
        /// </summary>
        private float m_RotateScale = 3f;
        /// <summary>
        /// 相机和哪些layer参与碰撞
        /// </summary>
        private LayerMask m_CollisionLayerMask;
        private const float m_ZoomMoveThreshold = 10f;
        private UnityEngine.Collider[] m_TempList = new UnityEngine.Collider[1];
        private CollisionResolveOperation m_ResolveOperation;
        /// <summary>
        /// 检测坐标是否在地面之上
        /// </summary>
        private Func<Vector3, bool> m_IsAboveGround;
        private bool m_IsActive = false;
    }
}
