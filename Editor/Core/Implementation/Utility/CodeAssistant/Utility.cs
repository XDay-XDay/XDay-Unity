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
using UnityEditor;
using System.IO;

namespace XDay.UtilityAPI.Editor.CodeAssistant
{
    internal static class Utility
    {
        public static string ReadString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length > 0)
            {
                var bytes = reader.ReadBytes(length);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }

            return "";
        }

        public static string[] ReadStringArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            string[] ret = new string[length];
            for (int i = 0; i < length; ++i)
            {
                ret[i] = ReadString(reader);
            }

            return ret;
        }

        public static void OpenCSFile(string dtePath, string filePath, int line, string folderName)
        {    
            string arguments = $"{folderName} {filePath} {line} false";
            System.Diagnostics.Process.Start(dtePath, arguments);
        }

        public static string GetAccessModifierString(AccessModifier modifier)
        {
            if (modifier == AccessModifier.Internal)
            {
                return "*";
            }
            if (modifier == AccessModifier.ProtectedInternal)
            {
                return "**";
            }
            if (modifier == AccessModifier.Public)
            {
                return "+";
            }
            if (modifier == AccessModifier.Private)
            {
                return "-";
            }
            if (modifier == AccessModifier.Protected)
            {
                return "#";
            }
            return "";
        }

        public static Color GetTypeColor(TypeInfo type)
        {
            if (type is ClassInfo)
            {
                return new Color32(255, 255, 179, 255);
            }

            if (type is InterfaceInfo)
            {
                return new Color32(157, 255, 157, 255);
            }

            if (type is StructInfo)
            {
                return new Color32(255, 153, 104, 255);
            }

            if (type is EnumInfo)
            {
                return new Color32(128, 255, 255, 255);
            }

            UnityEngine.Debug.Assert(false, "todo");
            return Color.red;
        }

        public static CodeAssistantData GetData()
        {
            return EditorWindow.GetWindow<CodeAssistantTreeViewWindow>("Code Tree", false).data;
        }

        public static Color32 METHOD_COLOR = new Color32(255, 127, 39, 255);
        public static Color32 PROPERTY_COLOR = new Color32(255, 159, 207, 255);
    }
}

