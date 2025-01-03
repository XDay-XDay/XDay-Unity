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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XDay.SerializationAPI
{
    internal class FileSerializer : ISerializer
    {
        public byte[] Data => m_Serializer.Data;
        public string Text => m_Serializer.Text;
        public string FilePath => m_FilePath;

        public FileSerializer(ISerializer serializer, string filePath)
        {
            m_Serializer = serializer;
            m_FilePath = filePath;
        }

        public void Uninit()
        {
            m_Serializer.Uninit();

            if (m_Serializer.Text != null)
            {
                File.WriteAllText(m_FilePath, m_Serializer.Text);
            }
            else
            {
                File.WriteAllBytes(m_FilePath, m_Serializer.Data);
            }
        }

        public void WriteArray<T>(T[] values, string label, Action<T, int> writeArrayElement)
        {
            m_Serializer.WriteArray(values, label, writeArrayElement);
        }

        public void WriteBoolean(bool value, string label)
        {
            m_Serializer.WriteBoolean(value, label);
        }

        public void WriteByte(byte value, string label)
        {
            m_Serializer.WriteByte(value, label);
        }

        public void WriteColor(Color color, string label)
        {
            m_Serializer.WriteColor(color, label);
        }

        public void WriteColor32(Color32 color, string label)
        {
            m_Serializer.WriteColor32(color, label);
        }

        public void WriteStructure(string label, Action writeFunc)
        {
            m_Serializer.WriteStructure(label, writeFunc);
        }

        public void WriteEnum<T>(T value, string label) where T : Enum
        {
            m_Serializer.WriteEnum(value, label);
        }

        public void WriteObjectID(int id, string label, IObjectIDConverter converter)
        {
            m_Serializer.WriteObjectID(id, label, converter);
        }

        public void WriteInt32(int value, string label)
        {
            m_Serializer.WriteInt32(value, label);
        }

        public void WriteInt32Array(int[] value, string label)
        {
            m_Serializer.WriteInt32Array(value, label);
        }

        public void WriteByteArray(byte[] value, string label)
        {
            m_Serializer.WriteByteArray(value, label);
        }

        public void WriteInt32List(List<int> value, string label)
        {
            m_Serializer.WriteInt32List(value, label);
        }

        public void WriteInt64(long value, string label)
        {
            m_Serializer.WriteInt64(value, label);
        }

        public void WriteList<T>(List<T> values, string label, Action<T, int> writeListElement)
        {
            m_Serializer.WriteList(values, label, writeListElement);
        }

        public void WriteQuaternion(Quaternion value, string label)
        {
            m_Serializer.WriteQuaternion(value, label);
        }

        public void WriteSerializable(ISerializable serializable, string label, IObjectIDConverter converter, bool gameData)
        {
            m_Serializer.WriteSerializable(serializable, label, converter, gameData);
        }

        public void WriteSingle(float value, string label)
        {
            m_Serializer.WriteSingle(value, label);
        }

        public void WriteSingleArray(float[] value, string label)
        {
            m_Serializer.WriteSingleArray(value, label);
        }

        public void WriteSingleList(List<float> value, string label)
        {
            m_Serializer.WriteSingleList(value, label);
        }

        public void WriteString(string value, string label)
        {
            m_Serializer.WriteString(value, label);
        }

        public void WriteStringList(List<string> value, string label)
        {
            m_Serializer.WriteStringList(value, label);
        }

        public void WriteUInt32(uint value, string label)
        {
            m_Serializer.WriteUInt32(value, label);
        }

        public void WriteUInt64(ulong value, string label)
        {
            m_Serializer.WriteUInt64(value, label);
        }

        public void WriteVector2(Vector2 value, string label)
        {
            m_Serializer.WriteVector2(value, label);
        }

        public void WriteVector3(Vector3 value, string label)
        {
            m_Serializer.WriteVector3(value, label);
        }

        public void WriteVector4(Vector4 value, string label)
        {
            m_Serializer.WriteVector4(value, label);
        }

        public void WriteStringArray(string[] value, string label)
        {
            m_Serializer.WriteStringArray(value, label);
        }

        public void WriteBounds(Bounds bounds, string label)
        {
            m_Serializer.WriteBounds(bounds, label);
        }

        public void WriteRect(Rect rect, string label)
        {
            m_Serializer.WriteRect(rect, label);
        }

        public void WriteVector2Array(Vector2[] value, string label)
        {
            m_Serializer.WriteVector2Array(value, label);
        }

        public void WriteVector3Array(Vector3[] value, string label)
        {
            m_Serializer.WriteVector3Array(value, label);
        }

        public void WriteVector4Array(Vector4[] value, string label)
        {
            m_Serializer.WriteVector4Array(value, label);
        }

        private ISerializer m_Serializer;
        private string m_FilePath;
    }
}

//XDay
