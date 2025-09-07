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
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.CodeStripping
{
    public class CodeStripper
    {
        public Dictionary<string, AssemblyGroup> Groups => m_Groups;

        public bool Generate(CodeStripperSetting setting, List<string> preservedBaseTypes = null)
        {
            if (setting == null)
            {
                Debug.LogError("CodeStripperSetting file not found!");
                return false;
            }

            List<Type> types = new();
            if (preservedBaseTypes != null)
            {
                foreach (var name in preservedBaseTypes)
                {
                    var type = Type.GetType(name);
                    if (type != null)
                    {
                        types.Add(type);
                    }
                    else
                    {
                        Debug.LogError($"Type {name} not found!");
                    }
                }
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            CreateGroups(assemblies, setting);

            FilterEditorAssemblies();

            PreserveTypes(types);

            GenerateLinkXMLFile();

            return true;
        }

        private void PreserveTypes(List<Type> preservedBaseTypes)
        {
            var assemblies = GetPreservedAssemblies();
            foreach (var assembly in assemblies)
            {
                var allTypes = assembly.Assembly.GetTypes();
                foreach (var type in allTypes)
                {
                    if (type.IsSubclassOf(typeof(MonoBehaviour)) ||
                        type.IsSubclassOf(typeof(ScriptableObject)) ||
                        IsAssignableType(preservedBaseTypes, type))
                    {
                        assembly.PreservedTypes.Add(type);
                    }
                }
            }
        }

        private bool IsAssignableType(List<Type> preservedBaseTypes, Type type)
        {
            foreach (var pt in preservedBaseTypes)
            {
                if (pt.IsAssignableFrom(type))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 过滤编辑器下的assembly
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void FilterEditorAssemblies()
        {
            //check assemblies
            var allAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();
            foreach (var assembly in allAssemblies)
            {
                var editorOnly = assembly.flags.HasFlag(UnityEditor.Compilation.AssemblyFlags.EditorAssembly);
                if (editorOnly)
                {
                    m_EditorAssemblyNames.Add(assembly.name);
                }
            }

            var allImporters = PluginImporter.GetAllImporters();

            //check precompiled dlls
            var assetsDllFiles = Helper.SearchFiles("Assets", "dll");
            var packagesDllFiles = Helper.SearchFiles("Packages", "dll");
            var libraryDllFiles = Helper.SearchFiles("Library/PackageCache", "dll");
            assetsDllFiles.AddRange(packagesDllFiles);
            assetsDllFiles.AddRange(libraryDllFiles);
            foreach (var file in assetsDllFiles)
            {
                var importer = AssetImporter.GetAtPath(file) as PluginImporter;
                if (importer == null)
                {
                    importer = FindImporter(allImporters, file);
                }
                if (importer == null)
                {
                    continue;
                }

                var editorOnly = EditorHelper.IsPluginMarkedAsEditorOnly(importer);
                if (editorOnly)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    m_EditorAssemblyNames.Add(fileName);
                }
            }
            
            foreach (var group in m_Groups.Values)
            {
                foreach (var assembly in group.Assemblies)
                {
                    if (m_EditorAssemblyNames.Contains(assembly.Name))
                    {
                        assembly.PreserveOption = PreserveOption.DontPreserve;
                        assembly.EditorOnly = true;
                    }
                    else
                    {
                        assembly.EditorOnly = false;
                    }
                }
            }
        }

        private PluginImporter FindImporter(PluginImporter[] allImporters, string file)
        {
            var fileName = Path.GetFileName(file);
            foreach (var importer in allImporters)
            {
                if (Path.GetFileName(importer.assetPath) == fileName)
                {
                    return importer;
                }
            }
            return null;
        }

        private void CreateGroups(Assembly[] assemblies, CodeStripperSetting setting)
        {
            foreach (var assembly in assemblies)
            {
                PreserveOption defaultOption = PreserveOption.PreserveType;
                var name = assembly.GetName().Name;
                AssemblyGroup group = null;
                if (name.StartsWith("System") ||
                    name.StartsWith("Microsoft") ||
                    name.StartsWith("Mono") ||
                    name.StartsWith("netstandard") ||
                    name.StartsWith("mscorlib"))
                {
                    group = GetOrCreateGroup("System");
                    defaultOption = PreserveOption.DontPreserve;
                }
                else if (name.StartsWith("Unity"))
                {
                    group = GetOrCreateGroup("Unity");
                    defaultOption = PreserveOption.DontPreserve;
                }
                else if (name.StartsWith("Game") ||
                    name.StartsWith("XDay"))
                {
                    group = GetOrCreateGroup("Game");
                }
                else
                {
                    group = GetOrCreateGroup("Other");
                }
                group.Show = setting != null && setting.GetGroupState(group.Name);
                var state = setting != null ? setting.GetPreserveState(assembly.GetName().Name, defaultOption) : defaultOption;
                group.Assemblies.Add(new AssemblyInfo()
                {
                    Assembly = assembly,
                    PreserveOption = state,
                });
            }

            foreach (var group in m_Groups.Values)
            {
                group.Sort();
            }
        }

        private AssemblyGroup GetOrCreateGroup(string name)
        {
            m_Groups.TryGetValue(name, out var group);
            if (group == null)
            {
                group = new AssemblyGroup()
                {
                    Name = name,
                };
                m_Groups[name] = group;
            }
            return group;
        }

        private void GenerateLinkXMLFile()
        {
            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<linker>
$CONTENT$
</linker>
";
            StringBuilder builder = new();
            List<AssemblyInfo> keptAssemblies = GetPreservedAssemblies();
            foreach (var assembly in keptAssemblies)
            {
                if (assembly.PreserveOption == PreserveOption.PreserveType)
                {
                    if (assembly.PreservedTypes.Count > 0)
                    {
                        builder.AppendLine($"\t<assembly fullname=\"{assembly.Name}\">");
                        foreach (var preservedType in assembly.PreservedTypes)
                        {
                            builder.AppendLine($"\t\t<type fullname=\"{preservedType.FullName}\" preserve=\"all\"/>");
                        }
                        builder.AppendLine("\t</assembly>");
                    }
                }
                else if (assembly.PreserveOption == PreserveOption.PreserveAssembly)
                {
                    builder.AppendLine($"\t<assembly fullname=\"{assembly.Name}\" preserve=\"all\"/>");
                }
            }
            content = content.Replace("$CONTENT$", builder.ToString());
            File.WriteAllText("Assets/link.xml", content);
            AssetDatabase.Refresh();
        }

        public List<AssemblyInfo> GetPreservedAssemblies()
        {
            List<AssemblyInfo> ret = new();
            foreach (var group in m_Groups.Values)
            {
                foreach (var assembly in group.Assemblies)
                {
                    if (assembly.PreserveOption != PreserveOption.DontPreserve)
                    {
                        ret.Add(assembly);
                    }
                }
            }
            return ret;
        }

        public List<AssemblyInfo> GetPreservedAssemblies(PreserveOption option)
        {
            List<AssemblyInfo> ret = new();
            foreach (var group in m_Groups.Values)
            {
                foreach (var assembly in group.Assemblies)
                {
                    if (assembly.PreserveOption == option)
                    {
                        ret.Add(assembly);
                    }
                }
            }
            return ret;
        }

        private readonly Dictionary<string, AssemblyGroup> m_Groups = new();
        private readonly HashSet<string> m_EditorAssemblyNames = new();
    }
}