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
using System.Collections.Generic;
using UnityEngine;

namespace XDay.WorldAPI
{
    internal class WorldPluginLoader
    {
        public WorldPluginLoader(IWorldAssetLoader assetLoader, ISerializableFactory serializableFactory)
        {
            m_AssetLoader = assetLoader;
            m_SerializableFactory = serializableFactory;
        }

        public void Load(int worldID, string folder)
        {
            LoadPluginList(folder);

            m_Deserializers.Clear();

            foreach (var info in m_PluginList)
            {
                foreach (var fileName in info.FileNames)
                {
                    AddDeserializer(worldID, fileName, CreateDeserializer($"{folder}/{fileName}.bytes"));
                }
            }

            AddDeserializer(worldID, WorldDefine.GAME_FILE_NAME, CreateDeserializer($"{folder}/{WorldDefine.GAME_FILE_NAME}.bytes"));
        }

        public IDeserializer GetPluginDeserializer(int worldID, string fileName)
        {
            m_Deserializers.TryGetValue(worldID, out var deserializers);
            if (deserializers != null)
            {
                deserializers.TryGetValue(fileName, out var deserializer);
                return deserializer;
            }
            return null;
        }

        public IDeserializer GetMainDeserializer(int worldID)
        {
            return GetPluginDeserializer(worldID, WorldDefine.GAME_FILE_NAME);
        }

        public void RegisterPlugin(string typeName, List<string> files)
        {
            Debug.Assert(!m_FileNameMapping.ContainsKey(typeName));

            m_FileNameMapping[typeName] = files;
        }

        public List<WorldPlugin> LoadPlugins(World world)
        {
            var plugins = new List<WorldPlugin>();
            foreach (var info in m_PluginList)
            {
                var found = true;
                foreach (var fileName in info.FileNames)
                {
                    if (GetPluginDeserializer(world.ID, fileName) == null)
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    var plugin = m_SerializableFactory.CreateObject(QueryTypeName(info.FileNames)) as WorldPlugin;
                    plugins.Add(plugin);
                    plugin.LoadGameData(world, info.PluginName);
                }
            }

            m_Deserializers.Remove(world.ID);

            return plugins;
        }

        private IDeserializer CreateDeserializer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (!m_AssetLoader.Exists(filePath))
            {
                Debug.LogError($"file {filePath} not found!");
                return null;
            }

            return IDeserializer.CreateBinary(m_AssetLoader.LoadTextStream(filePath), m_SerializableFactory);
        }

        private void LoadPluginList(string folder)
        {
            var deserializer = IDeserializer.CreateBinary(m_AssetLoader.LoadTextStream($"{folder}/PluginList.bytes"), m_SerializableFactory);

            var n = deserializer.ReadInt32("Plugin Count");
            m_PluginList = new(n);
            for (var i = 0; i < n; i++)
            {
                var pluginName = deserializer.ReadString("");
                var fileNames = deserializer.ReadStringList("");
                m_PluginList.Add(new(fileNames, pluginName));
            }
            deserializer.Uninit();
        }

        private string QueryTypeName(List<string> fileNames)
        {
            foreach (var kv in m_FileNameMapping)
            {
                foreach (var fileName in kv.Value)
                {
                    foreach (var name in fileNames)
                    {
                        if (name.StartsWith(fileName))
                        {
                            return kv.Key;
                        }
                    }
                }
            }
            return null;
        }

        private void AddDeserializer(int worldID, string fileName, IDeserializer deserializer)
        {
            if (deserializer == null)
            {
                return;
            }

            m_Deserializers.TryGetValue(worldID, out var list);
            if (list == null)
            {
                list = new();
                m_Deserializers.Add(worldID, list);
            }
            list.Add(fileName, deserializer);
        }

        private class PluginInfo
        {
            public PluginInfo(List<string> fileNames, string pluginName)
            {
                FileNames = fileNames;
                PluginName = pluginName;
            }

            public List<string> FileNames { get; }
            public string PluginName { get; }
        }

        private Dictionary<string, List<string>> m_FileNameMapping = new();
        private List<PluginInfo> m_PluginList;
        private IWorldAssetLoader m_AssetLoader;
        private Dictionary<int, Dictionary<string, IDeserializer>> m_Deserializers = new();
        private ISerializableFactory m_SerializableFactory;
    }
}


//XDay