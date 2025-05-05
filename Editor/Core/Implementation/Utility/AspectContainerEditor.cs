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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public class AspectContainerEditor
    {
        public IAspectContainer Properties => m_Properties;

        public void Draw(
            bool canEdit,
            IAspectContainer properties, 
            System.Action<INamedAspect> onAddProperty = null, 
            System.Action<INamedAspect> onRemoveProperty = null, 
            System.Action<string, INamedAspect> onRenameProperty = null)
        {
            m_CanEdit = canEdit;
            m_Properties = properties;
            m_OnAddProperty = onAddProperty;
            m_OnRemoveProperty = onRemoveProperty;
            m_OnRenameProperty = onRenameProperty;

            EditorGUILayout.BeginHorizontal();
            m_Visible = EditorGUILayout.Foldout(m_Visible, "属性");
            EditorGUILayout.Space();
            if (m_CanEdit)
            {
                if (GUILayout.Button("+", GUILayout.MaxWidth(20)))
                {
                    var items = new List<ParameterWindow.Parameter> {
                                new ParameterWindow.StringParameter("名称", "", "New Property"),
                                new ParameterWindow.StringArrayParameter("类型", "", m_PropertyTypeNames),
                            };
                    ParameterWindow.Open("添加属性", items, (parameters) =>
                    {
                        var ok = ParameterWindow.GetString(parameters[0], out var name);
                        ok &= ParameterWindow.GetStringArraySelection(parameters[1], out var type);
                        if (ok && m_Properties.QueryAspect(name) == null)
                        {
                            AddProperty(name, m_PropertyTypes[type]);
                            return true;
                        }
                        return false;
                    });
                }
            }
            EditorGUILayout.EndHorizontal();
            if (m_Visible)
            {
                DrawPropertyListUI();
            }
        }

        private void AddProperty(string propertyName, UnionType type)
        {
            if (m_Properties == null)
            {
                return;
            }

            INamedAspect prop = null;
            switch (type)
            {
                case UnionType.Int32:
                    prop = INamedAspect.Create(IAspect.FromInt32(0), propertyName);
                    break;
                case UnionType.Single:
                    prop = INamedAspect.Create(IAspect.FromSingle(0), propertyName);
                    break;
                case UnionType.String:
                    prop = INamedAspect.Create(IAspect.FromString(""), propertyName);
                    break;
                case UnionType.Boolean:
                    prop = INamedAspect.Create(IAspect.FromBoolean(false), propertyName);
                    break;
                case UnionType.Vector2:
                    prop = INamedAspect.Create(IAspect.FromVector2(Vector2.zero), propertyName);
                    break;
                case UnionType.Vector3:
                    prop = INamedAspect.Create(IAspect.FromVector3(Vector3.zero), propertyName);
                    break;
                case UnionType.Vector4:
                    prop = INamedAspect.Create(IAspect.FromVector4(Vector4.zero), propertyName);
                    break;
                case UnionType.Color:
                    prop = INamedAspect.Create(IAspect.FromColor(Color.white), propertyName);
                    break;
                default:
                    Debug.Assert(false, $"unknown property type {type}");
                    break;
            }
            if (prop != null)
            {
                m_Properties.AddAspect(prop);

                if (m_OnAddProperty != null)
                {
                    m_OnAddProperty(prop);
                }
                return;
            }
        }

        private void DrawPropertyListUI()
        {
            EditorGUIUtility.labelWidth = 100;
            if (m_Properties != null)
            {
                var n = m_Properties.AspectCount;

                if (n > 0)
                {
                    EditorGUILayout.BeginVertical("GroupBox");
                }

                for (int i = 0; i < n; ++i)
                {
                    var property = m_Properties.GetAspect(i);
                    var type = property.Value.Type;
                    if (type == UnionType.Int32)
                    {
                        DrawIntProperty(property, m_Properties);
                    }
                    else if (type == UnionType.Single)
                    {
                        DrawFloatProperty(property, m_Properties);
                    }
                    else if (type == UnionType.String)
                    {
                        DrawStringProperty(property, m_Properties);
                    }
                    else if (type == UnionType.Vector2)
                    {
                        DrawVector2Property(property, m_Properties);
                    }
                    else if (type == UnionType.Vector3)
                    {
                        DrawVector3Property(property, m_Properties);
                    }
                    else if (type == UnionType.Vector4)
                    {
                        DrawVector4Property(property, m_Properties);
                    }
                    else if (type == UnionType.Color)
                    {
                        DrawColorProperty(property, m_Properties);
                    }
                    else if (type == UnionType.Boolean)
                    {
                        DrawBoolProperty(property, m_Properties);
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
                if (n > 0)
                {
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUIUtility.labelWidth = 0;
        }

        private void AddPropertyButtons(INamedAspect property, IAspectContainer properties)
        {
            if (m_CanEdit)
            {
                if (GUILayout.Button("重命名", GUILayout.Width(60)))
                {
                    var items = new List<ParameterWindow.Parameter> 
                    {
                        new ParameterWindow.StringParameter("新名称", "", property.Name),
                    };
                    ParameterWindow.Open("修改名称", items, (parameters) =>
                    {
                        bool ok = ParameterWindow.GetString(parameters[0], out var newName);
                        if (ok && properties.QueryAspect(newName) == null)
                        {
                            string oldName = property.Name;
                            property.Name = newName;
                            m_OnRenameProperty?.Invoke(oldName, property);
                            return true;
                        }
                        return false;
                    });
                }
                if (GUILayout.Button("删除", GUILayout.Width(40)))
                {
                    if (EditorUtility.DisplayDialog("删除属性", "继续?", "确定", "取消"))
                    {
                        m_Properties.RemoveAspect(property.Name);
                        if (m_OnRemoveProperty != null)
                        {
                            m_OnRemoveProperty(property);
                        }
                    }
                }
            }
        }

        private void DrawIntProperty(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetInt32(EditorGUILayout.IntField(property.Name, property.Value.GetInt32()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFloatProperty(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetSingle(EditorGUILayout.FloatField(property.Name, property.Value.GetSingle()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStringProperty(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetString(EditorGUILayout.TextField(property.Name, property.Value.GetString()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVector2Property(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetVector2(EditorGUILayout.Vector2Field(property.Name, property.Value.GetVector2()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVector3Property(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetVector3(EditorGUILayout.Vector3Field(property.Name, property.Value.GetVector3()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVector4Property(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetVector4(EditorGUILayout.Vector4Field(property.Name, property.Value.GetVector4()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColorProperty(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetColor(EditorGUILayout.ColorField(property.Name, property.Value.GetColor()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBoolProperty(INamedAspect property, IAspectContainer properties)
        {
            EditorGUILayout.BeginHorizontal();
            property.Value.SetBoolean(EditorGUILayout.Toggle(property.Name, property.Value.GetBoolean()));
            AddPropertyButtons(property, properties);
            EditorGUILayout.EndHorizontal();
        }

        private IAspectContainer m_Properties;
        private System.Action<INamedAspect> m_OnAddProperty;
        private System.Action<INamedAspect> m_OnRemoveProperty;
        private System.Action<string, INamedAspect> m_OnRenameProperty;
        private readonly string[] m_PropertyTypeNames = new string[] 
        {
            "Int32",
            "Float",
            "Boolean",
            "String",
            "Vector2",
            "Vector3",
            "Vector4",
            "Color",
        };
        private readonly UnionType[] m_PropertyTypes = new UnionType[]
        {
            UnionType.Int32,
            UnionType.Single,
            UnionType.Boolean,
            UnionType.String,
            UnionType.Vector2,
            UnionType.Vector3,
            UnionType.Vector4,
            UnionType.Color,
        };
        private bool m_Visible = true;
        private bool m_CanEdit;
    }
}

