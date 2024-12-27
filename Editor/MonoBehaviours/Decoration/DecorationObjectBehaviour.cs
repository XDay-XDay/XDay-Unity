/*
 * Copyright (c) 2024 XDay
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
        public Vector3 InitLocalPosition { get; private set; }
        public Quaternion InitLocalRotation { get; private set; }
        public Vector3 InitLocalScale { get; private set; }
        public int ObjectID => m_ObjectID;

        public void Init(int objectID, Action<int> onTransformChange)
        {
            m_ObjectID = objectID;
            m_OnTransformChange = onTransformChange;
        }

        public void RecordTransform()
        {
            InitLocalPosition = transform.localPosition;
            InitLocalRotation = transform.localRotation;
            InitLocalScale = transform.localScale;
        }

        public void Check()
        {
            if (transform.localPosition != InitLocalPosition)
            {
                m_OnTransformChange?.Invoke(m_ObjectID);
                InitLocalPosition = transform.localPosition;
            }

            if (transform.localRotation != InitLocalRotation)
            {
                m_OnTransformChange?.Invoke(m_ObjectID);
                InitLocalRotation = transform.localRotation;
            }

            if (transform.localScale != InitLocalScale)
            {
                m_OnTransformChange?.Invoke(m_ObjectID);
                InitLocalScale = transform.localScale;
            }
        }

        private int m_ObjectID;
        Action<int> m_OnTransformChange;
    }

    [CustomEditor(typeof(DecorationObjectBehaviour))]
    [CanEditMultipleObjects]
    class FreeObjectEventListenerEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
           foreach (var target in targets)
            {
                var obj = target as DecorationObjectBehaviour;
                obj.RecordTransform();
            }
        }

        public override void OnInspectorGUI()
        {
            var obj = target as DecorationObjectBehaviour;
            EditorGUILayout.LabelField($"ID: {obj.ObjectID}");
        }

        private void OnSceneGUI()
        {
            var obj = target as DecorationObjectBehaviour;
            obj.Check();

            SceneView.RepaintAll();
        }
    }
}

#endif