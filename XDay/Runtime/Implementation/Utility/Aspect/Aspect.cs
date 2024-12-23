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
using XDay.SerializationAPI;

namespace XDay.UtilityAPI
{
    internal class AspectContainer : IAspectContainer
    {
        public bool IsVisible { get => m_IsVisible; set => m_IsVisible = value; }
        public List<INamedAspect> Aspects => m_Aspects;

        public void AddAspect(INamedAspect property)
        {
            if (QueryAspect(property.Name) == null)
            {
                m_Aspects.Add(property);
            }
        }

        public void RemoveAspect(string name)
        {
            for (var i = 0; i < m_Aspects.Count; ++i)
            {
                if (m_Aspects[i].Name == name)
                {
                    m_Aspects.RemoveAt(i);
                    break;
                }
            }
        }

        public INamedAspect QueryAspect(string name)
        {
            foreach (var prop in m_Aspects)
            {
                if (prop.Name == name)
                {
                    return prop;
                }
            }
            return null;
        }

        public AspectContainer Clone()
        {
            var cloned = new AspectContainer();
            foreach (var prop in m_Aspects)
            {
                cloned.AddAspect(prop.Clone());
            }
            return cloned;
        }

        public void RenameProperty(string oldName, string newName)
        {
            foreach (var prop in m_Aspects)
            {
                if (prop.Name == oldName)
                {
                    prop.Name = newName;
                    break;
                }
            }
        }

        public void Serialize(ISerializer serializer)
        {
            serializer.WriteInt32(m_Version, "AspectContainer.Version");
            serializer.WriteList(m_Aspects, "Aspects", (aspect, index) => {
                serializer.WriteStructure($"Aspect {index}", () =>
                {
                    (aspect as NamedAspect).Serialize(serializer);
                });
            });
        }

        public void Deserialize(IDeserializer deserializer)
        {
            deserializer.ReadInt32("AspectContainer.Version");
            m_Aspects = deserializer.ReadList("Aspects", (index) => {
                var aspect = new NamedAspect();
                deserializer.ReadStructure($"Aspect {index}", () =>
                {
                    aspect.Deserialize(deserializer);
                });
                return aspect as INamedAspect;
            });
        }

        private bool m_IsVisible = true;
        private List<INamedAspect> m_Aspects = new();
        private const int m_Version = 1;
    }

    internal class Aspect : IAspect
    {
        public UnionType Type => m_Value.Type;
        public Union Data { get => m_Data; set => m_Data = value; }
        public int Size => m_Value.GetSize() + (m_Data != null ? m_Data.GetSize() : 0);

        internal Aspect()
        {
        }

        internal static Aspect FromArray<T>(T[] value, bool makeCopy)
        {
            var aspect = new Aspect();
            aspect.SetArray(value, makeCopy);
            return aspect;
        }

        internal static Aspect FromEnum<T>(T value) where T : System.Enum
        {
            var aspect = new Aspect();
            aspect.SetEnum(value);
            return aspect;
        }

        internal static Aspect FromBoolean(bool value)
        {
            var aspect = new Aspect();
            aspect.SetBoolean(value);
            return aspect;
        }

        internal static Aspect FromInt32(int value)
        {
            var aspect = new Aspect();
            aspect.SetInt32(value);
            return aspect;
        }

        internal static Aspect FromUInt32(uint value)
        {
            var aspect = new Aspect();
            aspect.SetUInt32(value);
            return aspect;
        }

        internal static Aspect FromSingle(float value)
        {
            var aspect = new Aspect();
            aspect.SetSingle(value);
            return aspect;
        }

        internal static Aspect FromObject(object value)
        {
            var aspect = new Aspect();
            aspect.SetObject(value);
            return aspect;
        }

        internal static Aspect FromQuaternion(Quaternion value)
        {
            var aspect = new Aspect();
            aspect.SetQuaternion(value);
            return aspect;
        }

        internal static Aspect FromVector2(Vector2 value)
        {
            var aspect = new Aspect();
            aspect.SetVector2(value);
            return aspect;
        }

        internal static Aspect FromVector3(Vector3 value)
        {
            var aspect = new Aspect();
            aspect.SetVector3(value);
            return aspect;
        }

        internal static Aspect FromVector4(Vector4 value)
        {
            var aspect = new Aspect();
            aspect.SetVector4(value);
            return aspect;
        }

        internal static Aspect FromString(string value)
        {
            var aspect = new Aspect();
            aspect.SetString(value);
            return aspect;
        }

        internal static Aspect FromColor(Color value)
        {
            var aspect = new Aspect();
            aspect.SetColor(value);
            return aspect;
        }

        public void SetArray<T>(T[] value, bool makeCopy)
        {
            m_Value = Union.FromArray(value, makeCopy);
        }

        public T[] GetArray<T>()
        {
            return m_Value.GetArray<T>();
        }

        public void SetEnum<T>(T value) where T : System.Enum
        {
            m_Value = Union.FromEnum(value);
        }

        public T GetEnum<T>() where T : System.Enum
        {
            return m_Value.GetEnum<T>();
        }

