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

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XDay.SerializationAPI
{
    public interface IObjectIDConverter
    {
        int Convert(int id);
    }

    public class PassID : IObjectIDConverter
    {
        public int Convert(int id)
        {
            return id;
        }
    }

    public interface ISerializer
    {
        static ISerializer CreateBinary(Stream outputStream = null)
        {
            return new BinarySerializer(outputStream);
        }

        static ISerializer CreateFile(ISerializer serializer, string filePath)
        {
            return new FileSerializer(serializer, filePath);
        }

        byte[] Data { get; }
        string Text { get; }

        void Uninit();

        void WriteInt32(int value, string mark);
        void WriteUInt32(uint value, string mark);
        void WriteInt64(long value, string mark);
        void WriteUInt64(ulong value, string mark);
        void WriteInt32Array(int[] value, string mark);
        void WriteSingleArray(float[] value, string mark);
        void WriteStringArray(string[] value, string mark);
        void WriteInt32List(List<int> value, string mark);
        void WriteStringList(List<string> value, string mark);
        void WriteSingleList(List<float> value, string mark);
        void WriteSingle(float value, string mark);
        void WriteBoolean(bool value, string mark);
        void WriteString(string value, string mark);
        
        void WriteVector2(Vector2 value, string mark);
        void WriteVector3(Vector3 value, string mark);
        void WriteVector4(Vector4 value, string mark);
        void WriteQuaternion(Quaternion value, string mark);
        void WriteColor(Color color, string mark);
        void WriteColor32(Color32 color, string mark);
        void WriteEnum<T>(T value, string mark) where T : System.Enum;
        void WriteList<T>(List<T> values, string mark, System.Action<T, int> writeListElement);
        void WriteArray<T>(T[] values, string mark, System.Action<T, int> writeArrayElement);
        void WriteStructure(string mark, System.Action writeFunc);
        void WriteObjectID(int id, string label, IObjectIDConverter converter);
        void WriteSerializable(ISerializable serializable, string label, IObjectIDConverter converter, bool gameData);
    }

    public interface IDeserializer
    {
        static IDeserializer CreateBinary(Stream stream, ISerializableFactory creator)
        {
            return new BinaryDeserializer(stream, creator);
        }

        void Uninit();
        int ReadInt32(string label, int missingValue = default);
        uint ReadUInt32(string label, uint missingValue = default);
        long ReadInt64(string label, long missingValue = default);
        ulong ReadUInt64(string label, ulong missingValue = default);
        float ReadSingle(string label, float missingValue = default);
        bool ReadBoolean(string label, bool missingValue = default);
        string ReadString(string label, string missingValue = default);
        Vector2 ReadVector2(string label, Vector2 missingValue = default);
        Vector3 ReadVector3(string label, Vector3 missingValue = default);
        Vector4 ReadVector4(string label, Vector4 missingValue = default);
        Quaternion ReadQuaternion(string label, Quaternion missingValue = default);
        Color ReadColor(string label, Color missingValue = default);
        Color32 ReadColor32(string label, Color32 missingValue = default);
        T ReadEnum<T>(string label, T missingValue = default) where T : System.Enum;
        int[] ReadInt32Array(string label);
        float[] ReadSingleArray(string label);
        string[] ReadStringArray(string label);
        List<int> ReadInt32List(string label);
        List<string> ReadStringList(string label);
        List<float> ReadSingleList(string label);
        List<T> ReadList<T>(string label, System.Func<int, T> readListElement);
        T[] ReadArray<T>(string label, System.Func<int, T> readArrayElement);
        void ReadStructure(string label, System.Action readFunc);
        T ReadSerializable<T>(string label, bool gameData) where T : class, ISerializable;
    }

    public interface ISerializable
    {
        string TypeName { get; }

        void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            throw new System.NotImplementedException();
        }

        void EditorDeserialize(IDeserializer deserializer, string label)
        {
            throw new System.NotImplementedException();
        }

        void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            throw new System.NotImplementedException();
        }
        void GameDeserialize(IDeserializer deserializer, string label)
        {
            throw new System.NotImplementedException();
        }
    }

    public interface ISerializableFactory
    {
        static ISerializableFactory Create()
        {
            return new SerializableFactory();
        }

        ISerializable CreateObject(string typeName);
    }
}