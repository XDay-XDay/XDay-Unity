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

using UnityEngine;

namespace XDay.UtilityAPI
{
    public struct PackedMatrix
    {
        public float m00;
        public float m10;
        public float m20;
        public float m01;
        public float m11;
        public float m21;
        public float m02;
        public float m12;
        public float m22;
        public float m03;
        public float m13;
        public float m23;

        public Vector3 Translation => new(m03, m13, m23);
        public Vector3 Scale
        {
            get
            {
                Matrix4x4 m = ToMatrix();
                return m.lossyScale;
            }
        }
        public Quaternion Rotation
        {
            get
            {
                Matrix4x4 m = ToMatrix();
                return m.rotation;
            }
        }

        public PackedMatrix(Matrix4x4 mat)
        {
            m00 = mat.m00;
            m10 = mat.m10;
            m20 = mat.m20;
            m01 = mat.m01;
            m11 = mat.m11;
            m21 = mat.m21;
            m02 = mat.m02;
            m12 = mat.m12;
            m22 = mat.m22;
            m03 = mat.m03;
            m13 = mat.m13;
            m23 = mat.m23;
        }

        public Matrix4x4 ToMatrix()
        {
            return new()
            {
                m00 = m00,
                m01 = m01,
                m02 = m02,
                m03 = m03,
                m10 = m10,
                m11 = m11,
                m12 = m12,
                m13 = m13,
                m20 = m20,
                m21 = m21,
                m22 = m22,
                m23 = m23,
                m30 = 0,
                m31 = 0,
                m32 = 0,
                m33 = 1,
            };
        }

        public PackedMatrix GetInverse()
        {
            float m33 = 1.0f;
            float m32 = 0.0f;
            float m31 = 0.0f;
            float m30 = 0.0f;
            float inv0, inv1, inv2, inv3, inv4, inv5, inv6, inv7, inv8, inv9, inv10, inv11, inv12, inv13, inv14, inv15;
            inv0 = m11 * m22 * m33 - m11 * m23 * m32 - m21 * m12 * m33 + m21 * m13 * m32 + m31 * m12 * m23 - m31 * m13 * m22;
            inv4 = -m10 * m22 * m33 + m10 * m23 * m32 + m20 * m12 * m33 - m20 * m13 * m32 - m30 * m12 * m23 + m30 * m13 * m22;
            inv8 = m10 * m21 * m33 - m10 * m23 * m31 - m20 * m11 * m33 + m20 * m13 * m31 + m30 * m11 * m23 - m30 * m13 * m21;
            inv12 = -m10 * m21 * m32 + m10 * m22 * m31 + m20 * m11 * m32 - m20 * m12 * m31 - m30 * m11 * m22 + m30 * m12 * m21;
            inv1 = -m01 * m22 * m33 + m01 * m23 * m32 + m21 * m02 * m33 - m21 * m03 * m32 - m31 * m02 * m23 + m31 * m03 * m22;
            inv5 = m00 * m22 * m33 - m00 * m23 * m32 - m20 * m02 * m33 + m20 * m03 * m32 + m30 * m02 * m23 - m30 * m03 * m22;
            inv9 = -m00 * m21 * m33 + m00 * m23 * m31 + m20 * m01 * m33 - m20 * m03 * m31 - m30 * m01 * m23 + m30 * m03 * m21;
            inv13 = m00 * m21 * m32 - m00 * m22 * m31 - m20 * m01 * m32 + m20 * m02 * m31 + m30 * m01 * m22 - m30 * m02 * m21;
            inv2 = m01 * m12 * m33 - m01 * m13 * m32 - m11 * m02 * m33 + m11 * m03 * m32 + m31 * m02 * m13 - m31 * m03 * m12;
            inv6 = -m00 * m12 * m33 + m00 * m13 * m32 + m10 * m02 * m33 - m10 * m03 * m32 - m30 * m02 * m13 + m30 * m03 * m12;
            inv10 = m00 * m11 * m33 - m00 * m13 * m31 - m10 * m01 * m33 + m10 * m03 * m31 + m30 * m01 * m13 - m30 * m03 * m11;
            inv14 = -m00 * m11 * m32 + m00 * m12 * m31 + m10 * m01 * m32 - m10 * m02 * m31 - m30 * m01 * m12 + m30 * m02 * m11;
            inv3 = -m01 * m12 * m23 + m01 * m13 * m22 + m11 * m02 * m23 - m11 * m03 * m22 - m21 * m02 * m13 + m21 * m03 * m12;
            inv7 = m00 * m12 * m23 - m00 * m13 * m22 - m10 * m02 * m23 + m10 * m03 * m22 + m20 * m02 * m13 - m20 * m03 * m12;
            inv11 = -m00 * m11 * m23 + m00 * m13 * m21 + m10 * m01 * m23 - m10 * m03 * m21 - m20 * m01 * m13 + m20 * m03 * m11;
            inv15 = m00 * m11 * m22 - m00 * m12 * m21 - m10 * m01 * m22 + m10 * m02 * m21 + m20 * m01 * m12 - m20 * m02 * m11;

            float det = m00 * inv0 + m01 * inv4 + m02 * inv8 + m03 * inv12;
            if (det == 0)
            {
                Debug.LogError("Can't be inverted!");
                return new PackedMatrix();
            }

            det = 1.0f / det;

            return new PackedMatrix()
            {
                m00 = inv0 * det,
                m01 = inv1 * det,
                m02 = inv2 * det,
                m03 = inv3 * det,
                m10 = inv4 * det,
                m11 = inv5 * det,
                m12 = inv6 * det,
                m13 = inv7 * det,
                m20 = inv8 * det,
                m21 = inv9 * det,
                m22 = inv10 * det,
                m23 = inv11 * det,
            };
        }

