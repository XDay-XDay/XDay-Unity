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



using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    public abstract class UIControl
    {
        protected void RenderInternal()
        {
            if (Event.current.type != EventType.Layout)
            {
                m_Bounds = GUILayoutUtility.GetLastRect();
            }
        }

        public Rect Bounds { get { return m_Bounds; } }

        protected Rect m_Bounds;
    }

    public class Popup : UIControl
    {
        public Popup(string label, string tooltip, int size)
        {
            m_Label = label;
            m_Tooltip = tooltip;
            m_Size = size;
        }

        public int Render(int index, string[] names, float labelWidth)
        {
            EditorGUIUtility.labelWidth = labelWidth;
            int newIndex = EditorGUILayout.Popup(new GUIContent(m_Label, m_Tooltip), index, names, GUILayout.MaxWidth(m_Size));
            EditorGUIUtility.labelWidth = 0;
            RenderInternal();
            return newIndex;
        }

        private string m_Label;
        private string m_Tooltip;
        private int m_Size;
    }

    public class EnumPopup : UIControl
    {
        public EnumPopup(string label, string tooltip, int size)
        {
            m_Label = label;
            m_Size = size;
            m_Tooltip = tooltip;
        }

        public System.Enum Render(System.Enum value, float labelWidth)
        {
            EditorGUIUtility.labelWidth = labelWidth;
            value = EditorGUILayout.EnumPopup(new GUIContent(m_Label, m_Tooltip), value, GUILayout.MaxWidth(m_Size));
            EditorGUIUtility.labelWidth = 0;
            RenderInternal();
            return value;
        }

        private string m_Label;
        private string m_Tooltip;
        private int m_Size;
    }

    public class FloatField : UIControl
    {
        public FloatField(string label, string tooltip, int size)
        {
            m_Label = label;
            m_Tooltip = tooltip;
            m_Size = size;
        }

        public float Render(float value, float labelWidth)
        {
            return RenderInternal(value, labelWidth);
        }

        public float Render(float value, float labelWidth, float minValue)
        {
            value = RenderInternal(value, labelWidth);
            value = Mathf.Max(minValue, value);
            return value;
        }

        public float Render(float value, float labelWidth, float minValue, float maxValue)
        {
            value = RenderInternal(value, labelWidth);
            value = Mathf.Clamp(value, minValue, maxValue);
            return value;
        }

        private float RenderInternal(float value, float labelWidth)
        {
            EditorGUIUtility.labelWidth = labelWidth;
            value = EditorGUILayout.FloatField(new GUIContent(m_Label, m_Tooltip), value, GUILayout.MaxWidth(m_Size));
            EditorGUIUtility.labelWidth = 0;
            RenderInternal();
            return value;
        }

        private int m_Size;
        private string m_Tooltip;
        private string m_Label;
    }

    public class ImageButton : UIControl
    {
        public Texture Image { set=>m_Image = value; }

        public ImageButton(Texture image, string tooltip, int size)
        {
            m_Image = image;
            m_Tooltip = tooltip;
            m_Size = size;
        }

        public bool Render(bool enabled)
        {
            var old = GUI.enabled;
            GUI.enabled = enabled;
            bool clicked = false;
            if (GUILayout.Button(new GUIContent(m_Image, m_Tooltip), GUILayout.MaxWidth(m_Size), GUILayout.MaxHeight(m_Size)))
            {
                clicked = true;
            }
            RenderInternal();
            GUI.enabled = old;
            return clicked;
        }

        public bool Render(int x, int y)
        {
            bool clicked = false;
            m_Bounds = new Rect(x, y, m_Size, m_Size);
            if (GUI.Button(m_Bounds, new GUIContent(m_Image, m_Tooltip)))
            {
                clicked = true;
            }
            return clicked;
        }

        private Texture m_Image;
        private int m_Size;
        private string m_Tooltip;
    }

    public class ToggleImageButton : UIControl
    {
        public bool Active { get { return m_Active; } set { m_Active = value; } }

        public ToggleImageButton(Texture onIcon, Texture offIcon, string tooltip, bool active, int size)
        {
            m_OnIcon = onIcon;
            m_OffIcon = offIcon;
            m_Tooltip = tooltip;
            m_Active = active;
            m_Size = size;
        }

        public bool Render(bool changeColor, bool enabled = true)
        {
            bool clicked = false;
            bool old = GUI.enabled;
            GUI.enabled = enabled;
            if (RenderToggleImageButton(m_OnIcon, m_OffIcon, m_Tooltip, m_Active, m_Size, changeColor))
            {
                clicked = true;
                m_Active = !m_Active;
            }
            GUI.enabled = old;
            RenderInternal();
            return clicked;
        }

        public bool Render(int x, int y, bool changeColor)
        {
            bool clicked = false;
            m_Bounds = new Rect(x, y, m_Size, m_Size);
            if (RenderToggleImageButton(m_Bounds, m_OnIcon, m_OffIcon, m_Tooltip, m_Active, changeColor))
            {
                clicked = true;
                m_Active = !m_Active;
            }
            return clicked;
        }

        private bool RenderToggleImageButton(Texture onImage, Texture offImage, string tooltip, bool active, int size, bool changeColor)
        {
            var color = GUI.backgroundColor;
            Texture image;
            if (active)
            {
                image = onImage;
                if (changeColor)
                {
                    GUI.backgroundColor = m_ActiveColor;
                }
            }
            else
            {
                image = offImage;
                if (changeColor)
                {
                    GUI.backgroundColor = m_InactiveColor;
                }
            }

            bool clicked = GUILayout.Button(new GUIContent(image, tooltip), GUILayout.MaxWidth(size), GUILayout.MaxHeight(size));
            GUI.backgroundColor = color;
            return clicked;
        }

        private bool RenderToggleImageButton(Rect rect, Texture onImage, Texture offImage, string tooltip, bool active, bool changeColor)
        {
            var color = GUI.backgroundColor;
            Texture image;
            if (active)
            {
                if (changeColor)
                {
                    GUI.backgroundColor = m_ActiveColor;
                }
                image = onImage;
            }
            else
            {
                if (changeColor)
                {
                    GUI.backgroundColor = m_InactiveColor;
                }
                image = offImage;
            }

            bool ret = GUI.Button(rect, new GUIContent(image, tooltip));
            GUI.backgroundColor = color;
            return ret;
        }

        private bool m_Active;
        private Texture m_OnIcon;
        private Texture m_OffIcon;
        private string m_Tooltip;
        private int m_Size;
        private static Color m_ActiveColor = new Color32(150, 230, 123, 255);
        private static Color m_InactiveColor = Color.white;
    }

    public class IntField : UIControl
    {
        public IntField(string label, string tooltip, int size)
        {
            m_Label = label;
            m_Tooltip = tooltip;
            m_Size = size;
        }

        public int Render(int value, float labelWidth)
        {
            return RenderInternal(value, labelWidth);
        }

        public int Render(int value, float labelWidth, int minValue)
        {
            value = RenderInternal(value, labelWidth);
            value = Mathf.Max(minValue, value);
            return value;
        }

        public int Render(int value, float labelWidth, int minValue, int maxValue)
        {
            value = RenderInternal(value, labelWidth);
            value = Mathf.Clamp(value, minValue, maxValue);
            return value;
        }

        private int RenderInternal(int value, float labelWidth)
        {
            EditorGUIUtility.labelWidth = labelWidth;
            value = EditorGUILayout.IntField(new GUIContent(m_Label, m_Tooltip), value, GUILayout.MaxWidth(m_Size));
            EditorGUIUtility.labelWidth = 0;
            RenderInternal();
            return value;
        }

        private int m_Size;
        private string m_Tooltip;
        private string m_Label;
    }

    public class ColorField : UIControl
    {
        public ColorField(string label, string tooltip, int size)
        {
            m_Label = label;
            m_Tooltip = tooltip;
            m_Size = size;
        }

        public Color Render(Color color, float labelWidth)
        {
            return RenderInternal(color, labelWidth);
        }

        private Color RenderInternal(Color color, float labelWidth)
        {
            EditorGUIUtility.labelWidth = labelWidth;
            color = EditorGUILayout.ColorField(new GUIContent(m_Label, m_Tooltip), color, GUILayout.MaxWidth(m_Size));
            EditorGUIUtility.labelWidth = 0;
            RenderInternal();
            return color;
        }

        private int m_Size;
        private string m_Tooltip;
        private string m_Label;
    }
}

//XDay