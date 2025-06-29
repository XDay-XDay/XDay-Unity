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

using XDay.InputAPI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.CameraAPI
{
    internal class CameraManipulator : ICameraManipulator
    {
        public event Action<ICameraManipulator> EventPositionChange;
        public event Action<ICameraManipulator> EventAltitudeChange;
        public event Action<ICameraManipulator> EventActiveStateChange;
        public CameraDirection Direction => m_Setup.Direction;
        public bool EnableFocusPointClamp { set => m_FocusPointClamp.EnableClampXZ = value; get => m_FocusPointClamp.EnableClampXZ; }
        public bool EnableRestore { set => m_FocusPointClamp.EnableRestore = value; get => m_FocusPointClamp.EnableRestore; }
        public bool EnableDrag { set => SetBehaviourActive(BehaviourType.Drag, value); }
        public bool EnableZoom
        {
            set
            {
                SetBehaviourActive(BehaviourType.Pinch, value);
                SetBehaviourActive(BehaviourType.MouseZoom, value);
                SetBehaviourActive(BehaviourType.ScrollZoom, value);
            }
        }
        public float MaxAltitude
        {
            get => m_Setup.MaxAltitude;
            set => m_Setup.MaxAltitude = value;
        }
        public float MinAltitude => m_Setup.MinAltitude;
        public Vector2 AreaMin => m_FocusPointClamp.AreaMin;
        public Vector2 AreaMax => m_FocusPointClamp.AreaMax;
        public Vector3 RenderPosition => m_Transform.CurrentRenderPosition;
        public Vector3 LogicPosition => m_Transform.CurrentLogicPosition;
        public Vector3 FocusPoint => m_FocusPointClamp.Position;
        public Vector3 Forward => m_Transform.CurrentRenderRotation * Vector3.forward;
        public float FocalLength => Vector3.Distance(m_Transform.CurrentLogicPosition, FocusPoint);
        public float ZoomFactor => m_Camera.fieldOfView * FocalLength;
        public CameraSetup Setup => m_Setup;
        public bool IsRenderTransformChanged => m_IsRenderTransformChanged;
        public Camera Camera => m_Camera;

        public void Init(CameraSetup setup, Camera camera, IDeviceInput input)
        {
            if (camera == null)
            {
                Log.Instance?.Warning($"Camera not set, will create default camera!");
                camera = CreateCameraObject(setup.Name);
                m_NeedDestroyCamera = true;
            }

            m_Setup = setup;
            m_Camera = camera;
            m_Camera.farClipPlane = 10000;
            m_Camera.transform.rotation = Quaternion.Euler(m_Setup.Orbit.Pitch, m_Setup.Orbit.Yaw, 0);

            m_Transform = new CameraTransform(camera);

            m_FocusPointClamp = new("Focus Point", camera.gameObject.transform, m_Setup.Direction);
            m_FOVUpdater = new(setup, camera);

            SetBehaviours(input);

            SetPostProcessors();

            SetActive(false);

            if (m_Setup.FocusPointBounds.width > 0 &&
                m_Setup.FocusPointBounds.height > 0)
            {
                EnableFocusPointClamp = true;
                SetFocusPointBounds(m_Setup.FocusPointBounds.min, m_Setup.FocusPointBounds.max);
            }
        }

        public void OnDestroy()
        {
            foreach (var sender in m_Senders)
            {
                sender.OnDestroy();
            }

            if (m_NeedDestroyCamera && m_Camera != null)
            {
                UnityEngine.Object.Destroy(m_Camera.gameObject);
            }
        }

        public void Focus(FocusParam param)
        {
            AddRequest(BehaviourFocus.Request.Create(param));
        }

        public void SetPosition(Vector3 position, Action onCameraReachTarget)
        {
            var request = BehaviourSetPosition.Request.Create(
                layer:0, 
                priority:0,
                RequestQueueType.Replace, 
                position,
                overrideMovement:true,
                interrupters: BehaviourMask.All,
                onCameraReachTarget,
                alwaysInvokeCallback:true);
            AddRequest(request);
        }

        public void SetFocusPointBounds(Vector2 min, Vector2 max)
        {
            m_FocusPointClamp.SetArea(min, max);
        }

        public void SetPenetrationDistance(float distance)
        {
            m_FocusPointClamp.PenetrationDistance = distance;
        }

        public void Shake(float duration, float frequency, float amplitude, bool changeAltitude)
        {
            var shake = QueryPostProcessor<UniformShake>();
            shake.Shake(amplitude, frequency, changeAltitude, duration);
        }

        public void FollowTarget(FollowParam param)
        {
            var request = BehaviourFollow.RequestFollow.Create(param);
            AddRequest(request);
        }

        public void StopFollow()
        {
            var request = BehaviourFollow.RequestStopFollow.Create(layer: 0, priority: 0, queueType: RequestQueueType.Replace);
            AddRequest(request);
        }

        public void SetActive(bool active)
        {
            if (m_Camera != null)
            {
                if (m_Camera.gameObject.TryGetComponent<AudioListener>(out var audioListener))
                {
                    audioListener.enabled = active;
                }

                m_Camera.enabled = active;
            }

            foreach (var sender in m_Senders)
            {
                sender.SetActive(active);
            }

            if (!active)
            {
                foreach (var receiver in m_ActiveReceivers)
                {
                    receiver.Over();
                }
                m_ActiveReceivers.Clear();
            }

            ClearQueue();

            EventActiveStateChange?.Invoke(this);
        }

        public bool IsBehaviourRunning(BehaviourType type)
        {
            foreach (var receiver in m_ActiveReceivers)
            {
                if (receiver.Type == type)
                {
                    return true;
                }
            }
            return false;
        }

        public void LateUpdate()
        {
            m_IsRenderTransformChanged = false;

            if (m_Camera.enabled)
            {
                m_Transform.BeginUpdate(m_Camera);

                UpdateQueue();

                UpdateReceivers();

                Clamp();

                bool postProcessed = UpdatePostProcessors();

                if (m_Transform.IsLogicTransformChanged || postProcessed)
                {
                    m_IsRenderTransformChanged = true;

                    var cameraTransform = m_Camera.transform;
                    cameraTransform.SetPositionAndRotation(m_Transform.CurrentRenderPosition, m_Transform.CurrentRenderRotation);

                    m_FOVUpdater.Update(m_Camera, m_Transform.CurrentRenderPosition.y);

                    if (m_Transform.IsLogicAltitudeChanged)
                    {
                        EventAltitudeChange?.Invoke(this);
                    }
                    EventPositionChange?.Invoke(this);
                }
            }
        }

        internal void AddRequest(BehaviourRequest request)
        {
            if (request.QueueType == RequestQueueType.Drop)
            {
                for (var i = 0; i < m_Queue.Count; ++i)
                {
                    if (m_Queue[i].Type == request.Type)
                    {
                        return;
                    }
                }
            }
            else if (request.QueueType == RequestQueueType.Replace)
            {
                for (var i = m_Queue.Count - 1; i >= 0; i--)
                {
                    if (m_Queue[i].Type == request.Type)
                    {
                        m_Queue[i].OnDestroy(overridden: true);
                        m_Queue.RemoveAt(i);
                    }
                }
            }

            m_Queue.Add(request);
        }

        private void ClearQueue()
        {
            foreach (var request in m_Queue)
            {
                request.OnDestroy(overridden:false);
            }
            m_Queue.Clear();
        }

        private bool Respond(BehaviourRequest request)
        {
            m_Receivers.TryGetValue(request.Type, out var receiver);
            if (receiver != null && receiver.IsActive)
            {
                var hasInterruptedAnyReceiver = true;
                for (var i = m_ActiveReceivers.Count - 1; i >= 0; i--)
                {
                    if (m_ActiveReceivers[i].CanBeInterruptedBy(request))
                    {
                        m_ActiveReceivers[i].OnBeingInterrupted(request);
                        m_ActiveReceivers.RemoveAt(i);
                    }
                    else
                    {
                        hasInterruptedAnyReceiver = false;
                    }
                }

                if (hasInterruptedAnyReceiver)
                {
                    m_ActiveReceivers.Add(receiver);
                    receiver.Respond(request, m_Transform);
                }

                return true;
            }

            return false;
        }

        private void UpdateReceivers()
        {
            for (var i = m_ActiveReceivers.Count - 1; i >= 0; i--)
            {
                if (m_ActiveReceivers[i].Update(m_Transform) == BehaviourState.Over)
                {
                    m_ActiveReceivers.RemoveAt(i);
                }
            }
        }

        private void SetBehaviourActive(BehaviourType type, bool active)
        {
            m_Receivers.TryGetValue(type, out var receiver);
            if (receiver != null)
            {
                receiver.IsActive = active;
            }
        }

        private T QueryPostProcessor<T>() where T : CameraTransformPostProcessor
        {
            var type = typeof(T);
            for (var i = 0; i < m_PostProcessors.Count; ++i)
            {
                if (m_PostProcessors[i].GetType() == type)
                {
                    return m_PostProcessors[i] as T;
                }
            }
            return null;
        }

        private void SetBehaviours(IDeviceInput input)
        {
            m_Senders.Add(new BehaviourMouseZoom.Sender(this, input));
            m_Senders.Add(new BehaviourScrollZoom.Sender(this, input));
            m_Senders.Add(new BehaviourPinch.Sender(this, input, m_Setup.Orbit.MinAltitude, m_Setup.Orbit.MaxAltitude, m_Setup.Orbit.Range, false));
            m_Senders.Add(new BehaviourDrag.Sender(this, input, TouchID.Left));

            m_Receivers.Add(BehaviourType.MouseZoom, new BehaviourMouseZoom.Receiver(this));
            m_Receivers.Add(BehaviourType.ScrollZoom, new BehaviourScrollZoom.Receiver(this, m_Setup.MouseZoomSpeed));
            m_Receivers.Add(BehaviourType.Pinch, new BehaviourPinch.Receiver(this));
            m_Receivers.Add(BehaviourType.Follow, new BehaviourFollow.Receiver(this));
            m_Receivers.Add(BehaviourType.StopFollow, new BehaviourFollow.Receiver(this));
            m_Receivers.Add(BehaviourType.Drag, new BehaviourDrag.Receiver(this, 0.8f, 1.2f));
            m_Receivers.Add(BehaviourType.SetPosition, new BehaviourSetPosition.Receiver(this));
            m_Receivers.Add(BehaviourType.Focus, new BehaviourFocus.Receiver(this));
        }

        private void SetPostProcessors()
        {
            m_PostProcessors.Add(new UniformShake(this));
        }

        private void UpdateQueue()
        {
            m_Queue.Sort((x, y) =>
            {
                if (x.Layer != y.Layer)
                {
                    return x.Layer.CompareTo(y.Layer);
                }
                return x.Priority.CompareTo(y.Priority);
            });
            foreach (var request in m_Queue)
            {
                if (Respond(request))
                {
                    break;
                }
            }
            ClearQueue();
        }

        private bool UpdatePostProcessors()
        {
            m_ActivePostProcessors.Clear();
            foreach (var postProcessor in m_PostProcessors)
            {
                if (postProcessor.IsActive)
                {
                    m_ActivePostProcessors.Add(postProcessor);
                }
            }
            var changed = false;
            var pos = m_Transform.CurrentLogicPosition;
            var rot = m_Transform.CurrentLogicRotation;
            for (var i = 0; i < m_ActivePostProcessors.Count; ++i)
            {
                changed |= m_ActivePostProcessors[i].Update(pos, rot, out var newPos, out var newRot);
                pos = newPos;
                rot = newRot;
            }
            m_Transform.UpdateRenderTransform(pos, rot);
            return changed;
        }

        private void Clamp()
        {
            m_Transform.CurrentLogicPosition = m_FocusPointClamp.Clamp(
                m_Transform.CurrentLogicPosition,
                m_Transform.CurrentLogicRotation * Vector3.forward,
                Setup.Orbit.Pitch, MinAltitude, MaxAltitude, m_Camera);

            m_Transform.UpdateLogicTransform();
        }

        private Camera CreateCameraObject(string name)
        {
            var cameraObj = new GameObject(name);
            cameraObj.AddComponent<AudioListener>();
            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 30, -30);
            return camera;
        }

        private Camera m_Camera;
        private List<CameraTransformPostProcessor> m_PostProcessors = new();
        private List<CameraTransformPostProcessor> m_ActivePostProcessors = new();
        private CameraSetup m_Setup;
        private CameraTransform m_Transform;
        private PositionClamp m_FocusPointClamp;
        private FieldOfViewUpdater m_FOVUpdater;
        private List<BehaviourRequest> m_Queue = new();
        private List<BehaviourRequestSender> m_Senders = new();
        private Dictionary<BehaviourType, BehaviourRequestReceiver> m_Receivers = new();
        private List<BehaviourRequestReceiver> m_ActiveReceivers = new();
        private bool m_IsRenderTransformChanged;
        private bool m_NeedDestroyCamera = false;
    }
}

//XDay