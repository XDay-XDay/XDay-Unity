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

#if UNITY_EDITOR

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace XDay.UtilityAPI
{
    public static class EditorHelper
    {
        public static bool IsPluginMarkedAsEditorOnly(PluginImporter importer)
        {
            if (importer != null)
            {
                if (importer.GetCompatibleWithEditor() &&
                    !importer.GetCompatibleWithPlatform(BuildTarget.Android) &&
                    !importer.GetCompatibleWithPlatform(BuildTarget.iOS) &&
                    !importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows) &&
                    !importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX))
                {
                    return true;
                }
            }

            return false;
        }

        public static void HorizontalLine()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        public static bool SpanEqual(ReadOnlySpan<byte> span1, ReadOnlySpan<byte> span2)
        {
            return span1.SequenceEqual(span2);
        }

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

        public static GameObject GetEditingPrefab()
        {
            if (!IsPrefabMode())
            {
                return null;
            }
            var assetPath = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
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

        public static void SelectFolder(string folder)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                folder = Helper.ToUnityPath(folder);
                var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        public static string QueryAssetFilePath<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            var assets = QueryAssets<T>(searchInFolders);

            if (assets.Count > 0)
            {
                return AssetDatabase.GetAssetPath(assets[0]);
            }

            return "";
        }

        public static T GetObjectFromGuid<T>(string guid) where T : UnityEngine.Object
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public static string QueryScriptFilePath(string prefix, string className)
        {
            var assetGuids = AssetDatabase.FindAssets($"t:script {className}");
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var fileName = Helper.GetPathName(assetPath, false);
                if (fileName == className && 
                    assetPath.IndexOf("XDay") >= 0)
                {
                    if (assetPath.IndexOf(prefix) >= 0)
                    {
                        return assetPath;
                    }
                }
            }
            return null;
        }

        public static List<T> QueryAssets<T>(string[] searchInFolders = null, bool onlyTopDir = false) where T : UnityEngine.Object
        {
            var typeName = $"t:{typeof(T).Name}";
            string[] assetGuids;
            if (searchInFolders == null)
            {
                assetGuids = AssetDatabase.FindAssets(typeName);
            }
            else
            {
                assetGuids = AssetDatabase.FindAssets(typeName, searchInFolders);
            }

            List<T> assets = new();
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                if (searchInFolders == null ||
                    !onlyTopDir ||
                    IsInFolder(path, searchInFolders))
                {
                    assets.Add(AssetDatabase.LoadAssetAtPath<T>(path));
                }
            }
            return assets;
        }

        public static T QueryAsset<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            var assets = QueryAssets<T>(searchInFolders);
            if (assets.Count > 0)
            {
                return assets[0];
            }
            return null;
        }

        public static T QueryOneAsset<T>(string searchInFolder = null) where T : UnityEngine.Object
        {
            var assets = QueryAssets<T>(new string[] { searchInFolder });
            if (assets.Count > 0)
            {
                return assets[0];
            }
            return null;
        }

        public static void RunProcess(string exeName, string argument, string workingDir, out string output, out string error, 
            bool waitForExit = true, 
            bool shellExecute = false,
            bool createNoWindow = true)
        {
            output = "";
            error = "";

            // Define the process to run
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exeName,
                Arguments = argument,
                RedirectStandardOutput = !shellExecute, // Redirect output
                RedirectStandardError = !shellExecute, // Redirect errors
                UseShellExecute = shellExecute, // Necessary for redirection
                WorkingDirectory = workingDir,
                CreateNoWindow = createNoWindow,
            };

            try
            {
                using Process process = new();
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

        public static string ObjectFieldGUID<T>(string title, string assetGUID,
                    float maxWidth = 0, System.Action onValueChange = null, string tooltip = null,
                    bool allowSceneObjects = false) where T : UnityEngine.Object
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGUID);
            var newPath = ObjectField<T>(title, path, maxWidth, onValueChange, tooltip, allowSceneObjects);
            if (newPath != path)
            {
                return AssetDatabase.AssetPathToGUID(newPath);
            }
            return assetGUID;
        }

        public static string ObjectField<T>(string title, string assetPath, 
            float maxWidth = 0, System.Action onValueChange = null, string tooltip = null, 
            bool allowSceneObjects = false) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (string.IsNullOrEmpty(tooltip))
            {
                if (maxWidth > 0)
                {
                    asset = EditorGUILayout.ObjectField(title, asset, typeof(T), allowSceneObjects, GUILayout.MinWidth(maxWidth)) as T;
                }
                else
                {
                    asset = EditorGUILayout.ObjectField(title, asset, typeof(T), allowSceneObjects) as T;
                }
            }
            else
            {
                if (maxWidth > 0)
                {
                    asset = EditorGUILayout.ObjectField(new GUIContent(title, tooltip), asset, typeof(T), allowSceneObjects, GUILayout.MinWidth(maxWidth)) as T;
                }
                else
                {
                    asset = EditorGUILayout.ObjectField(new GUIContent(title, tooltip), asset, typeof(T), allowSceneObjects) as T;
                }
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

            if (GUILayout.Button("...", GUILayout.MaxWidth(30)))
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
            List<string> files = new();
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

        public static void WriteFile(string text, string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, text);
                return;
            }

            var existedText = File.ReadAllText(path);
            if (existedText != text)
            {
                File.WriteAllText(path, text);
            }
        }

        public static void WriteFile(byte[] bytes, string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, bytes);
                return;
            }

            var existedData = File.ReadAllBytes(path);
            if (existedData.Length != bytes.Length ||
                !SpanEqual(bytes, existedData))
            {
                File.WriteAllBytes(path, bytes);
            }
        }

        public static T LoadAssetByGUID<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static string GetObjectGUID(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return "";
            }
            var path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.AssetPathToGUID(path);
        }

        public static long GetObjectLocalID(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj.GetInstanceID(), out _, out long localID);
            return localID;
        }

        public static string QueryPlatformName()
        {
            var name = "StandaloneWindows";
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                name = "Android";
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                name = "iPhone";
            }
            return name;
        }

        public static bool DrawImageWithSelection(Texture2D texture, bool selected, int size)
        {
            EditorGUILayout.LabelField(new GUIContent(texture), GUILayout.Width(size), GUILayout.Height(size));
            var lastRect = GUILayoutUtility.GetLastRect();
            var evt = Event.current;

            if (selected)
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.yellow;
                GUI.Box(lastRect, GUIContent.none);
                GUI.backgroundColor = oldColor;
            }

            if (lastRect.Contains(evt.mousePosition) &&
                evt.type == EventType.MouseDown)
            {
                selected = !selected;
                evt.Use();
            }

            return selected;
        }

        public static Texture2D CreateReadableTexture(Texture2D texture)
        {
            if (texture.isReadable)
            {
                return UnityEngine.Object.Instantiate(texture);
            }

            var renderTexture = new RenderTexture(texture.width, texture.height, depth: 24);
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture, renderTexture);
            var copy = new Texture2D(texture.width, texture.height)
            {
                name = $"{texture.name}_readable"
            };
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            return copy;
        }

        public static bool IsEmptyDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return true;
            }
    
            foreach (var _ in Directory.EnumerateFileSystemEntries(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                return false;
            }            

            return true;
        }

        public static void SaveTextureToFile(Texture2D texture, string filePath)
        {
            if (!texture.isReadable)
            {
                UnityEngine.Debug.LogError("texture is not readable!");
                return;
            }
            var ext = Helper.GetPathExtension(filePath);
            if (ext == "png")
            {
                File.WriteAllBytes(filePath, texture.EncodeToPNG());
            }
            else if (ext == "tga")
            {
                File.WriteAllBytes(filePath, texture.EncodeToTGA());
            }
            else if (ext == "jpg")
            {
                File.WriteAllBytes(filePath, texture.EncodeToJPG());
            }
            else
            {
                UnityEngine.Debug.Assert(false, $"unknown format {filePath}");
            }
            AssetDatabase.Refresh();
        }

        //need call Handles.BeginGUI first
        public static void DrawCubeBezier(Vector2 startPoint, Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint, Color color, float width)
        {
            var oldColor = Handles.color;
            Handles.color = Color.green;
            Handles.DrawBezier(startPoint, endPoint, controlPoint1, controlPoint2, Color.green, null, width);
            Handles.color = oldColor;
        }

        public static void DrawLineRect(Rect r, Color color, float width)
        {
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawCircle(float radius, Vector2 center, Color color)
        {
            Handles.BeginGUI();

            Handles.DrawSolidArc(center, Vector3.forward, Vector3.right, 360, radius);

            Handles.EndGUI();
        }

        public static void DrawLineCircle(float radius, Vector2 center, Color color, float width)
        {
            Handles.BeginGUI();

            Handles.DrawWireArc(center, Vector3.forward, Vector3.right, 360, radius);

            Handles.EndGUI();
        }

        public static bool Foldout(bool state) 
        {
            return EditorGUILayout.Toggle(state, EditorStyles.foldout, GUILayout.Width(15));
        }

        public static bool ImageButton(Texture2D texture, string tooltip = "", int size = 18)
        {
            var oldPadding = GUI.skin.button.padding;
            int pad = 1;
            GUI.skin.button.padding = new RectOffset(pad, pad, pad, pad);
            bool pressed = false;
            if (GUILayout.Button(new GUIContent(texture, tooltip), GUILayout.MaxWidth(size), GUILayout.MaxHeight(size)))
            {
                pressed = true;
            }
            GUI.skin.button.padding = oldPadding;
            return pressed;
        }

        public static bool ImageButton(string texturePath, string tooltip = "", int size = 18)
        {
            var oldPadding = GUI.skin.button.padding;
            int pad = 1;
            GUI.skin.button.padding = new RectOffset(pad, pad, pad, pad);
            bool pressed = false;
            if (GUILayout.Button(new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath), tooltip), GUILayout.MaxWidth(size), GUILayout.MaxHeight(size)))
            {
                pressed = true;
            }
            GUI.skin.button.padding = oldPadding;
            return pressed;
        }

        public static bool Button(string text, string tooltip = "")
        {
            if (GUILayout.Button(new GUIContent(text, tooltip)))
            {
                return true;
            }
            return false;
        }

        public static void ImportTextureAsSprite(string texturePath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.spritePixelsPerUnit = 100;
                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Bilinear;
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                UnityEngine.Debug.LogError("import failed!");
            }
        }

        public static string ConvertPackageToPhysicalPath(string packagePath)
        {
            if (!packagePath.StartsWith("Packages/"))
            {
                return null;
            }

            // ��ȡ�������� "com.example.mypackage"��
            var parts = packagePath.Split('/');
            if (parts.Length < 2) return null;
            var packageName = parts[1];

            // 通过PackageManager获取包信息
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packagePath);
            if (packageInfo == null)
            {
                return null;
            }

            // 返回解析后的物理路径
            return packageInfo.resolvedPath;
        }

        public static bool DrawIntArray(string title, string elementName, ref int[] array, string tooltip = "")
        {
            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUILayout.DelayedIntField(new GUIContent(title, tooltip), array.Length);
            if (newSize != array.Length)
            {
                System.Array.Resize(ref array, newSize);
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.IntField($"{elementName} {i}", array[i]);
            }
            EditorGUI.indentLevel--;
            return EditorGUI.EndChangeCheck();
        }

        public static void DrawFloatArray(string title, string elementName, ref float[] array)
        {
            int newSize = EditorGUILayout.DelayedIntField(title, array.Length);
            if (newSize != array.Length)
            {
                System.Array.Resize(ref array, newSize);
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.FloatField($"{elementName} {i}", array[i]);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawFloatArray(string title, string[] elementNames, ref float[] array)
        {
            int newSize = EditorGUILayout.DelayedIntField(title, array.Length);
            if (newSize != array.Length)
            {
                System.Array.Resize(ref array, newSize);
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.FloatField($"{elementNames[i]}", array[i]);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawStringArray(string title, string elementName, ref string[] array)
        {
            int newSize = EditorGUILayout.DelayedIntField(title, array.Length);
            if (newSize != array.Length)
            {
                System.Array.Resize(ref array, newSize);
                for (var i = 0; i < array.Length; ++i)
                {
                    if (array[i] == null)
                    {
                        array[i] = "";
                    }
                }
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.TextField($"{elementName} {i}", array[i]);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawIntList(string title, string elementName, List<int> list)
        {
            int newSize = EditorGUILayout.DelayedIntField(title, list.Count);
            if (newSize != list.Count)
            {
                var added = newSize - list.Count;
                for (var i = 0; i < added; i++)
                {
                    list.Add(0);
                }
                for (var i = 0; i < -added; i++)
                {
                    list.RemoveAt(list.Count - 1);
                }
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = EditorGUILayout.IntField($"{elementName} {i}", list[i]);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawVector3Array(string title, string elementName, ref Vector3[] array)
        {
            int newSize = EditorGUILayout.DelayedIntField(title, array.Length);
            if (newSize != array.Length)
            {
                System.Array.Resize(ref array, newSize);
                for (var i = 0; i < array.Length; ++i)
                {
                    if (array[i] == null)
                    {
                        array[i] = Vector3.zero;
                    }
                }
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = EditorGUILayout.Vector3Field($"{elementName} {i}", array[i]);
            }
            EditorGUI.indentLevel--;
        }

        public static void DrawVector3List(string title, string elementName, List<Vector3> list)
        {
            int newSize = EditorGUILayout.DelayedIntField(title, list.Count);
            if (newSize != list.Count)
            {
                var added = newSize - list.Count;
                for (var i = 0; i < added; i++)
                {
                    list.Add(Vector3.zero);
                }
                for (var i = 0; i < -added; i++)
                {
                    list.RemoveAt(list.Count - 1);
                }
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                var removed = false;
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.Vector3Field($"{elementName} {i}", list[i]);
                EditorGUILayout.Space();
                if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                {
                    list.RemoveAt(i);
                    removed = true;
                }
                EditorGUILayout.EndHorizontal();

                if (removed)
                {
                    break;
                }
            }
            EditorGUI.indentLevel--;
        }

        public static Vector3 MousePositionToWorldRay(Vector2 mousePosition, out Ray ray, float planeHeight = 0)
        {
            ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Vector3.up, planeHeight);
            plane.Raycast(ray, out float distance);
            return ray.origin + ray.direction * distance;
        }

        public static void OpenCSFile(string filePath, int lineNumber)
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
        }

        public static void MoveAsset(UnityEngine.Object asset, string targetFolder)
        {
            if (asset == null)
            {
                return;
            }

            var oldPath = AssetDatabase.GetAssetPath(asset);
            var fileName = Helper.GetPathName(oldPath, true);
            var newPath = $"{targetFolder}/{fileName}";
            AssetDatabase.MoveAsset(oldPath, newPath);
        }

        public static bool IsInFolder(string path, string[] folders)
        {
            foreach (var dir in folders)
            {
                var folderPath = Helper.GetFolderPath(path);
                if (folderPath == dir)
                {
                    return true;
                }
            }
            return false;
        }
		
		public static void DrawFullRect(Color color, Vector2 offset)
        {
            var pos = Helper.GetCurrentCursorPos();
            EditorGUI.DrawRect(new Rect(pos.x + offset.x, pos.y + offset.y, EditorGUIUtility.currentViewWidth, 20), color);
        }

        public static void DrawLineStrip(List<Vector3> points, Color color, bool usingGizmo)
        {
            if (usingGizmo)
            {
                var originalColor = Gizmos.color;
                Gizmos.color = color;

                for (int i = 0; i < points.Count - 1; ++i)
                {
                    Gizmos.DrawLine(points[i], points[i + 1]);
                }

                Gizmos.color = originalColor;
            }
            else
            {
                var originalColor = Handles.color;
                Handles.color = color;

                for (int i = 0; i < points.Count - 1; ++i)
                {
                    Handles.DrawLine(points[i], points[i + 1]);
                }

                Handles.color = originalColor;
            }
        }

        public static string GetJsonValue(string jsonFilePath, string key)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(jsonFilePath));
            dict.TryGetValue(key, out var value);
            return value;
        }

        public static void PingObject(UnityEngine.Object obj)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        public static void DrawList<T>(string title, List<T> list, Action<T, int> drawElement) where T : new()
        {
            int newSize = EditorGUILayout.DelayedIntField(title, list.Count);
            if (newSize != list.Count)
            {
                var added = newSize - list.Count;
                for (var i = 0; i < added; i++)
                {
                    list.Add(new T());
                }
                for (var i = 0; i < -added; i++)
                {
                    list.RemoveAt(list.Count - 1);
                }
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                var removed = false;
                EditorGUILayout.BeginHorizontal();
                drawElement(list[i], i);
                EditorGUILayout.Space();
                if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                {
                    list.RemoveAt(i);
                    removed = true;
                }
                EditorGUILayout.EndHorizontal();

                if (removed)
                {
                    break;
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}

#endif

//XDay