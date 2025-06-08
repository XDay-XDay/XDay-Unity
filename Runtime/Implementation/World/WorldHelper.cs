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



using XDay.UtilityAPI;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace XDay.WorldAPI
{
    public static class WorldHelper
    {
        public static T CloneSerializable<T>(T serializable, World world) where T : class, ISerializable
        {
            var serializer = ISerializer.CreateBinary();
            serializer.WriteSerializable(serializable, "", new NewObjectID(world), false);
            serializer.Uninit();

            var deserializer = IDeserializer.CreateBinary(new MemoryStream(serializer.Data), world.SerializableFactory);
            var clonned = deserializer.ReadSerializable<T>("", false);
            deserializer.Uninit();

            return clonned;
        }

        public static int GetLODIndex(string path)
        {
            var offset = path.IndexOf(WorldDefine.LOD_KEYWORD, System.StringComparison.OrdinalIgnoreCase);
            if (offset == -1)
            {
                return -1;
            }
            var prefix = Helper.GetPathName(path[offset..], false);
            var rx = new Regex(@$"{WorldDefine.LOD_KEYWORD}\d+", RegexOptions.IgnoreCase);
            if (rx.Match(prefix).Length != prefix.Length)
            {
                return -1;
            }

            var pathNameWithoutExtension = Helper.GetPathName(path, false);
            offset = pathNameWithoutExtension.IndexOf(WorldDefine.LOD_KEYWORD, System.StringComparison.OrdinalIgnoreCase);
            var lodValueOffset = pathNameWithoutExtension.IndexOf(WorldDefine.LOD_KEYWORD, offset, System.StringComparison.OrdinalIgnoreCase);
            var lodValueString = pathNameWithoutExtension[(lodValueOffset + WorldDefine.LOD_KEYWORD.Length)..];
            int.TryParse(lodValueString, out var lod);
            return lod;
        }

        public static bool ImageButton(string name, string tooltip, int imageSize = 18)
        {
#if UNITY_EDITOR
            return GUILayout.Button(new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(GetIconPath(name)), tooltip), GUILayout.MaxWidth(imageSize), GUILayout.MaxHeight(imageSize));
#else
            return false;
#endif
        }

        public static string GetIconPath(string name)
        {
            return GetResourcePath(name, "Icon");
        }

        public static string GetPrefabPath(string name)
        {
            return GetResourcePath(name, "Prefab");
        }

        public static string GetShaderPath(string name)
        {
            return GetResourcePath(name, "Shader");
        }

        public static string GetBrushPath()
        {
            return GetResourcePath("", "Brush");
        }

        private static string GetResourcePath(string name, string type)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(m_ResourceDir))
            {
                string[] guids = AssetDatabase.FindAssets("XDayDummy t:Scene");
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var sceneName = Path.GetFileName(assetPath);
                    if (sceneName == "XDayDummy.unity")
                    {
                        m_ResourceDir = Helper.GetFolderPath(assetPath);
                    }
                }
            }
            var path = Path.Combine($"{m_ResourceDir}/{type}", name);
            path = path.Replace('\\', '/');
            return path;
#else
            return "";
#endif
        }


        private static string m_ResourceDir;
    }
}
