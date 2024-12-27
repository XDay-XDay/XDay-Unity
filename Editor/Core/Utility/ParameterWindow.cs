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

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public class ParameterWindow : EditorWindow
    {
        public class Parameter
        {
            public Parameter(string n, string tooltip)
            {
                name = n;
                this.tooltip = tooltip;
            }

            public string name;
            public string tooltip;
        }

        public class IntParameter : Parameter
        {
            public IntParameter(string name, string tooltip, int value) : base(name, tooltip)
            {
                this.value = value;
            }

            public int value;
        }

        public class FloatParameter : Parameter
        {
            public FloatParameter(string name, string tooltip, float value) : base(name, tooltip)
            {
                this.value = value;
            }

            public float value;
        }

        public class StringParameter : Parameter
        {
            public StringParameter(string name, string tooltip, string text) : base(name, tooltip)
            {
                this.text = text;
            }

            public string text;
        }

        public class StringListParameter : Parameter
        {
            public StringListParameter(string name, string tooltip, string[] texts) : base(name, tooltip)
            {
                this.texts = texts;
                if (texts.Length > 0)
                {
                    selection = 0;
                }
                else
                {
                    selection = -1;
                }
            }

            public string[] texts;
            public int selection = 0;
        }

        public class ObjectParameter : Parameter
        {
            public ObjectParameter(string name, string tooltip, Object obj, System.Type type, bool allowSceneObject) : base(name, tooltip)
            {
                this.obj = obj;
                this.type = type;
                this.allowSceneObject = allowSceneObject;
            }

            public Object obj;
            public System.Type type;
            public bool allowSceneObject;
        }

        public class BoolParameter : Parameter
        {
            public BoolParameter(string name, string tooltip, bool val, bool toggleLeft = false) : base(name, tooltip)
            {
                value = val;
                this.toggleLeft = toggleLeft;
            }

            public bool value;
            public bool toggleLeft;
        }

        public class EnumParameter : Parameter
        {
            public EnumParameter(string name, string tooltip, System.Enum val) : base(name, tooltip)
            {
                value = val;
            }

            public System.Enum value;
        }

        public class PathParameter : Parameter
        {
            public PathParameter(string name, string tooltip, string text) : base(name, tooltip)
            {
                this.text = text;
            }

            public string text;
        }

        public class ColorParameter : Parameter
        {
            public ColorParameter(string name, string tooltip, Color color) : base(name, tooltip)
            {
                this.color = color;
            }

            public Color color;
        }

        public static ParameterWindow Open(string title, List<Parameter> parameters, System.Func<List<Parameter>, bool> onClickOK, bool setSize = true)
        {
            var window = Open(title, setSize);

            window.Show(parameters, onClickOK);

            return window;
        }

        static ParameterWindow Open(string title, bool setSize = true)
        {
            var inputDialog = GetWindow<ParameterWindow>(title);
            if (setSize)
            {
                inputDialog.minSize = new Vector2(200, 500);
            }
            var position = inputDialog.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            inputDialog.position = position;

            return inputDialog;
        }

        public void Show(List<Parameter> items, System.Func<List<Parameter>, bool> OnClickOK, float labelWidth = 0)
        {
            m_Parameters = items;
            m_OnClickOK = OnClickOK;
            m_LabelWidth = labelWidth;
        }

        public static bool GetInt(Parameter param, out int ret)
        {
            ret = (param as IntParameter).value;
            return true;
        }

        public static bool GetFloat(Parameter param, out float ret)
        {
            ret = (param as FloatParameter).value;
            return true;
        }

        public static bool GetBool(Parameter param, out bool ret)
        {
            ret = (param as BoolParameter).value;
            return true;
        }

        public static bool GetEnum<T>(Parameter param, out T ret) where T : System.Enum
        {
            ret = (T)((param as EnumParameter).value);
            return true;
        }

        public static bool GetObject<T>(Parameter param, out T ret) where T : UnityEngine.Object
        {
            ret = (param as ObjectParameter).obj as T;
            return true;
        }

        public static bool GetColor(Parameter param, out Color ret)
        {
            ret = (param as ColorParameter).color;
            return true;
        }

        public static bool GetString(Parameter param, out string ret)
        {
            ret = (param as StringParameter).text;
            return !string.IsNullOrEmpty(ret);
        }

        public static bool GetStringList(Parameter param, out string[] ret)
        {
            ret = (param as StringListParameter).texts;
            return true;
        }

        public static bool GetPath(Parameter param, out string ret)
        {
            ret = (param as PathParameter).text;
            return !string.IsNullOrEmpty(ret);
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = m_LabelWidth;
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < m_Parameters.Count; ++i)
            {
                if (m_Parameters[i] is IntParameter intParam)
                {
                    intParam.value = EditorGUILayout.IntField(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), intParam.value);
                }
                else if (m_Parameters[i] is FloatParameter floatParam)
                {
                    floatParam.value = EditorGUILayout.FloatField(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), floatParam.value);
                }
                else if (m_Parameters[i] is StringParameter stringParam)
                {
                    stringParam.text = EditorGUILayout.TextField(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), stringParam.text);
                }
                else if (m_Parameters[i] is StringListParameter stringListParam)
                {
                    stringListParam.selection = EditorGUILayout.Popup(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), stringListParam.selection, stringListParam.texts);
                }
                else if (m_Parameters[i] is ObjectParameter objParam)
                {
                    objParam.obj = EditorGUILayout.ObjectField(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), objParam.obj, objParam.type, objParam.allowSceneObject);
                }
                else if (m_Parameters[i] is PathParameter pathParam)
                {
                    EditorGUILayout.BeginHorizontal();
                    pathParam.text = EditorGUILayout.TextField(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), pathParam.text);
                    if (GUILayout.Button("..."))
                    {
                        pathParam.text = EditorUtility.OpenFolderPanel("Select folder", "", "");
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (m_Parameters[i] is BoolParameter boolParam)
                {
                    if (boolParam.toggleLeft)
                    {
                        boolParam.value = EditorGUILayout.ToggleLeft(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), boolParam.value);
                    }
                    else
                    {
                        boolParam.value = EditorGUILayout.Toggle(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), boolParam.value);
                    }
                }
                else if (m_Parameters[i] is ColorParameter colorParam)
                {
                    colorParam.color = EditorGUILayout.ColorField(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), colorParam.color);
                }
                else if (m_Parameters[i] is EnumParameter enumParam)
                {
                    enumParam.value = EditorGUILayout.EnumPopup(new GUIContent(m_Parameters[i].name, m_Parameters[i].tooltip), enumParam.value);
                }
                else
                {
                    Debug.Assert(false, "todo");
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0;

            if (GUILayout.Button("OK"))
            {
                if (m_OnClickOK != null)
                {
                    if (m_OnClickOK(m_Parameters))
                    {
                        Close();
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        private float m_LabelWidth;
        private System.Func<List<Parameter>, bool> m_OnClickOK;
        private List<Parameter> m_Parameters = new List<Parameter>();
    }
}

//XDay

