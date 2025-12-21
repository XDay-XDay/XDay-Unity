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
using UnityEngine;
using XDay.WorldAPI;

namespace XDay.UtilityAPI.Shape
{
    [SelectionBase]
    [ExecuteInEditMode]
    public class Shape2DCollider : MonoBehaviour, IObstacle
    {
#if UNITY_EDITOR
        public static List<Shape2DCollider> AllColliders => m_AllColliders;
        public bool IsTransformChanged { get => m_TransformChangeCheck.IsDirty; set => m_TransformChangeCheck.IsDirty = value; }
        public int VertexCount => m_Data.VertexCount;
        public float VertexDisplaySize => m_Data.VertexDisplaySize;
        public bool IsValid => m_Data.IsValid;
        public bool ShowVertexIndex => m_Data.ShowVertexIndex;
        public List<Vector3> VerticesCopy => m_Data.VerticesCopy;
        public List<Vector3> WorldPolygon
        {
            get
            {
                var vertices = VerticesCopy;
                for (var i = 0; i < vertices.Count; ++i)
                {
                    vertices[i] = TransformToWorldPosition(vertices[i]);
                }
                return vertices;
            }
        }
        public Rect WorldBounds => m_Data.WorldBounds;
        internal Shape2DColliderData Data => m_Data;

        private void Awake()
        {
            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                m_AllColliders.Add(this);
            }

            Initialize();
        }

        private void OnDestroy()
        {
            m_AllColliders.Remove(this);
        }

        private void OnEnable()
        {
            m_Data?.Reverse();
        }

        public void Initialize()
        {
            m_Data ??= new Shape2DColliderData();
            m_Data.Initialize(transform);
        }

        public void ResetShape(GameObject gameObject)
        {
            m_Data.Create(new List<GameObject>() { gameObject });
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public Vector3 TransformToLocalPosition(Vector3 worldPosition)
        {
            var transformMatrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), transform.lossyScale);
            var worldToLocal = transformMatrix.inverse;
            return worldToLocal.MultiplyPoint(worldPosition);
        }

        public Vector3 TransformToWorldPosition(Vector3 localPosition)
        {
            var transformMatrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), transform.lossyScale);
            return transformMatrix.MultiplyPoint(localPosition);
        }

        public void MoveShape(Vector3 offset)
        {
            m_Data.MoveShape(offset);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public bool InsertVertex(int index, Vector3 localPosition)
        {
            var inserted = m_Data.InsertVertex(index, localPosition);
            UnityEditor.EditorUtility.SetDirty(this);
            return inserted;
        }

        public bool DeleteVertex(int index)
        {
            var deleted = m_Data.DeleteVertex(index);
            UnityEditor.EditorUtility.SetDirty(this);
            return deleted;
        }

        public void MoveVertex(int index, Vector3 offset)
        {
            m_Data.MoveVertex(index, offset);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public Vector3 GetVertexPosition(int index)
        {
            return m_Data.GetVertexPosition(index);
        }

        public List<Vector3> GetPolyonInLocalSpace()
        {
            return m_Data.GetPolyonInLocalSpace();
        }

        private void Update()
        {
            m_TransformChangeCheck.Update(transform);
        }

        [SerializeField]
        private Shape2DColliderData m_Data;

        private readonly TransformChangedCheck m_TransformChangeCheck = new();

        private static readonly List<Shape2DCollider> m_AllColliders = new();
#else
        public int AreaID => 0;
        public float Height => 0;
        public bool Walkable => false;
        public bool IsValid => false;
        public ObstacleAttribute Attribute => ObstacleAttribute.None;
        public List<Vector3> WorldPolygon => null;
        public Rect WorldBounds => new Rect();
        public static List<Shape2DCollider> AllColliders => null;
        public bool IsTransformChanged { get => false; set { } }
        public int VertexCount => 0;
        public float VertexDisplaySize => 0;
        public bool ShowVertexIndex => false;
        public List<Vector3> VerticesCopy => null;

        public Vector3 GetVertexPosition(int index)
        {
            throw new System.NotImplementedException();
        }

        public Vector3 TransformToLocalPosition(Vector3 worldPos)
        {
            throw new System.NotImplementedException();
        }

        public void MoveVertex(int index, Vector3 moveOffset)
        {
            throw new System.NotImplementedException();
        }

        public void MoveShape(Vector3 moveOffset)
        {
            throw new System.NotImplementedException();
        }

        public bool InsertVertex(int index, Vector3 localPosition)
        {
            throw new System.NotImplementedException();
        }

        public bool DeleteVertex(int index)
        {
            throw new System.NotImplementedException();
        }

        public List<Vector3> GetPolyonInLocalSpace()
        {
            throw new System.NotImplementedException();
        }

        public void ResetShape(GameObject gameObject)
        {
            throw new System.NotImplementedException();
        }

        public Vector3 TransformToWorldPosition(Vector3 vector3)
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}
