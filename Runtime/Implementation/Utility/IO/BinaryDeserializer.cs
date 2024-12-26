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
    internal class BinaryDeserializer : IDeserializer
    {
        public long Position => m_Reader.Stream.Position;

        public BinaryDeserializer(Stream stream, ISerializableFactory creator)
        {
            if (stream == null)
            {
                Debug.LogError("invalid stream");
            }

            m_SerializableCreator = creator;

            m_Reader = new BinaryReader(stream);

            //load string table            
            long stringTableOffset = m_Reader.ReadInt64();
            var position = Position;
            Seek(stringTableOffset, SeekOrigin.Begin);
            m_IsReadingStringTable = true;
            m_StringTable = ReadSerializable<StringTable>("String Table", false);
            m_IsReadingStringTable = false;
            Seek(position, SeekOrigin.Begin);
        }

        public void Init(Stream stream)
        {
            m_Reader.Init(stream);
        }

        public void Uninit()
        {
            m_Reader.Uninit();
        }

        public int ReadInt32(string label = null, int missingValue = default)
        {
            return m_Reader.ReadInt32();
        }

        public float ReadSingle(string label = null, float missingValue = default)
        {
            return m_Reader.ReadFloat();
        }

        public int[] ReadInt32Array(string label = null)
        {
            int n = m_Reader.ReadInt32();
            int[] ret = new int[n];
            for (int i = 0; i < n; ++i)
            {
                ret[i] = m_Reader.ReadInt32();
            }
            return ret;
        }

        public float[] ReadSingleArray(string label = null)
        {
            int n = m_Reader.ReadInt32();
            float[] ret = new float[n];
            for (int i = 0; i < n; ++i)
            {
                ret[i] = m_Reader.ReadFloat();
            }
            return ret;
        }

        public List<int> ReadInt32List(string label = null)
        {
            int n = m_Reader.ReadInt32();
            List<int> ret = new List<int>(n);
            for (int i = 0; i < n; ++i)
            {
                ret.Add(m_Reader.ReadInt32());
            }
            return ret;
        }

        public List<float> ReadSingleList(string label = null)
        {
            int n = m_Reader.ReadInt32();
            List<float> ret = new List<float>(n);
            for (int i = 0; i < n; ++i)
            {
                ret.Add(m_Reader.ReadFloat());
            }
            return ret;
        }

        public string ReadString(string label = null, string missingValue = default)
        {
            if (m_IsReadingStringTable)
            {
                return ReadPureString();
            }
            
            int id = m_Reader.ReadInt32();
            var str = m_StringTable.GetString(id);
            Debug.Assert(!string.IsNullOrEmpty(str));
            return str;   
        }

        public Vector3 ReadVector3(string label = null, Vector3 missingValue = default)
        {
            return new Vector3(m_Reader.ReadFloat(), m_Reader.ReadFloat(), m_Reader.ReadFloat());
        }

        public List<T> ReadList<T>(string label, Func<int, T> readListElement)
        {
            int count = m_Reader.ReadInt32();
            var list = new List<T>(count);
            for (int i = 0; i < count; ++i)
            {
                list.Add(readListElement(i));
            }
            return list;
        }

        public T[] ReadArray<T>(string label, Func<int, T> readArrayElement)
        {
            int length = m_Reader.ReadInt32();
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = readArrayElement(i);
            }
            return array;
        }

        public uint ReadUInt32(string label, uint missingValue = default)
        {
            return m_Reader.ReadUInt32();
        }

        public List<string> ReadStringList(string label)
        {
            int n = m_Reader.ReadInt32();
            List<string> strings = new List<string>(n);
            for (int i = 0; i < n; ++i)
            {
                strings.Add(ReadString());
            }
            return strings;
        }

        public bool ReadBoolean(string label = null, bool missingValue = default)
        {
            return m_Reader.ReadBoolean();
        }

        public Vector2 ReadVector2(string label, Vector2 missingValue = default)
        {
            return new Vector2(m_Reader.ReadFloat(), m_Reader.ReadFloat());
        }

        public Vector4 ReadVector4(string label, Vector4 missingValue = default)
        {
            return new Vector4(
                m_Reader.ReadFloat(), 
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat());
        }

        public Quaternion ReadQuaternion(string label, Quaternion missingValue = default)
        {
            return new Quaternion(
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat());
        }

        public Color ReadColor(string label, Color missingValue = default)
        {
            return new Color(
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat(),
                m_Reader.ReadFloat());
        }

        public Color32 ReadColor32(string label, Color32 missingValue = default)
        {
            return new Color32(
                m_Reader.ReadByte(),
                m_Reader.ReadByte(),
                m_Reader.ReadByte(),
                m_Reader.ReadByte());
        }

        public T ReadEnum<T>(string label, T missingValue = default) where T : Enum
        {
            long val = m_Reader.ReadInt64();
            return (T)Enum.ToObject(typeof(T), val);
        }

        public long ReadInt64(string label, long missingValue = default)
        {
            return m_Reader.ReadInt64();
        }

        public ulong ReadUInt64(string label, ulong missingValue = default)
        {
            return m_Reader.ReadUInt64();
        }

        public void ReadStructure(string label, Action readFunc)
        {
            readFunc();
        }

        public T ReadSerializable<T>(string label, bool gameData) where T : class, ISerializable
        {
            var valid = ReadBoolean();
            if (valid)
            {
                string typeAlias;
                if (m_IsReadingStringTable)
                {
                    typeAlias = ReadPureString();
                }
                else
                {
                    typeAlias = ReadString();
                }
                var serializable = m_SerializableCreator.CreateObject(typeAlias);
                if (serializable != null)
                {
                    if (gameData)
                    {
                        serializable.GameDeserialize(this, label);
                    }
                    else
                    {
                        serializable.EditorDeserialize(this, label);
                    }
                }
                return serializable as T;
            }
            return null;
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            m_Reader.Stream.Seek(offset, origin);
        }

        private string ReadPureString()
        {
            var length = ReadInt32();
            if (length > 0)
            {
                var bytes = m_Reader.Read(length);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            return "";
        }

        public string[] ReadStringArray(string label)
        {
            int n = m_Reader.ReadInt32();
            string[] ret = new string[n];
            for (int i = 0; i < n; ++i)
            {
                ret[i] = ReadString();
            }
            return ret;
        }

        private BinaryReader m_Reader;
        private StringTable m_StringTable;
        private ISerializableFactory m_SerializableCreator;
        private bool m_IsReadingStringTable;
    }
}

//XDay