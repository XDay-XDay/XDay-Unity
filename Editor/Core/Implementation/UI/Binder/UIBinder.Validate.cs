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



using System.Collections.Generic;

namespace XDay.GUIAPI.Editor
{
    internal partial class UIBinder
    {
        private string Validate()
        {
            HashSet<string> variableNames = new();
            HashSet<string> eventHandlerNames = new();
            if (string.IsNullOrEmpty(m_Metadata.ViewClassName))
            {
                return "View class name is null";
            }

            if (string.IsNullOrEmpty(m_Metadata.ControllerClassName))
            {
                return "Controller class name is null";
            }

            if (m_Metadata.ControllerClassName == m_Metadata.ViewClassName)
            {
                return "Controller and View class name is same";
            }

            if (string.IsNullOrEmpty(m_Metadata.PrefabGUID))
            {
                return "Prefab guid is null";
            }

            foreach (var go in m_Metadata.GameObjects)
            {
                var gameObject = UIEditorHelper.GetGameObjectFromGUIDAndLocalID(m_Metadata.PrefabGUID, go.localID);

                if (go.BindGameObject)
                {
                    if (string.IsNullOrEmpty(go.GameObjectVariableName))
                    {
                        return $"{gameObject.name} game object variable name is null";
                    }
                    bool ok = variableNames.Add(go.GameObjectVariableName);
                    if (!ok)
                    {
                        return $"Duplicated name {go.GameObjectVariableName}";
                    }
                }

                if (go.BindView)
                {
                    if (string.IsNullOrEmpty(go.ViewVariableName) || string.IsNullOrEmpty(go.ViewClassName))
                    {
                        return $"{gameObject.name} view variable name or view class name is null";
                    }
                    bool ok =variableNames.Add(go.ViewVariableName);
                    if (!ok)
                    {
                        return $"Duplicated name {go.ViewVariableName}";
                    }
                }

                foreach (var component in go.Components)
                {
                    if (string.IsNullOrEmpty(component.VariableName))
                    {
                        return $"{gameObject.name}-{component.TypeName} variable name is null";
                    }
                    bool ok = variableNames.Add(component.VariableName);
                    if (!ok)
                    {
                        return $"Duplicated name {component.VariableName}";
                    }
                }

                foreach (var e in go.Events)
                {
                    if (string.IsNullOrEmpty(e.HandlerName))
                    {
                        return $"{gameObject.name}-{e.Type} handler name is null";
                    }

                    bool ok = eventHandlerNames.Add(e.HandlerName);
                    if (!ok)
                    {
                        return $"Duplicated event handler name {e.HandlerName}";
                    }
                }
            }

            return "";
        }
    }
}
