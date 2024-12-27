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

namespace XDay.WorldAPI.Editor
{
    public abstract class WorldPluginCreateWindow : EditorWindow
    {
        public void Show(System.Action onPluginCreated, World world)
        {
            m_OnPluginCreated = onPluginCreated;
            m_World = world;

            m_Name = DisplayName;
            m_Width = m_World.Width;
            m_Height = m_World.Height;

            Show();

            ShowInternal();
        }

        private void OnDisable()
        {
            DisabledInternal();
        }

        private void OnGUI()
        {
            m_Name = EditorGUILayout.TextField("Name", m_Name);

            if (m_DrawSize)
            {
                GUILayout.BeginHorizontal();

                m_Width = EditorGUILayout.FloatField("Width", m_Width);
                m_Height = EditorGUILayout.FloatField("Height", m_Height);

                GUILayout.EndHorizontal();
            }

            GUIInternal();

            if (GUILayout.Button("Create"))
            {
                var err = Validate();
                if (string.IsNullOrEmpty(err))
                {
                    CreateInternal();

                    m_OnPluginCreated?.Invoke();

                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", err, "OK");
                }
            }
        }

        private string Validate()
        {
            if (string.IsNullOrEmpty(m_Name))
            {
                return "Invalid name";
            }

            if (m_Width <= 0 || m_Height <= 0)
            {
                return "Invalid size";
            }

            if (m_World.HasPlugin(m_Name))
            {
                return "Name already used!";
            }

            return ValidateInternal();
        }

        protected virtual void ShowInternal() { }
        protected virtual void DisabledInternal() { }
        protected abstract void CreateInternal();
        protected virtual string ValidateInternal() { return null; }
        protected abstract void GUIInternal();

        protected World World => m_World;
        protected abstract string DisplayName { get; }

        protected bool m_DrawSize = true;
        protected string m_Name;
        protected float m_Width;
        protected float m_Height;
        private System.Action m_OnPluginCreated;
        private World m_World;
    }

    public abstract class GridBasedWorldPluginCreateWindow : WorldPluginCreateWindow
    {
        protected Vector2 CalculateOrigin(float width, float height)
        {
            if (SetGridCount)
            {
                return Vector2.zero;
            }
            var center = World.Bounds.center;
            return new Vector2(center.x - width * 0.5f, center.y - height * 0.5f);
        }

        protected override string ValidateInternal()
        {
            if (SetGridCount)
            {
                if (m_GridCountX <= 0 || m_GridCountY <= 0)
                {
                    return "Invalid grid count";
                }
            }
            else
            {
                if (m_GridWidth <= 0 || m_GridHeight <= 0)
                {
                    return "Invalid grid size";
                }
            }

            return null;
        }

        protected override void GUIInternal()
        {
            GUILayout.BeginHorizontal();

            if (SetGridCount)
            {
                m_GridCountX = EditorGUILayout.IntField("Horizontal Grid Count", m_GridCountX);
                m_GridCountY = EditorGUILayout.IntField("Vertical Grid Count", m_GridCountY);
            }
            else
            {
                m_GridWidth = EditorGUILayout.FloatField("Grid Width", m_GridWidth);
                m_GridHeight = EditorGUILayout.FloatField("Grid Height", m_GridHeight);
            }

            GUILayout.EndHorizontal();
        }

        protected abstract bool SetGridCount { get; }

        protected int m_GridCountX = 10;
        protected int m_GridCountY = 10;
        protected float m_GridWidth = 100;
        protected float m_GridHeight = 100;
    }

    public abstract class GenericWorldPluginCreateWindow : WorldPluginCreateWindow
    {
        protected override void GUIInternal()
        {
        }
    }
}

//XDay
