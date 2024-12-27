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

using UnityEngine;
using UnityEditor;

namespace XDay.WorldAPI.Decoration.Editor
{
    [ExecuteInEditMode]
    public class SubObject : MonoBehaviour
    {
        public void Init()
        {
            InitLocalPosition = transform.localPosition;
            InitLocalRotation = transform.localRotation;
            InitLocalScale = transform.localScale;
        }

        public void Check()
        {
            if (transform.localPosition != InitLocalPosition)
            {
                transform.localPosition = InitLocalPosition;
            }

            if (transform.localRotation != InitLocalRotation)
            {
                transform.localRotation = InitLocalRotation;
            }

            if (transform.localScale != InitLocalScale)
            {
                transform.localScale = InitLocalScale;
            }
        }

        public Vector3 InitLocalPosition { get; private set; }
        public Quaternion InitLocalRotation { get; private set; }
        public Vector3 InitLocalScale { get; private set; }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SubObject))]
    internal class SubObjectEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            foreach (var target in targets)
            {
                var obj = target as SubObject;
                obj.Init();
            }
        }

        private void OnSceneGUI()
        {
            var obj = target as SubObject;
            obj.Check();

            SceneView.RepaintAll();
        }
    }
}

#endif