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

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace XDay.UtilityAPI
{
    public static class EditorHelper
    {
        public static bool IsIdentity(GameObject obj)
        {
            return obj.transform.position == Vector3.zero &&
                obj.transform.rotation == Quaternion.identity &&
                obj.transform.localScale == Vector3.one;
        }

        public static bool IsPrefabMode()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        public static bool IsPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            var type = PrefabUtility.GetPrefabAssetType(prefab);
            return type == PrefabAssetType.Regular || type == PrefabAssetType.Variant;
        }

        public static void IndentLayout(Action callback, int space = 20)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(space);
                using (new GUILayout.VerticalScope())
                {
                    callback();
                }
            }
        }

        public static void ShowInExplorer(string folder)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                folder = Helper.ToWinSlash(folder);

                Process.Start("explorer.exe", $"\"{folder}\"");
            }
        }

        public static string QueryAssetFilePath<T>() where T : UnityEngine.Object
        {
            var assets = QueryAssets<T>();

            if (assets.Length > 0)
            {
                return AssetDatabase.GetAssetPath(assets[0]);
            }

            return "";
        }

        public static T[] QueryAssets<T>() where T : UnityEngine.Object
        {
            var typeName = $"t:{typeof(T).Name}";
            var assetGuids = AssetDatabase.FindAssets(typeName);
            T[] assets = new T[assetGuids.Length];
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assetGuids[i]));
            }
            return assets;
        }

        public static T QueryAsset<T>() where T : UnityEngine.Object
        {
            var assets = QueryAssets<T>();
            if (assets.Length > 0)
            {
                return assets[0];
            }
            return null;
        }

        public static void RunProcess(string exeName, string argument, out string output, out string error, bool waitForExit = true)
        {
            output = "";
            error = "";

            // Define the process to run
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exeName,
                Arguments = argument,
                RedirectStandardOutput = true, // Redirect output
                RedirectStandardError = true, // Redirect errors
                UseShellExecute = false, // Necessary for redirection
                CreateNoWindow = true // Do not create a window
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Shake the process
                    process.Start();

                    // Read output and error streams
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();

                    if (waitForExit)
                    {
                        process.WaitForExit();
                    }

                    // Get the return code (exit code)
                    int exitCode = process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public static void MakeWindowCenterAndSetSize(this EditorWindow window, float minWidth = 0, float minHeight = 0, float maxWidth = 0, float maxHeight = 0)
        {
            if (minWidth > 0)
            {
                window.minSize = new Vector2(minWidth, minHeight);
                window.maxSize = new Vector2(maxWidth, maxHeight);
            }

            var position = window.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            window.position = position;
        }

        public static void DeleteFolderContent(string folder, List<string> exceptions = null)
        {
            if (Directory.Exists(folder))
            {
                foreach (var path in Directory.EnumerateFileSystemEntries(folder, "*.*", SearchOption.AllDirectories))
                {
                    var validPath = Helper.ToNixSlash(path);
                    var fileName = Helper.GetPathName(validPath, true);
                    if (exceptions != null)
                    {
                        var found = false;
                        foreach (var exception in exceptions)
                        {
                            if (validPath.IndexOf(exception) >= 0)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            continue;
                        }
                    }
                    var ok = FileUtil.DeleteFileOrDirectory(validPath);
                    UnityEngine.Debug.Assert(ok);
                }
            }

            AssetDatabase.Refresh();
        }

        public static string ObjectField<T>(string title, string assetPath, System.Action onValueChange = null, string tooltip = null, bool allowSceneObjects = false) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (string.IsNullOrEmpty(tooltip))
            {
                asset = EditorGUILayout.ObjectField(title, asset, typeof(T), allowSceneObjects) as T;
            }
            else
            {
                asset = EditorGUILayout.ObjectField(new GUIContent(title, tooltip), asset, typeof(T), allowSceneObjects) as T;
            }
            var newPath = AssetDatabase.GetAssetPath(asset);
            if (newPath != assetPath && onValueChange != null)
            {
                onValueChange();
            }
            return newPath;
        }

        public static string DirectoryField(string title, string path, string defaultFolder = "", System.Action onValueChange = null, string tooltip = null)
        {
            string newPath;

            EditorGUILayout.BeginHorizontal();

            if (string.IsNullOrEmpty(tooltip))
            {
                newPath = EditorGUILayout.TextField(title, path);
            }
            else
            {
                newPath = EditorGUILayout.TextField(new GUIContent(title, tooltip), path);
            }

            if (GUILayout.Button("...", GUILayout.MaxWidth(60)))
            {
                newPath = EditorUtility.OpenFolderPanel("Select", defaultFolder, "");
            }

            EditorGUILayout.EndHorizontal();

            if (newPath != path && onValueChange != null)
            {
                onValueChange();
            }
            return newPath;
        }

        public static List<string> EnumerateFiles(string folder, bool recursively, bool convertToUnityAssetPath)
        {
            List<string> files = new List<string>();
            foreach (var filePath in Directory.EnumerateFiles(folder, "*.*", recursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                string validFilePath = Helper.ToNixSlash(filePath);
                if (convertToUnityAssetPath)
                {
                    validFilePath = Helper.ToUnityPath(validFilePath);
                }
                files.Add(validFilePath);
            }

            return files;
        }

        public static T GetEditorWindow<T>() where T : EditorWindow
        {
            UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(T));
            if (array.Length > 0)
            {
                return array[0] as T;
            }
            return null;
        }
    }
}

#endif

//XDay