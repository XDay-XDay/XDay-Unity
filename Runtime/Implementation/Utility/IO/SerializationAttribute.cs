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


namespace XDay.SerializationAPI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class XDaySerializableClassAttribute : Attribute
    {
        public string Label => m_Label;
        public bool HasPostLoad => m_HasPostLoad;

        public XDaySerializableClassAttribute(string label, bool hasPostLoad = false)
        {
            m_Label = label;
            m_HasPostLoad = hasPostLoad;
        }

        private string m_Label;
        private bool m_HasPostLoad;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XDaySerializableFieldAttribute : Attribute
    {
        public string Label => m_Label;
        public int Version => m_Version;

        public XDaySerializableFieldAttribute(int version, string label)
        {
            m_Version = version;
            m_Label = label;
        }

        private string m_Label;
        private int m_Version;
    }
}

//XDay