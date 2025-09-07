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

using TMPro;
using UnityEngine;

namespace XDay.GUIAPI
{
    [AddComponentMenu("XDay/UI/TextMeshPro", 0)]
    public class TextMeshPro3D : TMPro.TextMeshPro
    {
        public float UnderlayDilate;
        public float UnderlaySoftness;
        public Color UnderlayColor;

        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            var mat = fontMaterial;
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, UnderlayColor);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, UnderlayDilate);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, UnderlaySoftness);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                var underlayColor = fontSharedMaterial.GetColor(ShaderUtilities.ID_UnderlayColor);
                if (underlayColor != UnderlayColor)
                {
                    UnderlayColor = underlayColor;
                    UnityEditor.EditorUtility.SetDirty(gameObject);
                }

                var underlaySoftness = fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlaySoftness);
                if (underlaySoftness != UnderlaySoftness)
                {
                    UnderlaySoftness = underlaySoftness;
                    UnityEditor.EditorUtility.SetDirty(gameObject);
                }

                var underlayDilate = fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlayDilate);
                if (underlayDilate != UnderlayDilate)
                {
                    UnderlayDilate = underlayDilate;
                    UnityEditor.EditorUtility.SetDirty(gameObject);
                }
            }
        }
#endif
    }
}
