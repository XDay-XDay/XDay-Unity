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
using UnityEngine;

namespace XDay.UtilityAPI
{
    /// <summary>
    /// any value container
    /// </summary>
    public interface IAspect
    {
        static IAspect FromArray<T>(T[] val, bool makeCopy)
        {
            return Aspect.FromArray(val, makeCopy);
        }

        static IAspect FromEnum<T>(T val) where T : Enum
        {
            return Aspect.FromEnum(val);
        }

        static IAspect FromQuaternion(Quaternion val)
        {
            return Aspect.FromQuaternion(val);
        }

        static IAspect FromColor(Color val)
        {
            return Aspect.FromColor(val);
        }

        static IAspect FromString(string val)
        {
            return Aspect.FromString(val);
        }

        static IAspect FromUInt32(uint val)
        {
            return Aspect.FromUInt32(val);
        }

        static IAspect FromUInt64(ulong val)
        {
            return Aspect.FromUInt64(val);
        }

        static IAspect FromBoolean(bool val)
        {
            return Aspect.FromBoolean(val);
        }
        
        static IAspect FromInt32(int val)
        {
            return Aspect.FromInt32(val);
        }

        static IAspect FromInt64(long val)
        {
            return Aspect.FromInt64(val);
        }

        static IAspect FromSingle(float val)
        {
            return Aspect.FromSingle(val);
        }

        static IAspect FromObject(object val)
        {
            return Aspect.FromObject(val);
        }

        static IAspect FromVector2(Vector2 val)
        {
            return Aspect.FromVector2(val);
        }

        static IAspect FromVector3(Vector3 val)
        {
            return Aspect.FromVector3(val);
        }

        static IAspect FromVector4(Vector4 val)
        {
            return Aspect.FromVector4(val);
        }

        int Size { get; }

        void SetArray<T>(T[] value, bool makeCopy);
        T[] GetArray<T>();
        void SetEnum<T>(T value) where T : System.Enum;
        T GetEnum<T>() where T : System.Enum;
        void SetVector2(Vector2 value);
        Vector2 GetVector2();
        void SetVector3(Vector3 value);
        Vector3 GetVector3();
        void SetVector4(Vector4 value);
        Vector4 GetVector4();
        void SetQuaternion(Quaternion value);
        Quaternion GetQuaternion();
        void SetUInt32(uint value);
        uint GetUInt32();
        void SetInt32(int value);
        int GetInt32();
        void SetUInt64(ulong value);
        ulong GetUInt64();
        void SetInt64(long value);
        long GetInt64();
        void SetBoolean(bool value);
        bool GetBoolean();
        void SetColor(Color value);
        Color GetColor();
        void SetSingle(float value);
        float GetSingle();
        void SetString(string value);
        string GetString();
        void SetObject(object value);
        object GetObject();
    }

    /// <summary>
    /// aspect with a name
    /// </summary>
    public interface INamedAspect
    {
        static INamedAspect Create(IAspect aspect, string name)
        {
            return new NamedAspect(aspect, name);
        }

        string Name { get; set; }
        IAspect Value { get; }
    }

    /// <summary>
    /// manage a bunch of aspects
    /// </summary>
    public interface IAspectContainer
    {
        /// <summary>
        /// create an instance
        /// </summary>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        static IAspectContainer Create(Dictionary<string, object> keyValues = null)
        {
            return new AspectContainer(keyValues);
        }

        /// <summary>
        /// find aspect by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        INamedAspect QueryAspect(string name);

        /// <summary>
        /// add aspect
        /// </summary>
        /// <param name="aspect"></param>
        void AddAspect(INamedAspect aspect);
        
        /// <summary>
        /// remove aspect by name
        /// </summary>
        /// <param name="name"></param>
        void RemoveAspect(string name);

        /// <summary>
        /// get string aspect
        /// </summary>
        /// <param name="name"></param>
        /// <param name="missingValue">value if not exists</param>
        /// <returns></returns>
        string GetString(string name, string missingValue);
        Quaternion GetQuaternion(string name, Quaternion missingValue);
        Color GetColor(string name, Color missingValue);
        Vector2 GetVector2(string name, Vector2 missingValue);
        Vector3 GetVector3(string name, Vector3 missingValue);
        Vector4 GetVector4(string name, Vector4 missingValue);
        float GetSingle(string name, float missingValue);
        bool GetBoolean(string name, bool missingValue);
        int GetInt32(string name, int missingValue);
        long GetInt64(string name, long missingValue);
        uint GetUInt32(string name, uint missingValue);
        ulong GetUInt64(string name, ulong missingValue);
    }
}
