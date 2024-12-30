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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XDay.SerializationAPI
{
    internal class BinarySerializer : ISerializer
    {
        public byte[] Data { get { return m_Buffer; } }
        public string Text { get { return null; } }
        public long Position { get => m_Writer.Stream.Position; set => m_Writer.Stream.Position = value; }

        public BinarySerializer(Stream outputStream)
        {
            if (outputStream == null)
            {
                outputStream = new MemoryStream();
                m_UseInternalStream = true;
            }
            else
            {
                m_UseInternalStream = false;
            }

            m_Writer = new BinaryWriter(outputStream);

            m_StringTablePlaceholderOffset = Position;

            long offsetPlaceholder = 0;
            WriteInt64(offsetPlaceholder);
        }

        public void Init(Stream stream)
        {
            m_Writer.Init(stream);
        }

        public void Uninit()
        {
            SeekWriteRestore(m_StringTablePlaceholderOffset, Position);

            m_WritingStringTable = true;
            WriteSerializable(m_StringTable, "String Table", new PassID(), false);
            m_WritingStringTable = false;

            if (m_UseInternalStream)
            {
                var stream = m_Writer.Stream as MemoryStream;

                m_Buffer = new byte[stream.Length];
                Buffer.BlockCopy(stream.GetBuffer(), 0, m_Buffer, 0, (int)stream.Length);
            }
            m_Writer.Uninit();
        }

        public void WriteInt32(int value, string label = null)
        {
            m_Writer.Write(value);
        }

        public void WriteUInt32(uint value, string label = null)
        {
            m_Writer.Write(value);
        }

        public void WriteEnum<T>(T value, string label = null) where T : System.Enum
        {
            long val = System.Convert.ToInt64(value);
            WriteInt64(val, label);
        }

        public void WriteSingle(float value, string label = null)
        {
            m_Writer.Write(value);
        }

        public void WriteBoolean(bool value, string label = null)
        {
            m_Writer.Write(value);
        }

        public void WriteInt32Array(int[] value, string label = null)
        {
            int n = value != null ? value.Length : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                m_Writer.Write(value[i]);
            }
        }

        public void WriteByteArray(byte[] value, string label = null)
        {
            int n = value != null ? value.Length : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                m_Writer.Write(value[i]);
            }
        }

        public void WriteSingleArray(float[] value, string label = null)
        {
            int n = value != null ? value.Length : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                m_Writer.Write(value[i]);
            }
        }

        public void WriteStringArray(string[] value, string label)
        {
            int n = value != null ? value.Length : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                WriteString(value[i], "");
            }
        }

        public void WriteStringList(List<string> value, string label = null)
        {
            int n = value != null ? value.Count : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                WriteString(value[i], "");
            }
        }

        public void WriteInt32List(List<int> value, string label = null)
        {
            int n = value != null ? value.Count : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                m_Writer.Write(value[i]);
            }
        }

        public void WriteSingleList(List<float> value, string label = null)
        {
            int n = value != null ? value.Count : 0;
            m_Writer.Write(n);
            for (int i = 0; i < n; ++i)
            {
                m_Writer.Write(value[i]);
            }
        }

        public void WriteString(string value, string label = null)
        {
            if (m_WritingStringTable)
            {
                WritePureString(value);
            }
            else
            {
                int stringID = m_StringTable.GetID(value);
                WriteInt32(stringID);
            }
        }

        public void WriteList<T>(List<T> list, string label, System.Action<T, int> writeListElement)
        {
            m_Writer.Write(list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                writeListElement(list[i], i);
            }
        }

        public void WriteArray<T>(T[] array, string label, System.Action<T, int> writeArrayElement)
        {
            m_Writer.Write(array.Length);
            for (int i = 0; i < array.Length; ++i)
            {
                writeArrayElement(array[i], i);
            }
        }

        public void WriteDictionary<K, V>(Dictionary<K, V> dictionary, string label,
            System.Action<K, int> writeElementKey,
            System.Action<V, int> writeElementValue)
        {
            m_Writer.Write(dictionary.Count);
            int idx = 0;
            foreach (var p in dictionary)
            {
                writeElementKey(p.Key, idx);
                writeElementValue(p.Value, idx);
                ++idx;
            }
        }

        public void WriteVector2(Vector2 value, string label = null)
        {
            m_Writer.Write(value.x);
            m_Writer.Write(value.y);
        }

        public void WriteVector3(Vector3 value, string label = null)
        {
            m_Writer.Write(value.x);
            m_Writer.Write(value.y);
            m_Writer.Write(value.z);
        }

        public void WriteVector4(Vector4 value, string label = null)
        {
            m_Writer.Write(value.x);
            m_Writer.Write(value.y);
            m_Writer.Write(value.z);
            m_Writer.Write(value.w);
        }

        public void WriteQuaternion(Quaternion value, string label = null)
        {
            m_Writer.Write(value.x);
            m_Writer.Write(value.y);
            m_Writer.Write(value.z);
            m_Writer.Write(value.w);
        }

        public void WriteColor32(Color32 color, string label = null)
        {
            m_Writer.Write(color.r);
            m_Writer.Write(color.g);
            m_Writer.Write(color.b);
            m_Writer.Write(color.a);
        }

        public void WriteColor(Color color, string label = null)
        {
            m_Writer.Write(color.r);
            m_Writer.Write(color.g);
            m_Writer.Write(color.b);
            m_Writer.Write(color.a);
        }

        public void WriteInt64(long value, string label = null)
        {
            m_Writer.Write(value);
        }

        public void WriteUInt64(ulong value, string label = null)
        {
            m_Writer.Write(value);
        }

        public void WriteObjectID(int id, string label, IObjectIDConverter converter)
        {
            id = converter != null ? converter.Convert(id) : id;
            WriteInt32(id, label);
        }

        public void WriteStructure(string label, Action writeFunc)
        {
            writeFunc();
        }

        public void WriteSerializable(ISerializable serializable, string label, IObjectIDConverter converter, bool gameData)
        {
            Debug.Assert(converter != null);

            var valid = serializable != null;
            WriteBoolean(valid);

            if (serializable != null)
            {
                if (gameData)
                {
                    WriteString(serializable.GameTypeName);
                    serializable.GameSerialize(this, label, converter);
                }
                else
                {
                    WriteString(serializable.TypeName);
                    serializable.EditorSerialize(this, label, converter);
                }
            }
        }

        public byte[] CreateBuffer(MemoryStream stream)
        {
            byte[] ret = new byte[stream.Length];
            System.Array.Copy(stream.GetBuffer(), ret, stream.Length);
            return ret;
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            m_Writer.Stream.Seek(offset, origin);
        }

        public void WriteRect(Rect rect, string label)
        {
            m_Writer.Write(rect.xMin);
            m_Writer.Write(rect.yMin);
            m_Writer.Write(rect.xMax);
            m_Writer.Write(rect.yMax);
        }

        private void WritePureString(string val)
        {
            byte[] bytes;
            if (string.IsNullOrEmpty(val))
            {
                bytes = new byte[0];
            }
            else
            {
                bytes = System.Text.Encoding.UTF8.GetBytes(val);
            }

            m_Writer.Write(bytes.Length);
            if (bytes.Length > 0)
            {
                m_Writer.Write(bytes);
            }
        }

        public void WriteBounds(Bounds bounds, string label)
        {
            m_Writer.Write(bounds.min.x);
            m_Writer.Write(bounds.min.y);
            m_Writer.Write(bounds.min.z);
            m_Writer.Write(bounds.max.x);
            m_Writer.Write(bounds.max.y);
            m_Writer.Write(bounds.max.z);
        }

        private void SeekWriteRestore(long seekToOffset, long writeValue)
        {
            var curPos = Position;
            Seek(seekToOffset, SeekOrigin.Begin);
            WriteInt64(writeValue);
            Position = curPos;
        }

        public void WriteVector2Array(Vector2[] value, string label)
        {
            WriteInt32(value.Length);
            foreach (var v in value)
            {
                WriteVector2(v);
            }
        }

        public void WriteVector3Array(Vector3[] value, string label)
        {
            WriteInt32(value.Length);
            foreach (var v in value)
            {
                WriteVector3(v);
            }
        }

        public void WriteVector4Array(Vector4[] value, string label)
        {
            WriteInt32(value.Length);
            foreach (var v in value)
            {
                WriteVector4(v);
            }
        }

        private BinaryWriter m_Writer;
        private StringTable m_StringTable = new StringTable();
        private long m_StringTablePlaceholderOffset = 0;
        private byte[] m_Buffer;
        private bool m_UseInternalStream;
        private bool m_WritingStringTable = false;
    }
}

//XDay