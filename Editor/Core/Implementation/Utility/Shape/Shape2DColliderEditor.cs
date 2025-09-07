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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Shape.Editor
{
    [CustomEditor(typeof(Shape2DCollider))]
    [CanEditMultipleObjects]
    internal class Shape2DColliderEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            var createInfo = new ShapeBuilder.ShapeBuilderCreateInfo
            {
                CreateShape = null,
                GetVertexLocalPosition = (int index) => 
                {
                    return m_Colliders[0].GetVertexPosition(index);
                },
                GetVertexCount = () => { return m_Colliders[0].VertexCount; },
                ConvertWorldToLocal = (worldPos) => { return m_Colliders[0].TransformToLocalPosition(worldPos); },
                RepaintInspector = Repaint,

                MoveVertex = (startMoving, index, moveOffset) =>
                {
                    m_Colliders[0].MoveVertex(index, moveOffset);
                    UpdateRenderer(m_Colliders[0]);
                },
                MoveShape = (startMoving, moveOffset) =>
                {
                    m_Colliders[0].MoveShape(moveOffset);
                    UpdateRenderer(m_Colliders[0]);
                },
                InsertVertex = (index, localPosition) =>
                {
                    var inserted = m_Colliders[0].InsertVertex(index, localPosition);
                    UpdateRenderer(m_Colliders[0]);
                    return inserted;
                },
                DeleteVertex = (index) =>
                {
                    var deleted = m_Colliders[0].DeleteVertex(index);
                    UpdateRenderer(m_Colliders[0]);
                    return deleted;
                }
            };

            m_Builder = new ShapeBuilder(createInfo);

            m_Colliders = new Shape2DCollider[targets.Length];
            for (var i = 0; i < targets.Length; ++i)
            {
                m_Colliders[i] = targets[i] as Shape2DCollider;
            }

            m_ColliderSerializedObject = new SerializedObject(m_Colliders);
        }

        public override void OnInspectorGUI()
        {
            DrawEditToggle();

            EditorGUI.BeginChangeCheck();

            m_ColliderSerializedObject.Update();

            var propVertexSize = m_ColliderSerializedObject.FindProperty("m_Data.m_VertexDisplaySize");
            EditorGUILayout.PropertyField(propVertexSize, new GUIContent("顶点大小"), includeChildren: true);

            var propShowVertexIndex = m_ColliderSerializedObject.FindProperty("m_Data.m_ShowVertexIndex");
            EditorGUILayout.PropertyField(propShowVertexIndex, new GUIContent("显示顶点序号"), includeChildren:true);

            var changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
            }

            m_ColliderSerializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var n = Mathf.Min(m_Colliders.Length, m_ColliderMaxDisplayCount);
            for (var i = 0; i < n; ++i)
            {
                if (m_Colliders[i].IsTransformChanged)
                {
                    m_Colliders[i].IsTransformChanged = false;
                    UpdateRenderer(m_Colliders[i]);
                }

                var renderer = GetRenderer(m_Colliders[i]);
                var valid = renderer.Draw(m_EnableEditing);
                if (!valid)
                {
                    m_ColliderRenderers.Remove(m_Colliders[i]);
                }
            }

            DrawShapeBuilder();

            Handles.BeginGUI();

            DrawText();

            Handles.EndGUI();
        }

        private void DrawShapeBuilder()
        {
            if (m_Colliders.Length == 1 && 
                m_EnableEditing)
            {
                m_Builder.DrawSceneGUI(vertexDisplaySize: m_Colliders[0].VertexDisplaySize, m_Colliders[0].transform.position.y, ShapeBuilder.Operation.Edit);
            }
        }

        private void SetEditMode(bool edit)
        {
            m_EnableEditing = edit;
            SceneView.RepaintAll();
        }

        private int GetTotalVertexCount()
        {
            var n = 0;
            foreach (var obstacle in m_Colliders)
            {
                n += obstacle.GetPolyonInLocalSpace().Count;
            }
            return n;
        }

        private void CommandResetShape()
        {
            foreach (var collider in m_Colliders)
            {
                collider.ResetShape(collider.gameObject);
                UpdateRenderer(collider);
            }

            SceneView.RepaintAll();
        }

        private void DrawEditToggle()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = m_Colliders.Length == 1;
            var edit = GUILayout.Toggle(m_EnableEditing, "编辑顶点");
            if (edit != m_EnableEditing)
            {
                SetEditMode(edit);
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            if (GUILayout.Button("重置形状", GUILayout.MaxWidth(80)))
            {
                CommandResetShape();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawText()
        {
            if (m_EnableEditing)
            {
                var style = GetStyle();
                EditorGUILayout.LabelField($"顶点数: {GetTotalVertexCount()}个", style);
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键增加顶点", style);
                EditorGUILayout.LabelField("按住Ctrl+Shit+鼠标左键删除顶点", style);
                EditorGUILayout.LabelField("按住Shift+鼠标左键整体移动形状", style);
            }
        }

        private Shape2DColliderRenderer GetRenderer(Shape2DCollider collider)
        {
            m_ColliderRenderers.TryGetValue(collider, out var renderer);
            if (renderer == null)
            {
                renderer = new Shape2DColliderRenderer(collider);
                m_ColliderRenderers.Add(collider, renderer);
            }
            return renderer;
        }

        private void UpdateRenderer(Shape2DCollider collider)
        {
            EditorUtility.SetDirty(collider);
            var renderer = GetRenderer(collider);
            renderer.SetDirty();
        }

        private GUIStyle GetStyle()
        {
            if (m_TextStyle == null)
            {
                m_TextStyle = new GUIStyle(EditorStyles.label);
                m_TextStyle.normal.textColor = Color.white;
            }
            return m_TextStyle;
        }

        private readonly int m_ColliderMaxDisplayCount = 25;
        private bool m_EnableEditing = false;
        private Shape2DCollider[] m_Colliders;
        private ShapeBuilder m_Builder;
        private SerializedObject m_ColliderSerializedObject;
        private readonly Dictionary<Shape2DCollider, Shape2DColliderRenderer> m_ColliderRenderers = new();
        private GUIStyle m_TextStyle;
    }
}
