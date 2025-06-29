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
using System;
using System.Collections.Generic;

namespace XDay.GUIAPI
{
    public enum UIEventType
    {
        Click,
        Down,
        Up,
        Enter,
        Exit,
        Drag,
        BeginDrag,
        EndDrag,
    }

    [Serializable]
    public class UIComponentMetadata
    {
        public long localID;
        public string VariableName;
        public string TypeName;
    }

    [Serializable]
    public class UIEventMetadata
    {
        public UIEventType Type;
        public string HandlerName;
    }

    [Serializable]
    public class UIGameObjectMetadata
    {
        public long localID;
        public bool BindGameObject = false;
        public string GameObjectVariableName;
        public bool BindView;
        public string ViewVariableName;
        public string ViewClassName;
        public List<UIComponentMetadata> Components = new();
        public List<UIEventMetadata> Events = new();
    }

    [Serializable]
    public class UIMetadata
    {
        public string ViewClassName;
        public string ControllerClassName;
        public string Namespace;
        public string PrefabGUID;
        public UIWindowLayer WindowLayer = UIWindowLayer.Layer0;
        public List<UIGameObjectMetadata> GameObjects = new();
    }

    [CreateAssetMenu(fileName = "UIMetadataManager.asset", menuName = "XDay/UI/Metadata Manager")]
    public class UIMetadataManager : ScriptableObject
    {
        public List<UIMetadata> UIMetadatas = new();

        public string GetViewClassName(string guid)
        {
            foreach (var metadata in UIMetadatas)
            {
                if (metadata.PrefabGUID == guid)
                {
                    return metadata.ViewClassName;
                }
            }

            return "";
        }
    }
}
