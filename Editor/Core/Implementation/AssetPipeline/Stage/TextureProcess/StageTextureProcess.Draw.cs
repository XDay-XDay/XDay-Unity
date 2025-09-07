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
using XDay.UtilityAPI;

namespace XDay.AssetPipeline.Editor
{
    internal partial class StageTextureProcess
    {
        public override void Draw()
        {
            DrawRules();
        }

        private void DrawRules()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowRules = EditorGUILayout.Foldout(m_ShowRules, "规则");
            if (GUILayout.Button("+", GUILayout.MaxWidth(20)))
            {
                Rules.Add(new TextureImportRule());
            }
            EditorGUILayout.EndHorizontal();
            if (m_ShowRules)
            {
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < Rules.Count; ++i)
                    {
                        bool deleted = DrawRule(i, Rules[i]);
                        if (deleted)
                        {
                            Rules.RemoveAt(i);
                            break;
                        }
                    }
                });
            }
        }

        private bool DrawRule(int index, TextureImportRule rule)
        {
            bool deleted = false;
            EditorGUILayout.BeginHorizontal();
            rule.Show = EditorGUILayout.Foldout(rule.Show, $"{index}");
            if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
            {
                deleted = true;
            }
            EditorGUILayout.EndHorizontal();
            if (rule.Show)
            {
                EditorHelper.IndentLayout(() =>
                {
                    rule.Enable = EditorGUILayout.Toggle("Enable", rule.Enable);
                    rule.Description = EditorGUILayout.TextArea(rule.Description);
                    rule.PathPattern = EditorGUILayout.TextField("Pattern", rule.PathPattern);
                    rule.EnableMipmap = EditorGUILayout.Toggle("Enable Mipmap", rule.EnableMipmap);
                    rule.MaxTextureSize = EditorGUILayout.IntField("Max Size", rule.MaxTextureSize);
                    rule.WrapMode = (TextureWrapModeEx)EditorGUILayout.EnumPopup("Wrap Mode", rule.WrapMode);
                    rule.FilterMode = (FilterModeEx)EditorGUILayout.EnumPopup("Filter Mode", rule.FilterMode);
                    rule.Readable = (ReadableMode)EditorGUILayout.EnumPopup("Readable", rule.Readable);
                    rule.SpriteMeshType = (SpriteMeshType)EditorGUILayout.EnumPopup("Sprite Mesh Type", rule.SpriteMeshType);
                    rule.Compression = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", rule.Compression);
                });
            }
            return deleted;
        }

        [SerializeField]
        private bool m_ShowRules = true;
    }
}