        public static PackedMatrix TRS(ref Vector3 t, ref Quaternion r, ref Vector3 s)
        {
            return new PackedMatrix()
            {
                m00 = (1.0f - 2.0f * (r.y * r.y + r.z * r.z)) * s.x,
                m10 = (r.x * r.y + r.z * r.w) * s.x * 2.0f,
                m20 = (r.x * r.z - r.y * r.w) * s.x * 2.0f,
                m01 = (r.x * r.y - r.z * r.w) * s.y * 2.0f,
                m11 = (1.0f - 2.0f * (r.x * r.x + r.z * r.z)) * s.y,
                m21 = (r.y * r.z + r.x * r.w) * s.y * 2.0f,
                m02 = (r.x * r.z + r.y * r.w) * s.z * 2.0f,
                m12 = (r.y * r.z - r.x * r.w) * s.z * 2.0f,
                m22 = (1.0f - 2.0f * (r.x * r.x + r.y * r.y)) * s.z,
                m03 = t.x,
                m13 = t.y,
                m23 = t.z,
            };
        }

        public static unsafe void TRSWithInverse(ref Vector3 t, ref Quaternion r, ref Vector3 s, PackedMatrix* outMat, PackedMatrix* outMatInverse)
        {
            outMat->m00 = (1.0f - 2.0f * (r.y * r.y + r.z * r.z)) * s.x;
            outMat->m10 = (r.x * r.y + r.z * r.w) * s.x * 2.0f;
            outMat->m20 = (r.x * r.z - r.y * r.w) * s.x * 2.0f;
            outMat->m01 = (r.x * r.y - r.z * r.w) * s.y * 2.0f;
            outMat->m11 = (1.0f - 2.0f * (r.x * r.x + r.z * r.z)) * s.y;
            outMat->m21 = (r.y * r.z + r.x * r.w) * s.y * 2.0f;
            outMat->m02 = (r.x * r.z + r.y * r.w) * s.z * 2.0f;
            outMat->m12 = (r.y * r.z - r.x * r.w) * s.z * 2.0f;
            outMat->m22 = (1.0f - 2.0f * (r.x * r.x + r.y * r.y)) * s.z;
            outMat->m03 = t.x;
            outMat->m13 = t.y;
            outMat->m23 = t.z;
            
            *outMatInverse = outMat->GetInverse();
        }


        public static unsafe void TRSWithInverse(ref Vector3 t, ref Quaternion r, ref Vector3 s, out PackedMatrix outMat, out PackedMatrix outMatInverse)
        {
            outMat = new PackedMatrix();
            outMat.m00 = (1.0f - 2.0f * (r.y * r.y + r.z * r.z)) * s.x;
            outMat.m10 = (r.x * r.y + r.z * r.w) * s.x * 2.0f;
            outMat.m20 = (r.x * r.z - r.y * r.w) * s.x * 2.0f;
            outMat.m01 = (r.x * r.y - r.z * r.w) * s.y * 2.0f;
            outMat.m11 = (1.0f - 2.0f * (r.x * r.x + r.z * r.z)) * s.y;
            outMat.m21 = (r.y * r.z + r.x * r.w) * s.y * 2.0f;
            outMat.m02 = (r.x * r.z + r.y * r.w) * s.z * 2.0f;
            outMat.m12 = (r.y * r.z - r.x * r.w) * s.z * 2.0f;
            outMat.m22 = (1.0f - 2.0f * (r.x * r.x + r.y * r.y)) * s.z;
            outMat.m03 = t.x;
            outMat.m13 = t.y;
            outMat.m23 = t.z;

            outMatInverse = outMat.GetInverse();
        }
    }
}

//XDay