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


using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    partial class CodeAssistantTreeView : TreeView
    {
        void CreateSearchResult()
        {
            List<object> matchedObjects = new List<object>();
            for (int i = 0; i < m_Data.groups.Count; ++i)
            {
                var group = m_Data.groups[i];
                CreateSearchResultInGroup(group, matchedObjects);
            }

            int idx = 0;
            foreach (var obj in matchedObjects)
            {
                if (obj is TypeInfo type)
                {
                    CreateType(type, 0, idx, "search result");
                }
                else if (obj is MethodInfo method)
                {
                    CreateMethod(method, 0, idx, "search result");
                }
                else if (obj is PropertyInfo property)
                {
                    CreateProperty(property, 0, idx, "search result");
                }
                else
                {
                    Debug.Assert(false, "todo");
                }
                ++idx;
            }
        }

        void CreateSearchResultInGroup(GroupInfo group, List<object> matchedObjects)
        {
            foreach (var type in group.types)
            {
                if (MatchSearch(type.nameInfo.name, SearchType.Type))
                {
                    matchedObjects.Add(type);
                }

                foreach (var method in type.methods)
                {
                    if (MatchSearch(method.name, SearchType.Method))
                    {
                        matchedObjects.Add(method);
                    }
                }

                foreach (var property in type.properties)
                {
                    if (MatchSearch(property.name, SearchType.Property))
                    {
                        matchedObjects.Add(property);
                    }
                }
            }
        }

        bool MatchSearch(string name, SearchType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(m_Data.searchText));

            if (m_Data.searchType.HasFlag(type))
            {
                StringComparison compare = m_Data.searchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                if (m_Data.searchMode == SearchMode.MatchPart)
                {
                    if (name.IndexOf(m_Data.searchText, compare) >= 0)
                    {
                        return true;
                    }
                }
                else if (m_Data.searchMode == SearchMode.MatchStart)
                {
                    if (name.StartsWith(m_Data.searchText, compare))
                    {
                        return true;
                    }
                }
                else
                {
                    Debug.Assert(false, "todo");
                }
            }
            return false;
        }

        public string searchText
        {
            get { return m_Data.searchText; }
            set
            {
                if (m_Data.searchText != value)
                {
                    m_Data.searchText = value;
                    if (string.IsNullOrEmpty(m_Data.searchText))
                    {
                        m_SelectedType = null;
                        m_SelectedProperty = null;
                        m_SelectedMethod = null;

                        var selection = GetSelection();
                        if (selection.Count > 0)
                        {
                            foreach (var id in selection)
                            {
                                var item = GetTreeViewItem(id) as Item;        
                                if (item != null)
                                {
                                    if (item is TypeItem typeItem)
                                    {
                                        m_SelectedType = typeItem.type;
                                    }
                                    else if (item is MethodItem methodItem)
                                    {
                                        m_SelectedType = methodItem.method.owner;
                                        m_SelectedMethod = methodItem.method;
                                    }
                                    else if (item is PropertyItem propertyItem)
                                    {
                                        m_SelectedType = propertyItem.property.owner;
                                        m_SelectedProperty = propertyItem.property;
                                    }
                                    SetFocus();
                                    break;
                                }
                            }
                        }
                    }
                    Reload();
                }
            }
        }
    }
}


