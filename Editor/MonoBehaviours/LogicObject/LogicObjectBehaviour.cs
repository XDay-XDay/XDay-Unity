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

namespace XDay.WorldAPI.LogicObject.Editor
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class LogicObjectBehaviour : MonoBehaviour
    {
        public int ObjectID => m_ObjectID;
        public Vector3 RecordedScale { get; private set; }
        public Quaternion RecordedRot { get; private set; }
        public Vector3 RecordedPos { get; private set; }

        public void Init(int objectID, Action<int> transformChangeCallback)
        {
            m_ObjectID = objectID;
            m_TransformChangedCallback = transformChangeCallback;
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
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(LogicObjectBehaviour))]
    class LogicObjectBehaviourEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            foreach (var target in targets)
            {
                var behaviour = target as LogicObjectBehaviour;
                behaviour.Record();
            }
        }

        private void OnSceneGUI()
        {
            var behaviour = target as LogicObjectBehaviour;
            behaviour.UpdateTranfsorm();

            SceneView.RepaintAll();
        }

        public override void OnInspectorGUI()
        {
            var behaviour = target as LogicObjectBehaviour;
            EditorGUILayout.LabelField($"Object ID: {behaviour.ObjectID}");
        }
    }
}

#endif

//XDay