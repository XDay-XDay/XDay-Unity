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

using XDay.SerializationAPI;
using XDay.UtilityAPI;
using System.Collections.Generic;
using System.IO;

namespace XDay.WorldAPI.Editor
{
    internal class EditorWorldPluginLoader
    {
        public EditorWorldPluginLoader(ISerializableFactory serializableCreator)
        {
            m_SerializableFactory = serializableCreator;
        }

        public void RegisterPluginLoadFile(string type, string fileName)
        {
            m_FileNameMapping.Add(type, fileName);
        }

        public List<WorldPlugin> LoadPlugins(string path)
        {
            var plugins = new List<WorldPlugin>();
            var filePaths = EditorHelper.EnumerateFiles(path, false, false);
            foreach (var pair in m_FileNameMapping)
            {
                foreach (var matchedFileName in MatchFileNames(pair.Value, filePaths))
                {
                    IDeserializer deserializer = IDeserializer.CreateBinary(new FileStream(matchedFileName, FileMode.Open), m_SerializableFactory);

                    var plugin = deserializer.ReadSerializable<WorldPlugin>($"{pair.Key}", false);
                    plugins.Add(plugin);

                    deserializer.Uninit();
                }
            }

            return plugins;
        }

        private List<string> MatchFileNames(string name, List<string> paths)
        {
            var ret = new List<string>();
            foreach (var path in paths)
            {
                if (path.IndexOf(name) >= 0)
                {
                    ret.Add(path);
                }
            }
            return ret;
        }

        private Dictionary<string, string> m_FileNameMapping = new();
        private ISerializableFactory m_SerializableFactory;
    }
}

//XDay