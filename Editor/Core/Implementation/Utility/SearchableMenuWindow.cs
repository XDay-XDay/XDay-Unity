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
using System.Collections.Generic;
using System.Linq;

namespace XDay.UtilityAPI.Editor
{
    public class SearchableMenuWindow : EditorWindow
    {
        private string _searchText = "";
        private Vector2 _scrollPos;
        private List<string> _allItems;
        private List<string> _filteredItems;
        private int _selectedIndex = -1;
        private System.Action<int, string> _onItemSelected;

        public static void Show(Vector2 position, List<string> items, System.Action<int, string> onItemSelected)
        {
            var window = CreateInstance<SearchableMenuWindow>();
            window.position = new Rect(position, new Vector2(200, 750));
            window._allItems = items;
            window._filteredItems = items.ToList();
            window._onItemSelected = onItemSelected;
            window.ShowPopup();
        }

        private void OnEnable()
        {
            // 自动聚焦到搜索框
            if (GUI.GetNameOfFocusedControl() != "SearchField")
            {
                EditorGUI.FocusTextInControl("SearchField");
            }
        }

        private void OnGUI()
        {
            CreateStyle();

            // 搜索框
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("SearchField");
            _searchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();

            // 实时过滤
            if (Event.current.type == EventType.KeyDown)
            {
                HandleKeyboardNavigation();
            }

            UpdateFilteredItems();

            // 列表显示
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _filteredItems.Count; i++)
            {
                if (GUILayout.Button(_filteredItems[i], m_Style))
                {
                    SelectItem(i, _filteredItems[i]);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void UpdateFilteredItems()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredItems = _allItems.ToList();
            }
            else
            {
                _filteredItems = _allItems
                    .Where(item => item.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        }

        private void HandleKeyboardNavigation()
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.DownArrow:
                    _selectedIndex = Mathf.Min(_selectedIndex + 1, _filteredItems.Count - 1);
                    Event.current.Use();
                    break;
                case KeyCode.UpArrow:
                    _selectedIndex = Mathf.Max(_selectedIndex - 1, 0);
                    Event.current.Use();
                    break;
                case KeyCode.Return:
                    if (_selectedIndex >= 0 && _selectedIndex < _filteredItems.Count)
                    {
                        SelectItem(_selectedIndex, _filteredItems[_selectedIndex]);
                    }
                    break;
            }
        }

        private void SelectItem(int index, string item)
        {
            _onItemSelected?.Invoke(index, item);
            Close();
        }

        private void CreateStyle()
        {
            if (m_Style != null)
            {
                return;
            }

            m_Style = new GUIStyle(GUI.skin.button);
            m_Style.normal.background = CreateSolidTexture(2, 2, new Color32(50, 50, 50, 255));
            m_Style.hover.background = CreateSolidTexture(2, 2, new Color32(255, 201, 14, 150));
            m_Style.alignment = TextAnchor.MiddleCenter;
        }

        private Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnLostFocus() => Close();

        private GUIStyle m_Style;
    }
}