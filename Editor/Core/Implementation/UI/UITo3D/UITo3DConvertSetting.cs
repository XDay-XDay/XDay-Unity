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
using UnityEngine;

namespace XDay.GUIAPI.Editor
{
    [CreateAssetMenu(menuName = "XDay/UI/UITo3DConvertSetting")]
    public class UITo3DConvertSetting : ScriptableObject
    {
        public float OrthoSize = 4.3f;
        public bool EnableAnimation = false;
        public Vector2 ReferenceResolution = new(1080f, 1920f);
        public string GameObjectLayerName = "UIObject3D";
        public List<SpriteRendererSetting> SpriteRenderers = new();
        public List<TextMeshProSetting> TextMeshes = new();

        public SpriteRendererSetting GetSpriteRendererSetting(string hierarchyPath)
        {
            foreach (var r in SpriteRenderers)
            {
                if (r.HierarchyPath == hierarchyPath)
                {
                    return r;
                }
            }

            Debug.LogError($"GetSpriteRendererSetting failed, {hierarchyPath} not found!");
            return null;
        }

        public TextMeshProSetting GetTextMeshSetting(string hierarchyPath)
        {
            foreach (var textMesh in TextMeshes)
            {
                if (textMesh.HierarchyPath == hierarchyPath)
                {
                    return textMesh;
                }
            }
            Debug.LogError($"GetTextMeshSetting failed, {hierarchyPath} not found!");
            return null;
        }
    }

    [Serializable]
    public class SpriteRendererSetting
    {
        public string HierarchyPath;
        public SortOrderSetting SortOrder;
    }

    [Serializable]
    public class TextMeshProSetting
    {
        public string HierarchyPath;
        public SortOrderSetting SortOrder;
        public float FontSize = 2.3f;
        public float CharacterSpacing = 0;
        public float OutlineWidth = 0;
        //public float UnderlaySoftness = 0;
        //public float UnderlayDilate = 1;
        //public Color UnderlayColor = Color.black;
        public bool IsBold = true;
    }

    [Serializable]
    public class SortOrderSetting
    {
        public string SortingLayerName;
        public int OrderInLayer;
    }
}
