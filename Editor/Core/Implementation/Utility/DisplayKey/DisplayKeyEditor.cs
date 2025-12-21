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
using System.Text;
using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI.Editor;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.DisplayKeyAPI.Editor
{
    public class DisplayKeyEditor
    {
        public DisplayKeyManager DisplayKeyManager => m_DisplayKeyManager;
        
        public void SetCustomDrawer(Action<DisplayKey> drawer)
        {
            m_CustomDrawer = drawer;
        }

        public void SetCustomDataTranslator(Func<string, string> translator)
        {
            m_DisplayKeyManager.SetCustomDataTranslator(translator);
        }

        public void DrawEditor()
        {
            if (m_Config == null)
            {
                var config = EditorHelper.QueryAsset<DisplayKeyConfig>();
                if (config != null && config.IsValid())
                {
                    m_Config = config;
                }
                else
                {
                    EditorGUILayout.LabelField("DisplayKeyConfig is not valid!");
                }
            }

            if (m_Config != null)
            {
                Draw();
            }
            else
            {
                EditorGUILayout.LabelField("Create DisplayKeyConfig first!");
            }
        }

        public void Search(int keyID)
        {
            m_SearchText = keyID.ToString();
        }

        private void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            if (m_DisplayKeyManager != null)
            {
                if (GUILayout.Button("Save"))
                {
                    Save(true);
                }
            }

            if (GUILayout.Button("Load"))
            {
                Load();
            }
            if (m_DisplayKeyManager != null)
            {
                if (GUILayout.Button(new GUIContent("Add", "新建Group")))
                {
                    CreateGroup();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_DisplayKeyManager != null)
            {
                m_DisplayKeyManager.OutputFolder = EditorGUILayout.TextField(new GUIContent("Output Code Folder", "输出的ID文件夹"), m_DisplayKeyManager.OutputFolder);
                EditorGUILayout.BeginHorizontal();
                m_SearchText = EditorGUILayout.TextField("Search", m_SearchText, EditorStyles.toolbarSearchField);
                EditorGUILayout.IntField("Key Count", m_DisplayKeyManager.KeyCount);
                EditorGUILayout.EndHorizontal();
            }

            DrawGroups();
        }

        private void DrawGroups()
        {
            if (m_DisplayKeyManager != null)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

                foreach (var group in m_DisplayKeyManager.Groups)
                {
                    bool removed = DrawGroup(group);
                    if (removed)
                    {
                        m_DisplayKeyManager.RemoveGroup(group);
                        Save(false);
                        break;
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private bool DrawGroup(DisplayKeyGroup group)
        {
            EditorGUILayout.BeginHorizontal();
            group.Show = EditorHelper.Foldout(group.Show);
            EditorGUIUtility.labelWidth = 20;
            group.Name = EditorGUILayout.TextField("", group.Name, GUILayout.MaxWidth(100));
            EditorGUIUtility.labelWidth = 0;
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Add Key", "新建Key")))
            {
                group.AddKey(new DisplayKey());
                m_ScrollPos.y = float.MaxValue;
                Save(false);
            }

            if (GUILayout.Button(new GUIContent("Fold", "折叠Group所有Key的参数列表")))
            {
                FoldAll(group);
            }

            if (GUILayout.Button(new GUIContent("Expand", "展开Group所有Key的参数列表")))
            {
                ExpandAll(group);
            }

            bool removed = false;
            if (GUILayout.Button(new GUIContent("Delete", "删除Group")))
            {
                if (EditorUtility.DisplayDialog("Warning", "Are you sure?", "Yes", "No"))
                {
                    removed = true;
                }
            }

            EditorGUILayout.EndHorizontal();
            if (group.Show || !string.IsNullOrEmpty(m_SearchText))
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawDisplayKeys(group);
                });
            }

            return removed;
        }

        private void DrawDisplayKeys(DisplayKeyGroup group)
        {
            var keys = group.Keys;
            for (var i = 0; i < keys.Count; ++i)
            {
                var key = keys[i];

                if (!string.IsNullOrEmpty(m_SearchText))
                {
                    if (key.ID.ToString().IndexOf(m_SearchText) < 0)
                    {
                        continue;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                key.Foldout = EditorHelper.Foldout(key.Foldout);
                key.ID = EditorGUILayout.IntField("", key.ID, GUILayout.MaxWidth(90));
                EditorGUIUtility.labelWidth = 20;
                key.Name = EditorGUILayout.TextField("", key.Name, GUILayout.MaxWidth(100));
                EditorGUIUtility.labelWidth = 40;
                var prefabPath = AssetDatabase.AssetPathToGUID(EditorHelper.ObjectField<GameObject>("Prefab", AssetDatabase.GUIDToAssetPath(key.PrefabPath), 150));
                if (prefabPath != key.PrefabPath)
                {
                    key.PrefabPath = prefabPath;
                    Save(false);
                }
                EditorGUIUtility.labelWidth = 1;
                var iconPath = AssetDatabase.AssetPathToGUID(EditorHelper.ObjectField<Sprite>("", AssetDatabase.GUIDToAssetPath(key.IconPath), 0));
                if (key.IconPath != iconPath)
                {
                    key.IconPath = iconPath;
                    Save(false);
                }

                m_CustomDrawer?.Invoke(key);

                EditorGUIUtility.labelWidth = 50;
                var audioClipPath = AssetDatabase.AssetPathToGUID(EditorHelper.ObjectField<AudioClip>("Audio", AssetDatabase.GUIDToAssetPath(key.AudioClipPath), 140));
                if (key.AudioClipPath != audioClipPath)
                {
                    key.AudioClipPath = audioClipPath;
                    Save(false);
                }
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.Space();

                if (GUILayout.Button(new GUIContent("+", "新建Property"), GUILayout.MaxWidth(20)))
                {
                    key.Foldout = true;

                    var contextMenu = new GenericMenu();
                    contextMenu.AddItem(new GUIContent("string"), false, () => { OnClickAddParam(key, false, "string"); });
                    contextMenu.AddItem(new GUIContent("object"), false, () => { OnClickAddParam(key, true, "object"); });
                    contextMenu.AddItem(new GUIContent("int"), false, () => { OnClickAddParam(key, false, "int"); });
                    contextMenu.AddItem(new GUIContent("float"), false, () => { OnClickAddParam(key, false, "float"); });
                    contextMenu.AddItem(new GUIContent("bool"), false, () => { OnClickAddParam(key, false, "bool"); });
                    contextMenu.AddItem(new GUIContent("vector2"), false, () => { OnClickAddParam(key, false, "vector2"); });
                    contextMenu.AddItem(new GUIContent("vector3"), false, () => { OnClickAddParam(key, false, "vector3"); });
                    contextMenu.AddItem(new GUIContent("vector4"), false, () => { OnClickAddParam(key, false, "vector4"); });
                    contextMenu.AddItem(new GUIContent("color"), false, () => { OnClickAddParam(key, false, "color"); });
                    contextMenu.ShowAsContext();

                    Save(false);
                }

                bool removed = false;
                if (GUILayout.Button(new GUIContent("-", "删除Key"), GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Warning", "Are you sure?", "Yes", "No"))
                    {
                        removed = true;
                        keys.RemoveAt(i);

                        Save(false);
                    }
                }

                bool cloned = false;
                if (GUILayout.Button(new GUIContent("#", "复制"), GUILayout.Width(20)))
                {
                    cloned = true;
                    var newKey = key.Clone();
                    keys.Add(newKey);

                    m_ScrollPos.y = float.MaxValue;

                    Save(false);
                }
                if (GUILayout.Button(new GUIContent("=>", "移动到组"), GUILayout.Width(30)))
                {
                    var contextMenu = new GenericMenu();
                    string[] groupNames = GetGroupNames();
                    for (var g = 0; g < groupNames.Length; ++g)
                    {
                        var index = g;
                        contextMenu.AddItem(new GUIContent(groupNames[g]), false, () => {
                            OnClickMoveToGroup(key, group, index);
                        });
                    }
                    contextMenu.ShowAsContext();
                }

                EditorGUILayout.EndHorizontal();
                if (key.Foldout)
                {
                    EditorGUI.indentLevel += 3;
                    DrawParameters(key);
                    EditorGUI.indentLevel -= 3;
                }

                if (removed || cloned)
                {
                    break;
                }
            }
        }

        private void DrawParameters(DisplayKey key)
        {
            for (var i = 0; i < key.ParameterCount; i++)
            {
                bool removed = DrawParameter(key.GetParameter(i));
                if (removed)
                {
                    key.RemoveParameter(i);
                    break;
                }
            }
        }

        private bool DrawParameter(DisplayKeyParam param)
        {
            EditorGUILayout.BeginHorizontal();
            var aspect = param.Aspect;
            switch (aspect.Value.Type)
            {
                case UnionType.String:
                    {
                        var text = aspect.Value.GetString();
                        var newText = EditorGUILayout.TextField(aspect.Name, text);
                        if (newText != text)
                        {
                            aspect.Value.SetString(newText);
                        }
                    }
                    break;
                case UnionType.Single:
                    {
                        var value = aspect.Value.GetSingle();
                        var newValue = EditorGUILayout.FloatField(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetSingle(newValue);
                        }
                    }
                    break;
                case UnionType.Int32:
                    {
                        var value = aspect.Value.GetInt32();
                        var newValue = EditorGUILayout.IntField(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetInt32(newValue);
                        }
                    }
                    break;
                case UnionType.Boolean:
                    {
                        var value = aspect.Value.GetBoolean();
                        var newValue = EditorGUILayout.Toggle(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetBoolean(newValue);
                        }
                    }
                    break;
                case UnionType.Vector2:
                    {
                        var value = aspect.Value.GetVector2();
                        var newValue = EditorGUILayout.Vector2Field(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetVector2(newValue);
                        }
                    }
                    break;
                case UnionType.Vector3:
                    {
                        var value = aspect.Value.GetVector3();
                        var newValue = EditorGUILayout.Vector3Field(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetVector3(newValue);
                        }
                    }
                    break;
                case UnionType.Vector4:
                    {
                        var value = aspect.Value.GetVector4();
                        var newValue = EditorGUILayout.Vector4Field(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetVector4(newValue);
                        }
                    }
                    break;
                case UnionType.Color:
                    {
                        var value = aspect.Value.GetColor();
                        var newValue = EditorGUILayout.ColorField(aspect.Name, value);
                        if (newValue != value)
                        {
                            aspect.Value.SetColor(newValue);
                        }
                    }
                    break;
                case UnionType.Object:
                    {
                        if (param.UnityObjectType != null)
                        {
                            var obj = aspect.Value.GetObject() as UnityEngine.Object;
                            var newObj = EditorGUILayout.ObjectField(aspect.Name, obj, param.UnityObjectType, false);
                            if (newObj != obj)
                            {
                                aspect.Value.SetObject(newObj);
                            }
                        }
                        else
                        {
                            Log.Instance?.Error($"only support unity object type!");
                        }
                    }
                    break;
                default:
                    Log.Instance?.Error($"todo: {aspect.Value.Type}");
                    break;
            }
            EditorGUILayout.Space();

            bool removed = false;
            if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
            {
                if (EditorUtility.DisplayDialog("Warning", "Are you sure?", "Yes", "No"))
                {
                    removed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            return removed;
        }

        public void Load()
        {
            if (m_Config == null)
            {
                var config = EditorHelper.QueryAsset<DisplayKeyConfig>();
                if (config != null && config.IsValid())
                {
                    m_Config = config;
                }
                else
                {
                    EditorGUILayout.LabelField("DisplayKeyConfig is not valid!");
                }
            }

            m_DisplayKeyManager?.OnDestroy();
            m_DisplayKeyManager = new DisplayKeyManager();

            if (File.Exists(m_Config.EditorDataPath))
            {
                m_DisplayKeyManager.Load(m_Config.EditorDataPath);
            }
        }

        public void Save(bool genCode)
        {
            if (m_DisplayKeyManager == null)
            {
                return;
            }

            var err = Validate();
            if (!string.IsNullOrEmpty(err))
            {
                Debug.LogError(err);
                return;
            }

            m_DisplayKeyManager.Save(m_Config.EditorDataPath);
            m_DisplayKeyManager.Export(m_Config.RuntimeDataPath);

            if (genCode)
            {
                GenerateIDs($"{m_DisplayKeyManager.OutputFolder}/DisplayKeyIDs.cs");
            }

            AssetDatabase.Refresh();
        }

        private void OnClickMoveToGroup(DisplayKey key, DisplayKeyGroup oldGroup, int newGroupIndex)
        {
            oldGroup.Keys.Remove(key);
            m_DisplayKeyManager.Groups[newGroupIndex].Keys.Add(key);
            Save(false);
        }

        private void OnClickAddParam(DisplayKey key, bool showType, string type)
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("Name", "", ""),
            };
            if (showType)
            {
                parameters.Add(new ParameterWindow.StringParameter("Type", "", "Sprite"));
            }
            ParameterWindow.Open("Set Parameter Name", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var name);
                string typename = null;
                if (showType)
                {
                    ok &= ParameterWindow.GetString(p[1], out typename);
                }
                if (ok)
                {
                    if (type == "string")
                    {
                        key.AddString(name, "");
                    }
                    else if (type == "int")
                    {
                        key.AddInt32(name, 0);
                    }
                    else if (type == "float")
                    {
                        key.AddSingle(name, 0);
                    }
                    else if (type == "bool")
                    {
                        key.AddBoolean(name, false);
                    }
                    else if (type == "vector2")
                    {
                        key.AddVector2(name, Vector2.zero);
                    }
                    else if (type == "vector3")
                    {
                        key.AddVector3(name, Vector3.zero);
                    }
                    else if (type == "vector4")
                    {
                        key.AddVector4(name, Vector4.zero);
                    }
                    else if (type == "color")
                    {
                        key.AddColor(name, Color.white);
                    }
                    else if (type == "object")
                    {
                        var type = Helper.SearchTypeByName(typename);
                        if (type != null)
                        {
                            key.AddObject(name, null, type);
                        }
                        else
                        {
                            Debug.Assert(false, $"Invalid type {typename}");
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "todo");
                    }
                }
                return ok;
            });
        }

        private void FoldAll(DisplayKeyGroup group)
        {
            foreach (var key in group.Keys)
            {
                key.Foldout = false;
            }
        }

        private void ExpandAll(DisplayKeyGroup group)
        {
            foreach (var key in group.Keys)
            {
                key.Foldout = true;
            }
        }

        private void CreateGroup()
        {
            m_DisplayKeyManager.CreateGroup("Group");
            m_ScrollPos.y = float.MaxValue;

            Save(false);
        }

        private string Validate()
        {
            HashSet<int> keyIDs = new();
            foreach (var key in m_DisplayKeyManager.AllKeys)
            {
                if (keyIDs.Contains(key.ID))
                {
                    return $"Duplicated key id: {key.ID}";
                }
                else
                {
                    keyIDs.Add(key.ID);
                }
            }
            return null;
        }

        private string[] GetGroupNames()
        {
            string[] names = new string[m_DisplayKeyManager.Groups.Count];
            for (var i = 0; i < m_DisplayKeyManager.Groups.Count; ++i)
            {
                names[i] = m_DisplayKeyManager.Groups[i].Name;
            }
            return names;
        }

        private void GenerateIDs(string fileName)
        {
            var code =
@"
namespace XDay.DisplayKeyID 
{
    $CLASSES$
}
";
            StringBuilder builder = new StringBuilder();
            foreach (var group in m_DisplayKeyManager.Groups)
            {
                builder.AppendLine($"public static class {group.Name} {{");

                for (var i = 0; i < group.Keys.Count; ++i)
                {
                    var key = group.Keys[i];
                    if (!string.IsNullOrEmpty(key.Name))
                    {
                        builder.AppendLine($"public const int {key.Name.Replace(" ", "_")} = {key.ID};");
                    }
                }

                builder.AppendLine("}");
            }

            builder.AppendLine("");

            code = code.Replace("$CLASSES$", builder.ToString());

            File.WriteAllText(fileName, code);

            SerializationHelper.FormatCode(fileName);
        }

        internal void OnDestroy()
        {
            m_DisplayKeyManager?.OnDestroy();
        }

        private DisplayKeyManager m_DisplayKeyManager;
        private string m_SearchText = "";
        private DisplayKeyConfig m_Config;
        private Vector2 m_ScrollPos;
        private Action<DisplayKey> m_CustomDrawer;
    }
}
