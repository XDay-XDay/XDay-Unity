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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.SerializationAPI
{
    internal class SerializableFactory : ISerializableFactory
    {
        public SerializableFactory()
        {
            var types = Helper.QueryTypes<ISerializable>(false);
            foreach (var type in types)
            {
                var serializable = Activator.CreateInstance(type) as ISerializable;
                if (string.IsNullOrEmpty(serializable.TypeName))
                {
                    Debug.LogError($"{type} type name missing");
                    continue;
                }
                else if (m_TypeMapping.ContainsKey(serializable.TypeName))
                {
                    Debug.LogError($"{type} already added");
                    continue;
                }
                m_TypeMapping.Add(serializable.TypeName, type);
            }
        }

        public ISerializable CreateObject(string typeName)
        {
            m_TypeMapping.TryGetValue(typeName, out var type);
            if (type == null)
            {
                Debug.LogError($"typeName {typeName} not found!");
            }

            var obj = Activator.CreateInstance(type) as ISerializable;
            Debug.Assert(obj != null);
            return obj;
        }

        private Dictionary<string, Type> m_TypeMapping = new();
    }
}

//XDay
