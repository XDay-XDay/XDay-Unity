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
    public interface IAspect
    {
        static IAspect CreateArray<T>(T[] val, bool makeCopy)
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

        static IAspect FromBoolean(bool val)
        {
            return Aspect.FromBoolean(val);
        }
        
        static IAspect FromInt32(int val)
        {
            return Aspect.FromInt32(val);
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

    public interface INamedAspect
    {
        static INamedAspect Create(IAspect aspect, string name)
        {
            return new NamedAspect(aspect, name);
        }

        string Name { get; set; }
        IAspect Value { get; }

        INamedAspect Clone();
    }

    public interface IAspectContainer
    {
        bool IsVisible { get; set; }
        List<INamedAspect> Aspects { get; }

        INamedAspect QueryAspect(string name);
        void AddAspect(INamedAspect aspect);
        void RemoveAspect(string name);
    }
}
