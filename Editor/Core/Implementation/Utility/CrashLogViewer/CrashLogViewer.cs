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


using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    internal class CrashLogViewer : EditorWindow
    {
        [MenuItem("XDay/Other/Crash Log查看")]
        static void Open()
        {
            GetWindow<CrashLogViewer>("Crash Log查看").Show();
        }

        private void OnEnable()
        {
            m_SymbolFolderPath = EditorPrefs.GetString(m_SymbolFolderPathName);
            m_UnityPath = EditorPrefs.GetString(m_UnityPathName);
            m_CrashLogText = EditorPrefs.GetString(m_CrashLogTextName);
            m_StackText = EditorPrefs.GetString(m_StackTextName);
        }

        private void OnGUI()
        {
            var size = position.size;

            if (GUILayout.Button("显示堆栈"))
            {
                Convert();
            }

            EditorGUI.BeginChangeCheck();

            m_SymbolFolderPath = EditorGUILayout.TextField("符号表文件夹路径", m_SymbolFolderPath);
            m_UnityPath = EditorGUILayout.TextField("Unity文件夹路径", m_UnityPath);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Crash Log");
            m_CrashLogText = EditorGUILayout.TextArea(m_CrashLogText, EditorStyles.textArea, GUILayout.MaxWidth(size.x / 2), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Stack");
            EditorGUILayout.TextArea(m_StackText, EditorStyles.textArea, GUILayout.MaxWidth(size.x / 2), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(m_SymbolFolderPathName, m_SymbolFolderPath);
                EditorPrefs.SetString(m_UnityPathName, m_UnityPath);
                EditorPrefs.SetString(m_CrashLogTextName, m_CrashLogText);
                EditorPrefs.SetString(m_StackTextName, m_StackText);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Convert()
        {
            m_StackText = "";

            if (string.IsNullOrEmpty(m_UnityPath))
            {
                EditorUtility.DisplayDialog("出错了", "没有设置Unity目录", "确定");
                return;
            }

            if (string.IsNullOrEmpty(m_SymbolFolderPath))
            {
                EditorUtility.DisplayDialog("出错了", "没有设置符号表文件夹", "确定");
                return;
            }

            if (string.IsNullOrEmpty(m_CrashLogText))
            {
                EditorUtility.DisplayDialog("出错了", "没有输入Crash Log,无法显示堆栈", "确定");
                return;
            }

            StringBuilder stack = new();
            var lines = m_CrashLogText.Split('\n');
            var idx = 1;

            var line2AddrPath = $"{m_UnityPath}\\Editor\\Data\\PlaybackEngines\\AndroidPlayer\\NDK\\toolchains\\llvm\\prebuilt\\windows-x86_64\\bin\\llvm-addr2line.exe";

            foreach (var line in lines)
            {
                var validLine = line.Trim();
                if (string.IsNullOrEmpty(validLine))
                {
                    continue;
                }
                TryExtractInfo(validLine, out var address, out var soName, out var arch);
                string archFolder = "";
                if (arch == "armeabi-v7a")
                {
                    archFolder = "armeabi-v7a";
                }
                else if (arch == "arm64")
                {
                    archFolder = "arm64-v8a";
                }
                else
                {
                    Debug.Assert(false, $"Uknown arch: {arch}");
                }

                if (string.IsNullOrEmpty(archFolder))
                {
                    continue;
                }

                EditorHelper.RunProcess(line2AddrPath, $"-f -e {m_SymbolFolderPath}\\{archFolder}\\{soName} {address}", m_SymbolFolderPath, out var output, out var error);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);
                }
                stack.Append($"{idx}. {output}");
                ++idx;
            }
            m_StackText = stack.ToString();
            EditorPrefs.SetString(m_StackTextName, m_StackText);
        }

        private bool TryExtractInfo(string logLine, out string address, out string soName, out string arch)
        {
            address = null;
            soName = null;
            arch = null;

            Match match = Regex.Match(logLine, m_Pattern);
            if (match.Success)
            {
                address = match.Groups[1].Value;
                arch = match.Groups[2].Value;
                soName = match.Groups[3].Value; 
                return true;
            }
            return false;
        }

        private string m_SymbolFolderPath;
        private string m_UnityPath;
        private string m_CrashLogText;
        private string m_StackText;
        private const string m_Pattern = @"pc\s+([0-9a-fA-F]+)\s+.*?/lib/([^/]+)/([^/]+\.so)";
        private const string m_SymbolFolderPathName = "XDay.SymbolFolderPath";
        private const string m_UnityPathName = "XDay.UnityPath";
        private const string m_CrashLogTextName = "XDay.CrashLogText";
        private const string m_StackTextName = "XDay.StackText";
        private Vector2 m_ScrollPos;
    }
}
