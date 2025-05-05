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

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Decoration.Editor
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class DecorationObjectBehaviour : MonoBehaviour
    {
        public int ObjectID => m_ObjectID;
        public Vector3 RecordedScale { get; private set; }
        public Quaternion RecordedRot { get; private set; }
        public Vector3 RecordedPos { get; private set; }

        public void Init(
            int objectID, 
            Func<int, bool> getEnableHeightAdjust,
            Action<int, bool> setEnableHeightAdjust,
            Func<int, bool> getEnableInstanceRendering,
            Action<int, bool> setEnableInstanceRendering,
            Action<int> transformChangeCallback)
        {
            m_ObjectID = objectID;
            m_TransformChangedCallback = transformChangeCallback;
            m_GetEnableHeightAdjust = getEnableHeightAdjust;
            m_GetEnableInstanceRendering = getEnableInstanceRendering;
            m_SetEnableHeightAdjust = setEnableHeightAdjust;
            m_SetEnableInstanceRendering = setEnableInstanceRendering;
        }

        public bool GetEnableHeightAdjust()
        {
            return m_GetEnableHeightAdjust(m_ObjectID);
        }

        public bool GetEnableInstanceRendering()
        {
            return m_GetEnableInstanceRendering(m_ObjectID);
        }

        public void SetEnableHeightAdjust(bool enable)
        {
            m_SetEnableHeightAdjust(m_ObjectID, enable);
        }

        public void SetEnableInstanceRendering(bool enable)
        {
            m_SetEnableInstanceRendering(m_ObjectID, enable);
        }

        public void UpdateTranfsorm()
        {
            if (transform.localScale != RecordedScale)
            {
                m_TransformChangedCallback?.Invoke(m_ObjectID);
                RecordedScale = transform.localScale;
            }

            if (transform.localRotation != RecordedRot)
            {
                m_TransformChangedCallback?.Invoke(m_ObjectID);
                RecordedRot = transform.localRotation;
            }

            if (transform.localPosition != RecordedPos)
            {
                m_TransformChangedCallback?.Invoke(m_ObjectID);
                RecordedPos = transform.localPosition;
            }
        }

        public void Record()
        {
            RecordedPos = transform.localPosition;
            RecordedRot = transform.localRotation;
            RecordedScale = transform.localScale;
        }

        private int m_ObjectID;
        private Action<int> m_TransformChangedCallback;
        private Func<int, bool> m_GetEnableHeightAdjust;
        private Action<int, bool> m_SetEnableHeightAdjust;
        private Func<int, bool> m_GetEnableInstanceRendering;
        private Action<int, bool> m_SetEnableInstanceRendering;
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(DecorationObjectBehaviour))]
    class DecorationObjectBehaviourEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
           foreach (var target in targets)
            {
                var behaviour = target as DecorationObjectBehaviour;
                behaviour.Record();
            }
        }

        private void OnSceneGUI()
        {
            var behaviour = target as DecorationObjectBehaviour;
            behaviour.UpdateTranfsorm();

            SceneView.RepaintAll();
        }

        public override void OnInspectorGUI()
        {
            var behaviour = target as DecorationObjectBehaviour;
            EditorGUILayout.LabelField($"Object ID: {behaviour.ObjectID}");
            behaviour.SetEnableHeightAdjust(EditorGUILayout.Toggle("开启自适应高度修改", behaviour.GetEnableHeightAdjust()));
            behaviour.SetEnableInstanceRendering(EditorGUILayout.Toggle("开启Instance Rendering", behaviour.GetEnableInstanceRendering()));
        }
    }
}

#endif

//XDay