        public void SetVector2(Vector2 value)
        {
            m_Value = Union.FromVector2(value);
        }

        public Vector2 GetVector2()
        {
            return m_Value.GetVector2();
        }

        public void SetVector3(Vector3 value)
        {
            m_Value = Union.FromVector3(value);
        }

        public Vector3 GetVector3()
        {
            return m_Value.GetVector3();
        }

        public void SetVector4(Vector4 value)
        {
            m_Value = Union.FromVector4(value);
        }

        public Vector4 GetVector4()
        {
            return m_Value.GetVector4();
        }

        public void SetQuaternion(Quaternion value)
        {
            m_Value = Union.FromQuaternion(value);
        }

        public Quaternion GetQuaternion()
        {
            return m_Value.GetQuaternion();
        }

        public void SetUInt32(uint value)
        {
            m_Value = Union.FromUInt32(value);
        }

        public uint GetUInt32()
        {
            return m_Value.GetUInt32();
        }

        public void SetInt32(int value)
        {
            m_Value = Union.FromInt32(value);
        }

        public int GetInt32()
        {
            return m_Value.GetInt32();
        }

        public void SetBoolean(bool value)
        {
            m_Value = Union.FromBoolean(value);
        }

        public bool GetBoolean()
        {
            return m_Value.GetBoolean();
        }

        public void SetColor(Color value)
        {
            m_Value = Union.FromColor(value);
        }

        public Color GetColor()
        {
            return m_Value.GetColor();
        }

        public void SetSingle(float value)
        {
            m_Value = Union.FromSingle(value);
        }

        public float GetSingle()
        {
            return m_Value.GetSingle();
        }

        public void SetString(string value)
        {
            m_Value = Union.FromString(value);
        }

        public string GetString()
        {
            return m_Value.GetString();
        }

        public void SetObject(object value)
        {
            m_Value = Union.FromObject(value);
        }

        public object GetObject()
        {
            return m_Value.GetObject();
        }

        public void Serialize(ISerializer serializer)
        {
            serializer.WriteInt32(m_Version, "Aspect.Version");

            serializer.WriteStructure("Value", () =>
            {
                m_Value.Serialize(serializer);
            });

            serializer.WriteBoolean(m_Data != null, "Is Data Valid");
            if (m_Data != null)
            {
                serializer.WriteStructure("Data", () =>
                {
                    m_Data.Serialize(serializer);
                });
            }
        }

        public void Deserialize(IDeserializer deserializer)
        {
            deserializer.ReadInt32("Aspect.Version");

            m_Value = Union.CreateEmpty();
            deserializer.ReadStructure("Value", () =>
            {
                m_Value.Deserialize(deserializer);
            });

            bool hasData = deserializer.ReadBoolean("Is Data Valid");
            if (hasData)
            {
                m_Data = Union.CreateEmpty();
                deserializer.ReadStructure("Data", () =>
                {
                    m_Data.Deserialize(deserializer);
                });
            }
        }

        public static bool operator !=(Aspect a, Aspect b)
        {
            return !(a == b);
        }

        public static bool operator ==(Aspect a, Aspect b)
        {
            if ((a is null && b is not null) ||
                (a is not null && b is null))
            {
                return false;
            }

            return ReferenceEquals(a, b) || (a.m_Data == b.m_Data && a.m_Value == b.m_Value);
        }

        public override int GetHashCode()
        {
            if (m_Data.Type != UnionType.Undefined)
            {
                return m_Value.GetHashCode() ^ m_Data.GetHashCode();
            }
            return m_Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj as Aspect) == this;
        }

        private Union m_Value;
        private Union m_Data;
        private const int m_Version = 1;
    }

    internal class NamedAspect : INamedAspect
    {
        public IAspect Value => m_Value;
        public string Name { get => m_Name; set => m_Name = value; }

        public NamedAspect()
        {
        }

        public NamedAspect(IAspect value, string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_Value = value as Aspect;
            m_Name = name;
        }

        public INamedAspect Clone()
        {
            var serializer = ISerializer.CreateBinary();
            Serialize(serializer);
            serializer.Uninit();

            var deserializer = IDeserializer.CreateBinary(new MemoryStream(serializer.Data), null);
            var cloned = new NamedAspect();
            cloned.Deserialize(deserializer);
            deserializer.Uninit();

            return cloned;
        }

        public void Serialize(ISerializer serializer)
        {
            serializer.WriteInt32(m_Version, "NamedAspect");
            serializer.WriteStructure("Value", () => { 
                m_Value.Serialize(serializer);
            });
            serializer.WriteString(m_Name, "Name");
        }

        public void Deserialize(IDeserializer deserializer)
        {
            deserializer.ReadInt32("NamedAspect");
            m_Value = new Aspect();
            deserializer.ReadStructure("Value", () => {
                m_Value.Deserialize(deserializer);
            });
            m_Name = deserializer.ReadString("Name", "");
        }

        private string m_Name;
        private Aspect m_Value;
        private const int m_Version = 1;
    }
}

//XDay