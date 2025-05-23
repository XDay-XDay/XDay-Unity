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

namespace XDay.WorldAPI
{
    public class WorldPluginMetadataAttribute : System.Attribute
    {
        public string DisplayName => m_DisplayName;
        public string EditorFileName => m_EditorFileName;
        public bool IsSingleton => m_Singleton;
        public Type PluginCreateWindowType => m_PluginCreateWindowType;

        public WorldPluginMetadataAttribute(string displayName, string editorFileName, Type pluginCreateDialogType, bool singleton = true)
        {
            m_DisplayName = displayName;
            m_EditorFileName = editorFileName;
            m_PluginCreateWindowType = pluginCreateDialogType;
            m_Singleton = singleton;
        }

        private bool m_Singleton;
        private string m_DisplayName;
        private string m_EditorFileName;
        private Type m_PluginCreateWindowType;
    }
}

//XDay